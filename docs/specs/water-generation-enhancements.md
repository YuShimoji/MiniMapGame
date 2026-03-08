# SP-025: 水系生成強化

**Status**: partial
**Category**: core
**実装率**: 28% (W-1 done, W-5 done)

## 概要

現在の水系生成（WaterGenerator）の表現力を拡張する。
「メタ地理」レイヤーによる自然な川流路決定、海岸線の凹凸パターン、
水辺地形に応じた建物・オブジェクト配置制御、
地形との相互作用で生まれる二次的地形（砂州・浜辺帯）を追加する。

Phase 1-3 で構築済みのインフラ（WaterProfile, WaterBodyData, WaterTerrainInteraction, ElevationMap carving）の上に構築する。

---

## W-1: メタ地理ベースの川流路決定

### 現状
- 川は常に北→南（y=-5 → y=h+10）に固定
- `flowResponsiveness` で左右の地形追従はあるが、流れの大方向は固定
- `WaterBodyData.flowDirection` フィールドは存在するが未活用

### 設計思想
「表示されるマップ」と「地形の大局的構造」を分離する。
丘陵/山の配置情報から **標高勾配場** が自然に得られるので、
川はその勾配を降下することで方向が自然に決まる。

### 仕組み

```
 [丘陵配置] → [ElevationMap] → [標高勾配場]
                                    ↓
                    [源流: 高標高領域] → [勾配降下歩行] → [河口: 低標高端/海岸]
```

1. **源流配置**: ElevationMapの高標高領域をグリッドサンプリングして、
   上位N点の中からrngで選択
2. **歩行**: 各ステップで `(-dz/dx, -dz/dy)` の勾配方向をメイン方向とし、
   その上に蛇行(meanderBias) + ジッター(jitter) を重畳
3. **終端**: マップ端に到達 or 海岸ポリゴンに入ったら終了

### 自然に解決される問題

| 条件 | 結果 |
|------|------|
| 海岸あり | 丘陵が海岸反対側に集中 → 勾配が海岸方向 → 川は自然に海へ |
| 海岸なし | 丘陵群の谷間を通り、最も低いマップ端へ流出 |
| Mountain | 尾根から谷へ流れる渓流 |
| Rural | なだらかな丘陵の間をゆるやかに蛇行 |
| 平坦地 (Grid) | 勾配ほぼゼロ → momentum維持で直線的 = 運河的表現 |

### 技術詳細

**勾配計算** (各ステップ):
```
float elevN = elevMap.Sample(pos + (0, delta));
float elevS = elevMap.Sample(pos - (0, delta));
float elevE = elevMap.Sample(pos + (delta, 0));
float elevW = elevMap.Sample(pos - (delta, 0));
Vector2 gradient = new Vector2(elevE - elevW, elevN - elevS) / (2 * delta);
Vector2 flowDir = -gradient.normalized;  // 降下方向
```

**安定化メカニズム**:
- **momentum**: 前ステップ方向を40%保持 → 急転回防止
- **最大旋回角**: 1ステップで最大45°まで → ジグザグ防止
- **平坦地フォールバック**: 勾配magnitude < 0.01 の場合、momentum方向を維持
- **訪問済み回避**: pathPointsとの距離が一定以下ならmomentum方向を強制

**源流サンプリング** (rng不使用、決定論的):
- マップを 8×8 グリッドでサンプリング
- 上位3点を候補として `rng.Next()` で1点選択（rng消費1回）
- 海岸ポリゴン内の候補は除外

### WaterProfile変更
- 追加なし（勾配追従はElevationMapから自動決定）
- 既存 `flowResponsiveness` が勾配追従の強度として再利用される

### 影響範囲
- `WaterGenerator.GenerateRiver` の書き換え（源流決定 + 歩行ロジック）
- `WaterGenerator.FindRiverSource` 新メソッド追加
- 他ファイルへの影響なし（下流のwidth/depth/carvingは進行率tで制御済み）

---

## W-2: 海岸線の入り江・岬パターン

### 現状
- 海岸線は基準線に対する微小ジッター（`coastlineRoughness * 0.12`）のみ
- 結果として直線に近い単調な海岸線

### 提案
大振幅の凹凸パターンを追加し、入り江（bay）と岬（cape）を生成する。

**パターン生成**:
1. 海岸線の全長を L として、`bayCount = Mathf.RoundToInt(L / baySpacing)` 個の湾曲点を配置
2. 各湾曲点で sin/cos ベースの凹凸を生成（振幅 = `inlandReach * bayAmplitude`）
3. 凹凸の向きをランダム化（+なら岬、-なら入り江）
4. 既存の微小ジッターはこの大パターンの上に重畳

**パラメータ案（WaterProfile.CoastConfig に追加）**:

| フィールド | 型 | 説明 | デフォルト |
|-----------|-----|------|-----------|
| bayAmplitude | float [0, 0.5] | 入り江/岬の振幅（inlandReach比率） | 0.3 |
| baySpacing | float | 湾曲間隔（ワールド単位） | 120 |

### 体験への影響
- 入り江は「隠れた港」「行き止まり」として探索の発見スポットになる
- 岬は見晴らし台・ランドマーク配置に適する
- 海岸線の変化がマップごとの個性を強める

### 入り江/岬/デルタ内の配置 → W-7 参照

---

## W-3: 砂州（蛇行内側の浅瀬）

### 現状
- 川の蛇行（meanderFrequency）は実装済み
- 蛇行の内側/外側で地形・深度に差がない

### 提案
蛇行の内側に砂州（浅瀬スパイク）を生成する。

**検出ロジック**:
1. 川の pathPoints を3点ずつ走査し、曲率を計算
2. 曲率が閾値を超える箇所で、蛇行の内側方向を判定
3. 内側に shallow spike（depth を 20-30% に低減）を WaterBodyData.depths に反映
4. 対応する WaterTerrainInteraction で内側のカービングを軽減（砂州が盛り上がる）

**WaterProfile.RiverConfig 追加パラメータ**:

| フィールド | 型 | 説明 | デフォルト |
|-----------|-----|------|-----------|
| sandbankStrength | float [0, 1] | 砂州の強さ（0で無効） | 0.4 |

### 視覚効果
- UV2 の depth が浅くなる → Water.shader で ShallowColor 表示
- foamThreshold 以下なら泡エフェクト表示 → 自然な浅瀬表現
- 既存シェーダーで追加コード不要

### 未決事項
- [ ] 砂州上を歩行可能にするか（NavMesh への影響）
- [ ] 砂州の幅は川幅の何%が自然か

---

## W-4: 浜辺遷移帯（Beach Transition Zone）

### 現状
- 海岸カービングで地形は窪むが、視覚的な水陸遷移はない
- 地面テクスチャ（GridGround.shader）は全域一色

### 提案
海岸線の内陸側に「浜辺帯」を生成し、地面カラーを遷移させる。

**実装案**:
1. WaterTerrainInteraction で海岸カービングと同時に「浜辺ゾーンデータ」を生成
2. GroundMesh の頂点カラーに「海岸からの距離」を書き込む（0=海岸線, 1=内陸）
3. GridGround.shader で頂点カラーを読み取り、groundColor → beachColor をブレンド

**MapTheme 追加**:

| フィールド | 型 | 説明 | デフォルト |
|-----------|-----|------|-----------|
| beachColor | Color | 浜辺カラー | (0.76, 0.70, 0.50, 1) |
| beachWidth | float | 浜辺帯の幅 | 20 |

### 影響範囲
- GridGround.shader: 頂点カラー読み取り追加
- MapManager.GenerateGroundMesh: 頂点カラー書き込み
- MapTheme / ThemeManager: beachColor 追加

### 未決事項
- [ ] 川沿いにも浜辺帯を適用するか
- [ ] beachWidth は WaterProfile 側に持つべきか MapTheme 側か

---

## W-5: プリセット別 meanderFrequency 自動調整

### 現状
- meanderFrequency は WaterProfile で統一値（デフォルト 0.5）
- Rural もCoastal も同じ蛇行パターン

### 提案
GeneratorType に応じて蛇行特性を自動調整するデフォルトを設定する。

| GeneratorType | meanderFrequency | swayAmount倍率 | 表現意図 |
|--------------|------------------|---------------|---------|
| Organic | 0.5 (デフォルト) | 1.0x | 標準的な都市河川 |
| Rural | 0.3 | 0.65x (実装済み) | ゆったりした田園の川 |
| Mountain | 0.8 | 0.4x | 狭い渓谷、急カーブ |
| Grid | 0.1 | 0.3x | 直線的な運河風 |

**実装方針**:
- WaterProfile が null（デフォルトフォールバック）の場合のみ自動調整
- カスタム WaterProfile を設定した場合はデザイナーの値を尊重

### 未決事項
- [ ] CreateDefaultFallback() にGeneratorType引数を追加するか、GenerateRiver内で調整するか

---

## W-6: 河口デルタ（River Delta）

W-2 の入り江・岬と同じ設計思想で扱う。
デルタは「川が海岸に到達する地点に形成される特殊な水辺地形」であり、
W-7（水辺地形配置システム）の一部として制御される。

### 生成
1. 川の pathPoints が海岸ポリゴンに接近（~80u）した時点で分岐開始
2. メイン流路 + 1-2本の支流（角度 ±15-30°、幅はメインの50-70%）
3. 支流は独立した WaterBodyData として waterBodies に追加

**WaterProfile.RiverConfig 追加パラメータ**:

| フィールド | 型 | 説明 | デフォルト |
|-----------|-----|------|-----------|
| deltaEnabled | bool | デルタ生成の有無 | true |
| deltaBranchCount | int [1, 3] | 分岐数（メイン含む） | 2 |

### 前提条件
- W-1（メタ地理ベース流路）が先に必要

---

## W-7: 水辺地形配置システム（Waterfront Placement）

### 設計思想
水辺地形（入り江、岬、デルタ、河口、川沿い）は建物・オブジェクトを
**排除するのではなく、何が出現しやすいかを制御する**。

制御は2層構造:
- **マクロ特性**: 水辺ゾーン全体の性格（人工的 ↔ 自然的）
- **ミクロ因子**: 個別建物位置での具体的条件（水距離、地形保護度など）

### マクロ特性: WaterfrontCharacter

各水辺ゾーン（入り江、岬、デルタ中洲、川沿いなど）に
`WaterfrontCharacter` を割り当てる。

```csharp
[System.Serializable]
public struct WaterfrontCharacter
{
    /// <summary>
    /// 0 = 完全に自然（野生の入り江、崖の岬）
    /// 1 = 完全に人工（港湾、マリーナ、遊歩道）
    /// </summary>
    [Range(0f, 1f)] public float urbanization;

    /// <summary>
    /// 0 = 完全に露出（外洋、風当たり強い）
    /// 1 = 完全に保護（入り江奥、防波堤内）
    /// </summary>
    [Range(0f, 1f)] public float shelter;
}
```

**決定ロジック**:

| ゾーン種別 | urbanization | shelter | 根拠 |
|-----------|-------------|---------|------|
| 入り江（大きい） | 0.6-0.9 | 0.7-0.9 | 天然の良港、人が集まりやすい |
| 入り江（小さい） | 0.1-0.4 | 0.5-0.8 | 隠れた入り江、自然が残る |
| 岬先端 | 0.2-0.5 | 0.0-0.2 | 風当たり強い、灯台向き |
| デルタ中洲 | 0.0-0.3 | 0.3-0.5 | 不安定な地形、自然の場 |
| 河口付近 | 0.4-0.7 | 0.4-0.6 | 港湾と自然の混合 |
| 川沿い（市街地） | 0.7-1.0 | 0.5-0.7 | 護岸整備された都市河川 |
| 川沿い（郊外） | 0.2-0.5 | 0.3-0.5 | 自然堤防の田園河川 |

これらの範囲内で `rng.Next()` により各ゾーンごとに値を決定する。

**影響を受ける要素**:
- `MapPreset.generatorType`: Grid→urbanization+0.2、Rural→urbanization-0.2 など
- `MapPreset.buildingDensity`: 高密度→urbanization寄り
- 入り江のサイズ（bayAmplitude × 実際の凹み量）: 大→urbanization寄り

### ミクロ因子: 個別建物への影響

各建物の `InteriorBuildingContext` に以下を追加:

```csharp
// 水辺コンテキスト（BuildingClassifierで算出）
public float waterfrontDist;        // 最寄り水辺からの距離
public float waterfrontUrbanization; // 最寄りゾーンのurbanization
public float waterfrontShelter;      // 最寄りゾーンのshelter
public bool isWaterfront;            // waterfrontDist < 閾値
```

### 配置制御マトリクス

`BuildingClassifier` が `waterfrontUrbanization` × `waterfrontShelter` に応じて
カテゴリ/サブタイプの重み付けを調整する:

**高urbanization・高shelter（港湾）**:
- Commercial↑ (Restaurant, Hotel, Cafe)
- Special: Marina, Dock（新サブタイプ候補）
- Residential↓

**高urbanization・低shelter（開けた海岸沿い）**:
- Commercial (Hotel, Restaurant)
- Public↑ (展望台的)
- Industrial (造船所的)

**低urbanization・高shelter（隠れた入り江）**:
- Residential (漁師小屋的)
- Commercial↓
- 装飾: ボート、漂流物、桟橋

**低urbanization・低shelter（野生の岬/崖）**:
- 建物出現率 大幅↓ (buildingDensity × 0.2)
- Special: 灯台（ランドマーク候補）
- 装飾: 岩、風化した標識

### 因子の可視性

`InteriorBuildingContext` に全因子を保持するので、
デバッグUI / Inspector / 将来のゲーム内表示で
「なぜこの建物がここにあるか」を追跡可能。

```
建物ID: bldg_042
  category: Commercial (Restaurant)
  tier: 1
  isWaterfront: true
  waterfrontDist: 8.2
  waterfrontUrbanization: 0.72  ← 入り江(大) + Grid補正
  waterfrontShelter: 0.81       ← 入り江奥
  nearCoast: true
  nearHill: false
  elevation: 1.3
```

### デフォルメ制御

**WaterProfile に追加**:

| フィールド | 型 | 説明 | デフォルト |
|-----------|-----|------|-----------|
| waterfrontUrbanizationBias | float [-1, 1] | 全ゾーンのurbanizationをシフト | 0 |
| waterfrontBuildingDensityScale | float [0, 2] | 水辺の建物密度倍率 | 1.0 |

- `waterfrontUrbanizationBias = +0.5` → 全ゾーンが港湾寄りに
- `waterfrontUrbanizationBias = -0.5` → 全ゾーンが自然寄りに
- `waterfrontBuildingDensityScale = 0.0` → 水辺に建物なし（現行動作再現）
- `waterfrontBuildingDensityScale = 1.5` → 水辺に建物多め（港町表現）

**MapPreset.generatorType による自動調整**:

| GeneratorType | urbanizationBias | densityScale | 表現 |
|--------------|-----------------|-------------|------|
| Organic | 0 | 1.0 | バランス型 |
| Grid | +0.3 | 1.2 | 人工的港湾 |
| Rural | -0.3 | 0.5 | 自然河川 |
| Mountain | -0.4 | 0.3 | 渓谷・崖 |

### 将来の配置可能オブジェクト

建物以外のオブジェクトも同じ `WaterfrontCharacter` で制御:
- ボート / カヤック (shelter高)
- 漂流物 / 流木 (urbanization低)
- 桟橋 / 浮き桟橋 (urbanization中-高)
- ドック / クレーン (urbanization高)
- 灯台 (shelter低 + 岬)
- ベンチ / 遊歩道 (urbanization高)

これらは DecorationPlacer の拡張として、
DecorationType に水辺装飾を追加する形で実装できる。

### 影響範囲
- 新規: `WaterfrontCharacter` データ構造
- 変更: `InteriorBuildingContext` に4フィールド追加
- 変更: `BuildingClassifier.Classify` に水辺重み付けロジック追加
- 変更: `WaterGenerator` or 新クラスでゾーン割当ロジック
- 変更: `BuildingPlacer` で水辺建物密度制御

---

## 実装優先度の提案

| ID | 名称 | 複雑度 | 視覚インパクト | 探索への影響 | 依存 |
|----|------|--------|---------------|-------------|------|
| W-1 | メタ地理ベース川流路 | 中 | 高 | 高（マップ個性） | なし |
| W-2 | 入り江・岬 | 中 | 高 | 高（発見スポット） | なし |
| W-7 | 水辺配置システム | 中 | 中 | 高（環境の多様性） | W-2 |
| W-5 | 蛇行自動調整 | 低 | 中 | 中（プリセット個性） | なし |
| W-3 | 砂州 | 低 | 中 | 低 | なし |
| W-6 | 河口デルタ | 高 | 高 | 高（探索分岐） | W-1, W-7 |
| W-4 | 浜辺遷移帯 | 中 | 高 | 低 | なし |

**推奨順序**: W-1 → W-2 → W-7 → W-5 → W-3 → W-6 → W-4

理由:
- W-1 で川が自然な方向に流れるようになる → 全プリセットの見た目が改善
- W-2 で海岸線の個性が出る → 探索の発見スポットが増える
- W-7 で水辺に適切な建物が出る → W-2/W-6の入り江・デルタが「生きた場所」になる
- W-5/W-3 は低コストで表現力を追加
- W-6 は W-1+W-7 が揃ってから → デルタが探索的にも視覚的にも意味を持つ
- W-4 はシェーダー変更を伴うので最後
