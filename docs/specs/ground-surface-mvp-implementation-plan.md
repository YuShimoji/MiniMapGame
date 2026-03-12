# SP-033: SP-032 MVP実装計画

**Status**: partial
**Category**: system
**実装率**: 85% (Slice 1-4 実装済み、仕様同期済み。Slice 5残: 色パレット調整・手動検証)

## 目的

`SP-032` を Gate-1 完了後すぐ着手できる粒度まで分解する。
本書は「何をどの順番で実装するか」「どのファイルをどう触るか」「どこまでを MVP とするか」を固定する。

> Gate-1 は art direction 依存で保留。SP-032 MVP は先行着手し Slice 1-4 完了。

---

## MVP の範囲

### やること

- Ground は単一 mesh のまま維持する
- `GroundGridRes` は MVP で `40 -> 96` を第一候補とする
- CPU 生成の semantic mask を 2 枚導入する
- `GridGround.shader` を world-aligned compositing shader に更新する
- Theme 切替は色だけを差し替え、mask 再生成は行わない
- 道路 / 水 / 建物は既存どおり separate mesh を維持する

### やらないこと

- Ground chunking
- adaptive subdivision
- 4枚構成の ideal packed texture
- ground 専用 profile SO
- runtime での地表状態変化に応じた RenderTexture 更新

---

## MVP の確定判断

### 1. chunking は MVP から外す

理由:

- `SP-032` の本質は地表 shading の刷新であり、geometry 分割は二段目でも成立する
- chunking を同時導入すると collider / NavMesh / clear lifecycle の検証軸が増えすぎる
- Gate-1 直後に着手する最初のスライスは「見た目の主改善」を優先すべき

### 2. preset 別の強度は一旦 code-default で持つ

MVP では `MapPreset` に新規 float 群を増やさない。
代わりに `generatorType` ベースの resolver で ground tuning を返す。

理由:

- 4 preset asset の一斉 migration を避けられる
- Gate-1 後の最初の検証で調整対象が少ない
- tuning の妥当性が確認できてから SO 化したほうが安全

### 3. theme は color のみ責務を持つ

`MapTheme` には ground 用の色を追加するが、強度係数は持たせない。
強度は preset / resolver 側で管理する。

---

## 新規追加するファイル（MVP）

### `Assets/Scripts/Runtime/GroundSemanticMaskSet.cs`

責務:

- runtime 生成した ground 用 texture を束ねる
- lifecycle を `MapManager.Clear()` から破棄できるようにする

想定 API:

```csharp
namespace MiniMapGame.Runtime
{
    public sealed class GroundSemanticMaskSet
    {
        public Texture2D HeightSlopeTexture { get; }
        public Texture2D SemanticTexture { get; }
        public int Resolution { get; }

        public void DestroyTextures();
    }
}
```

### `Assets/Scripts/Runtime/GroundSemanticMaskBaker.cs`

責務:

- `MapData`, `ElevationMap`, `MapPreset`, `RoadProfile` から MVP 用 2 枚 mask を生成する

想定 API:

```csharp
namespace MiniMapGame.Runtime
{
    public static class GroundSemanticMaskBaker
    {
        public static GroundSemanticMaskSet Bake(
            MapData map,
            ElevationMap elevationMap,
            MapPreset preset,
            RoadProfile roadProfile,
            int bezierSegments,
            float intersectionRadiusFactor,
            int maskResolution);
    }
}
```

### `Assets/Scripts/Core/RoadCurveSampler.cs`

責務:

- `MapRenderer` と `GroundSemanticMaskBaker` が共通で使う道路ベジェサンプリングを提供する
- 「描画とマスクで道路の見え方がずれる」事故を防ぐ

想定 API:

```csharp
namespace MiniMapGame.Core
{
    public static class RoadCurveSampler
    {
        public static void Sample2D(
            MapEdge edge,
            List<MapNode> nodes,
            int segments,
            List<Vector2> outPoints);
    }
}
```

### `Assets/Scripts/Runtime/GroundSurfacePresetDefaults.cs`

責務:

- `generatorType` ごとの ground tuning を返す
- MVP の preset 差分を asset migration なしで制御する

想定戻り値:

- hillshade strength
- contour strength
- moisture strength
- road influence strength
- building influence strength
- near/far blend distances

---

## 既存ファイルの変更計画

### `Assets/Scripts/Runtime/MapManager.cs`

#### 追加フィールド

```csharp
[Header("Ground Surface")]
public int groundGridResolution = 96;
public int groundMaskResolution = 1024;
public Texture2D macroNoiseTexture;
```

#### 追加 private fields

```csharp
private Material _groundMaterialInstance;
private GroundSemanticMaskSet _groundMaskSet;
```

#### 追加メソッド

- `PrepareGroundMaterialInstance()`
- `ApplyGroundSurfaceTextures()`
- `ReleaseGroundSurfaceResources()`

#### 変更点

- `BridgeTunnelDetector.Detect(...)` の後に mask bake を追加
- `EnsureGroundPlane()` の material は `_groundMaterialInstance` を使用
- `Clear()` で `_groundMaskSet` と `_groundMaterialInstance` を破棄
- `GenerateGroundMesh()` は `groundGridResolution` を使うよう変更

#### 重要判断

`groundMaterial` は template asset として保持し、runtime では clone した material instance を使う。

理由:

- semantic texture を asset 側へ汚染しない
- Theme 切替時に runtime instance へ安全に反映できる

### `Assets/Scripts/Runtime/MapRenderer.cs`

#### 変更方針

- 道路描画責務は維持
- 道路 shape を別用途でも再利用できるよう sampling ロジックのみ分離

#### 具体

- `GenerateRoadStrip` 内で使っているベジェサンプリングロジックを
  `RoadCurveSampler` へ寄せる
- `intersectionRadiusFactor` と `bezierSegments` は `MapManager -> GroundSemanticMaskBaker`
  の入力として再利用する

### `Assets/Shaders/GridGround.shader`

#### プロパティ契約

MVP 時点で最低限必要な texture:

- `_GroundHeightSlopeTex`
- `_GroundSemanticTex`
- `_MacroNoiseTex`

MVP 時点で最低限必要な color:

- `_BaseColor`
- `_ElevTintLowColor`
- `_ElevTintHighColor`
- `_ContourColor`
- `_MoistureTintColor`
- `_RoadInfluenceColor`
- `_BuildingInfluenceColor`
- `_GridColor`

MVP 時点で最低限必要な float:

- `_ElevTintStrength`
- `_HillshadeStrength`
- `_HillshadeAzimuthDeg`
- `_HillshadeAltitudeDeg`
- `_HillshadeAmbient`
- `_ContourInterval`
- `_ContourWidth`
- `_ContourStrength`
- `_ContourJitter`
- `_MoistureStrength`
- `_RoadInfluenceStrength`
- `_BuildingInfluenceStrength`
- `_NearStart`
- `_NearEnd`
- `_FarFlattenStrength`
- `_GridMode`
- `_AnalysisGridOpacity`

#### 実装順

1. worldXZ -> mapUV 変換追加
2. packed texture サンプル追加
3. elevation tint + hillshade + contour
4. moisture / road / building influence
5. debug grid を optional overlay 化

### `Assets/Scripts/Data/MapTheme.cs`

#### 追加候補フィールド

```csharp
[Header("Ground Surface")]
public Color groundElevationLowColor;
public Color groundElevationHighColor;
public Color groundContourColor;
public Color groundMoistureTintColor;
public Color groundRoadInfluenceColor;
public Color groundBuildingInfluenceColor;
```

#### 判断

- `groundColor` は base color として残す
- strength は追加しない

### `Assets/Scripts/Runtime/ThemeManager.cs`

#### 変更方針

- theme の ground 適用先を `mapManager.groundMaterial` ではなく runtime instance 優先にする
- semantic texture は触らず、color parameter のみ更新する

#### 必要な調整

- `MapManager` 側から active ground material instance を取得する accessor を追加する
- `ApplyGround()` は null-safe に template / runtime の両方へ適用する

---

## MVP の baked channel 確定版

### `_GroundHeightSlopeTex`

- `R`: normalized elevation
- `G`: normalized slope
- `B`: curvature signed remap (`0.5 = flat`)
- `A`: contour jitter

### `_GroundSemanticTex`

- `R`: moisture / shore
- `G`: road influence
- `B`: building influence
- `A`: intersection boost

### channel への bake ルール

- road shoulder / tier weight / bridge-tunnel exception は `G` に統合
- water hard coverage / shoreline proximity は `R` に統合
- building footprint / halo は `B` に統合
- degree>=3 node の拡張のみ `A` に分離

---

## 実装スライス

### Slice 1: Infrastructure — DONE

対象:

- `GroundSemanticMaskSet` — 新規作成済み
- `GroundSurfacePresetDefaults` — 新規作成済み
- `MapManager` の runtime material lifecycle — 実装済み

実装内容:

- `MapManager` に `groundGridResolution=96`, `groundMaskResolution=1024` フィールド追加
- `_groundMaterialInstance` で共有マテリアル汚染を防止
- `CurrentMaskSet` プロパティで外部からマスク参照可能
- `Clear()` でテクスチャ・マテリアルインスタンスを破棄

### Slice 2: CPU Baker — DONE

対象:

- `GroundSemanticMaskBaker` — 新規作成済み
- `RoadCurveSampler` — 新規作成済み

実装内容:

- 道路ベジエポリライン事前計算 + PointToSegmentDistance
- 交差点（degree>=3ノード）の位置・半径事前計算
- 水辺ポイント事前計算（pathPoints を間引きサンプル）
- 全テクセルで elevation/slope/curvature/jitter + moisture/road/building/intersection を計算
- `Texture2D.Apply(false, true)` で GPU-only 化

### Slice 3: Ground Shader — DONE

対象:

- `GridGround.shader` — 全面書き換え済み

実装内容:

- 13 ステップ合成パイプライン（SP-032 Shader 設計セクション参照）
- mesh UV でマスクテクスチャをサンプル（worldXZ→UV 変換は不要）
- hillshade は `_GroundHeightSlopeTex_TexelSize` ベースの 4-tap
- dual-scale grid は距離フェード付きで常時表示（debug mode 未導入）

実装との差分:

- `_MacroNoiseTex` は MVP では省略
- `_GridMode` は未実装（grid は常時表示 + distance fade）
- property 名は仕様提案から簡略化（`_ElevTintLowColor` → `_MidColor` 等）

### Slice 4: Theme Integration — DONE

対象:

- `MapTheme` — 7 色フィールド追加済み
- `ThemeManager` — マテリアルインスタンス対応済み

実装内容:

- `MapTheme` に `groundMidColor`, `groundHighColor`, `groundSlopeColor`,
  `groundMoistureTint`, `groundRoadTint`, `groundBuildingTint`, `groundContourColor` 追加
- `ThemeManager.ApplyGround()` は `GetGroundMaterialInstance()` で runtime instance 優先
- strength は theme に持たせず `GroundSurfacePresetDefaults` で管理

実装との差分:

- 仕様提案の `groundElevationLowColor` / `groundElevationHighColor` は
  `groundMidColor` / `groundHighColor` に名称変更

### Slice 5: Validation Hooks — PENDING

対象:

- debug docs 更新
- optional logging
- 色パレット調整

残タスク:

- 既存 MapTheme SO アセットの Ground Surface Compositing 値を Inspector で更新
- 4 preset × 2 theme の手動検証
- Console ログ確認（mask bake 時間、NavMesh bake 時間）

---

## 受け入れ条件

### Functional

- 4 preset すべてで ground が表示される
- 道路 / 水 / 建物の silhouette が現状より甘くならない
- hillshade により高低差が読みやすくなる
- contour が視認できるが過剰にうるさくない
- 水辺・道路沿い・建物基部の補助表現が乗る

### Technical

- 同一 seed で baked texture が決定論的に一致する
- theme 切替で baked texture は不変
- `MapManager.Clear()` 後に texture / material instance が残らない
- `Generate()` の追加コストが過大にならない

### Regression

- NavMesh bake が継続動作する
- 道路描画・建物配置・水面表示に副作用がない
- Grid preset で地形が平坦でも shading が破綻しない

---

## 手動検証項目（実装後）

- [ ] Coastal / Rural / Grid / Mountain で hillshade 強度が妥当
- [ ] 水辺 tint が水上ではなく岸側に出る
- [ ] 道路 influence が道路本体を汚しすぎない
- [ ] building halo が広がりすぎない
- [ ] debug grid off で常設のグリッド感が消える
- [ ] near/far の読ませ方が破綻しない

---

## 実装開始時の推奨順

1. `MapManager` の runtime material / texture lifecycle 固定
2. `GroundSemanticMaskBaker` の MVP 2枚出力
3. `GridGround.shader` の texture 受け取りと合成
4. `ThemeManager` / `MapTheme` の色連携
5. ドキュメントの実装済み反映

---

## 実装開始前の最終確認（実施済み）

- ~~Gate-1 の結果が `PASS`~~ → Gate-1 は保留、SP-032 を先行実装
- ~~`docs/verification/road-p4-gate-results.md` が記録済み~~ → 保留
- `SP-032` と本書の保留事項が増えていない → OK
- `MapRenderer.cs` のユーザー変更と道路 sampling 分離が衝突しない → `RoadCurveSampler` で分離済み

