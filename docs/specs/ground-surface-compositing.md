# SP-032: 地表合成レンダリングパイプライン

**Status**: partial
**Category**: system
**実装率**: 12% (設計統合のみ、未実装)

## 概要

Unity Terrain へ移行せず、既存のプロシージャル地面メッシュを維持したまま、
地表の見た目を「読みやすい航空地図 / 疑似オルソフォト」寄りに更新する。

実装着手時の具体的な順序とファイル変更単位は `SP-033` を参照。

本仕様の主眼は次の4点:

1. 地形の起伏を hillshade と contour で読みやすくする
2. 水辺・市街地・道路沿いの地表変化をセマンティックに表現する
3. 道路・水面・建物の輪郭は既存どおり独立メッシュで鮮明に保つ
4. Theme 切替時にマスク再生成を行わず、色だけ差し替えられる構成にする

> **Gate note**: Gate-1（P4道路手動検証）完了前は本仕様の**実装着手は行わない**。
> 本書は Gate 待ち期間に行う設計統合メモである。

---

## 固定制約

- Ground は `CurrentElevationMap` から生成するプロシージャルメッシュを維持する
- Unity Terrain / TerrainLit / splatmap ベースへは移行しない
- Roads は `MapRenderer` の procedural mesh を維持する
- Water は `WaterRenderer` の separate mesh を維持する
- Buildings は `BuildingSpawner` の独立 geometry を維持する
- マスクは見た目用の semantic input であり、道路/水/建物の最終シルエットを置き換えない
- マスク生成は seed 決定論・CPU 側前処理・`Texture2D` を基本とする

---

## 設計判断

### 1. 地表は「carrier mesh + semantic mask + compositing shader」に再設計する

現状の Ground は「高さ付きメッシュ + 即席色付け」だが、今後は次の役割分担に変更する。

- Ground mesh: 高さ、衝突、NavMesh の carrier
- CPU 前処理: 標高・傾斜・水辺・道路・建物の semantic mask 生成
- `GridGround.shader`: 各 mask を合成して地表色・hillshade・contour を出力

### 2. エッジの鮮明さは geometry 側で守る

道路、水面、建物は mask に焼かず、既存の mesh 表現を主表示として維持する。
Ground 側はそれらの周辺に接地感・湿り・摩耗などを補助的に返す。

### 3. MVP と Ideal を分離する

- **MVP**: 単一 Ground mesh を維持しつつ解像度を引き上げ、2枚の CPU mask で地表合成を行う
- **Ideal**: Ground chunking + 高解像度 mask + 距離場ベースの influence を導入する

---

## データフロー

```text
Seed / Preset
  -> IMapGenerator.Generate
  -> TerrainGenerator.Generate
  -> ElevationMap
  -> WaterGenerator.Generate
  -> WaterTerrainInteraction.ApplyWaterCarving
  -> BuildingPlacer.Place
  -> BridgeTunnelDetector.Detect
  -> GroundSemanticMaskBaker.Bake
       -> Height / slope / curvature
       -> Moisture / shore
       -> Road influence / intersection boost / layer exceptions
       -> Building footprint / halo / density
       -> Packed Texture2D outputs
  -> MapManager binds textures to ground material
  -> GenerateGroundMesh / EnsureGroundPlane
  -> MapRenderer.Render (roads)
  -> WaterRenderer.Render (water)
  -> BuildingSpawner.Spawn (buildings)
```

### 呼び出し位置

`MapManager.Generate()` 内で `BridgeTunnelDetector.Detect(...)` の後、`EnsureGroundPlane()` の前に
`GroundSemanticMaskBaker` を挿入する。

---

## Mesh-driven と Mask-driven の責務分離

### Mesh-driven のまま維持するもの

- 地面の実際の高低差
- 道路の輪郭、交差点形状、橋脚、深度関係
- 水面の輪郭、汀線、透明度/深度表現
- 建物のシルエット、接地位置、深度

### Mask-driven に移行するもの

- hillshade
- subdued elevation tint
- readable contour
- moisture / shore tint
- road influence tint
- building influence tint
- 近景/遠景の読ませ方

---

## Ground geometry 方針

### 現状評価

現状の `GroundGridRes = 40` は遠景の大局形状には足りるが、次の用途では不足する。

- close camera inspection
- hillshade の安定性
- contour の滑らかさ
- 道路/水際周辺での地面だけ粗く見える問題の緩和

### 推奨

- **MVP**: Ground は単一メッシュ維持。ただし解像度は `40 -> 96` を第一候補とする
- **Ideal**: 2x2 以上の chunk ground に移行し、固定 meters-per-vertex で管理する

### chunking の採用理由

- 将来の高解像度化とカリングに向く
- semantic mask のタイル化と相性が良い
- NavMesh / collider 更新の局所化に繋げやすい

---

## 派生 mask 一覧（理想形）

| ID | 名称 | 用途 | 主ソース |
|----|------|------|---------|
| M01 | ElevationNorm | 高度帯ブレンド | `ElevationMap.Sample` |
| M02 | SlopeNorm | 斜面強度 | `ElevationMap.SampleSlope` |
| M03 | CurvatureSigned | 谷/尾根の補助判定 | `ElevationMap` 2次差分 |
| M04 | HillProfileInfluence | `SlopeProfile` 由来の地形キャラ | `terrain.hills`, `hillClusters` |
| M05 | RoadHardCoverage | 道路直下の明確な影響域 | `MapEdge`, `RoadProfile.TotalWidth` |
| M06 | RoadSoftShoulderDistance | 路肩の汚れ / 摩耗の減衰 | M05距離場 |
| M07 | RoadTierWeight | tier 差の重み | `edge.tier` |
| M08 | IntersectionBoost | 交差点拡張域 | node degree, `intersectionRadiusFactor` |
| M09 | RoadLayerException | 橋/トンネル例外 | `edge.layer` |
| M10 | WaterHardCoverage | 水面近傍の影響域 | `waterBodies` |
| M11 | ShorelineProximity | 汀線距離 | coast polygon 境界距離 |
| M12 | RiverCorridorMoisture | 川沿い湿潤コリドー | river centerline + width/depth |
| M13 | CoastTransition | 海岸遷移幅 | `coastSide`, carve radius |
| M14 | BuildingFootprintHard | 建物直下の占有域 | `MapBuilding` footprint |
| M15 | BuildingHaloSoft | 建物外周の接地ハロー | M14距離場 |
| M16 | BuildingDensityField | 市街地密度の補助場 | building 分布集計 |

---

## MVP の packed texture 構成

MVP では Worker 提案を統合し、**2枚の CPU 生成 mask + 1枚の固定ノイズ**にまとめる。

### `_GroundHeightSlopeTex` (RGBA8, CPU生成)

- `R`: `ElevationNorm`
- `G`: `SlopeNorm`
- `B`: `CurvatureSigned`（未使用期間は reserved 可）
- `A`: contour jitter / low-frequency noise

### `_GroundSemanticTex` (RGBA8, CPU生成)

- `R`: moisture / shore influence
- `G`: road influence
- `B`: building influence
- `A`: intersection boost

### `_MacroNoiseTex` (固定 asset or 共有 texture)

- `R`: 低周波色むら
- `G`: 中周波ノイズ
- `B`: 微細ざらつき
- `A`: domain warp 補助

### MVP で内部的に吸収する要素

以下は独立 texture にせず、`_GroundSemanticTex` 各 channel に bake 時点で統合する。

- `RoadSoftShoulderDistance`
- `RoadTierWeight`
- `RoadLayerException`
- `WaterHardCoverage`
- `ShorelineProximity`
- `BuildingFootprintHard`
- `BuildingHaloSoft`

---

## Ideal の packed texture 構成

### `_GroundMask0` (RGBA8)
- `M01 / M02 / M03 / M04`

### `_GroundMask1` (RGBA8)
- `M05 / M06 / M07 / M08`

### `_GroundMask2` (RGBA8)
- `M10 / M11 / M12 / M13`

### `_GroundMask3` (RGBA8)
- `M14 / M15 / M16 / M09`

> Theme 切替では上記 texture を再生成しない。色と強度だけ差し替える。

---

## Shader 設計（`GridGround.shader` 更新方針）

### keep

- `_BaseColor`
- `_GridColor`
- `_GridSize`
- `_GridOpacity`

### rename

- `_MidColor` -> `_ElevTintLowColor`
- `_HighColor` -> `_ElevTintHighColor`
- `_ElevMidThreshold` -> `_ElevTintStart`
- `_ElevHighThreshold` -> `_ElevTintEnd`
- `_ElevBlendRange` -> `_ElevTintFeather`
- `_SlopeColor` -> `_SlopeTintColor`
- `_SlopeThreshold` -> `_SlopeTintStart`

### add

**Color**
- `_ContourColor`
- `_HillshadeShadowColor`
- `_MoistureTintColor`
- `_RoadInfluenceColor`
- `_BuildingInfluenceColor`

**Scalar**
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
- `_NearDetailStrength`
- `_FarFlattenStrength`
- `_NearStart`
- `_NearEnd`
- `_GridMode`
- `_AnalysisGridOpacity`

**Vector**
- `_MapWorldSize` (`x=worldWidth, y=worldHeight, z=1/worldWidth, w=1/worldHeight`)
- `_MaskTexelSize`

**Texture**
- `_GroundHeightSlopeTex`
- `_GroundSemanticTex`
- `_MacroNoiseTex`

### フラグメント処理順

1. `positionWS.xz` から map UV を計算
2. `_GroundHeightSlopeTex`, `_GroundSemanticTex`, `_MacroNoiseTex` をサンプル
3. base ground color
4. subdued elevation tint
5. cartographic hillshade
6. readable contour
7. moisture / shore tint
8. road influence tint
9. building influence tint
10. near/far readability blend
11. optional debug / analysis grid overlay
12. fog

### 効果分類

**world-space only**
- カメラ距離ブレンド
- fog
- debug grid overlay

**texture-driven**
- moisture / shore
- road influence
- building influence
- macro color variation

**hybrid**
- hillshade
- contour
- elevation tint

### 計算戦略

**hillshade**
- `_GroundHeightSlopeTex.R` の近傍4tapから法線相当を再構成
- `azimuth / altitude` は地形読解用の固定光源として扱う
- 実シーン lighting とは分離し、読図性を優先する

**readable contour**
- `ElevationNorm` を基準に `major/minor` へ発展可能な形で実装
- MVP は単一 contour、Ideal で `major interval` を追加
- jitter は `_GroundHeightSlopeTex.A` を利用し、機械的な均一感を弱める

**moisture / shore tint**
- `_GroundSemanticTex.R` を利用
- 川沿い湿潤、海岸側遷移、岸辺暗化を 1 channel へ集約

**road influence tint**
- `_GroundSemanticTex.G` を利用
- 道路直下は濃すぎないよう抑え、主に路肩と道路沿いの地面差を出す

**building influence tint**
- `_GroundSemanticTex.B` を利用
- footprint のハードマスクではなく、接地リングの読ませ方を主役にする

### debug grid の扱い

- `_GridMode = 0`: デフォルト無効
- `_GridMode = 1`: Debug Overlay
- `_GridMode = 2`: Analysis Overlay

二段階グリッドは常設アートではなく、解析支援レイヤに降格する。

---

## CPU 前処理責務（`GroundSemanticMaskBaker`）

### 役割

- `ElevationMap` を地表 texture 解像度でサンプリング
- 道路中心線と交差点を shader 用 influence にラスタライズ
- 水辺距離と湿潤帯をラスタライズ
- 建物 footprint / halo / density をラスタライズ
- packed `Texture2D` を生成し、`MapManager` 経由で ground material にバインド

### 解像度戦略

```text
baseRes = clamp(nextPow2(max(worldWidth, worldHeight) / 1.25), 512, 2048)
```

MVP は 512-1024 を基本とし、最狭道路幅に対して最低 4 texel を確保する。

### update timing

- `MapManager.Generate()` 内で `CurrentMap` 確定後に再生成
- 同一 seed / preset / profile では決定論的に同一結果
- Theme-only change では再生成しない

### cache key

```text
seed + preset GUID + roadProfile GUID + waterProfile GUID + maskVersion
```

---

## Theme 連携方針

### Theme-driven に残すもの

- `_BaseColor`
- `_ElevTintLowColor`
- `_ElevTintHighColor`
- `_ContourColor`
- `_GridColor`
- `_MoistureTintColor`
- `_RoadInfluenceColor`
- `_BuildingInfluenceColor`

### Theme-independent に保つもの

- `_GroundHeightSlopeTex`
- `_GroundSemanticTex`
- `_MacroNoiseTex`
- mask 由来のしきい値・強度のうち preset 起因のもの

`ThemeManager.ApplyGround()` は色の差し替え担当に留め、semantic texture 再生成を行わない。

---

## ファイル別変更計画

### `Assets/Scripts/Runtime/MapManager.cs`

- `GroundSemanticMaskBaker` 呼び出しを追加
- ground material への texture / parameter bind を追加
- Ground mesh 解像度の引き上げ、将来的な chunk 化の入口を用意
- runtime 生成 texture の破棄責務を `Clear()` に追加

### `Assets/Scripts/Runtime/MapRenderer.cs`

- 道路ラスタ化用の sampling 規則を共有化
- `edge.tier`, `edge.layer`, `intersectionRadiusFactor`, `RoadProfile.TotalWidth` を
  baker が再利用できるようにする

### `Assets/Shaders/GridGround.shader`

- world-aligned UV + packed mask compositing へ更新
- hillshade / contour / moisture / road / building influence を主責務化
- dual grid は debug / analysis overlay モードへ移行

### `Assets/Scripts/Runtime/ThemeManager.cs`

- ground 用 color parameter の適用先を拡張
- semantic mask を触らず、色だけ差し替えるよう責務を明文化

### `SPEC.md`

- 実装着手後に pipeline へ `GroundSemanticMaskBaker` を追加
- Ground 描画方針を「carrier mesh + semantic masks + compositing shader」に更新
- `SP-032` への参照を追加

### `docs/debug-setup.md`

- 実装後の検証項目として hillshade / contour / moisture / edge integration を追加
- `Debug grid` と `default art style` を明確に分離する

---

## MVP 実装順

1. `GroundSemanticMaskBaker` を追加し、`_GroundHeightSlopeTex` / `_GroundSemanticTex` を出力
2. `MapManager.Generate()` に前処理ステージを追加
3. `GridGround.shader` を mask 合成型へ更新
4. `ThemeManager` の ground 適用を色中心に整理
5. `GroundGridRes` を引き上げて近景破綻を緩和

---

## 検証チェックリスト（実装後）

- [ ] 同一 seed で semantic mask の結果が一致する
- [ ] Theme 切替時に mask は再生成されず、見た目だけ変わる
- [ ] 道路縁 / 水際 / 建物基部で補助表現がにじみ過多にならない
- [ ] Ground / Road / Water の Z-fighting が発生しない
- [ ] hillshade が主照明条件に依存しすぎず、常に起伏読解を助ける
- [ ] contour が平地でうるさすぎず、斜面で読める
- [ ] 4プリセット × 2テーマで大破綻がない
- [ ] `Generate()` 再実行時の GC / フレーム落ちが許容範囲
- [ ] NavMesh 再Bake後に歩行不能領域が増えていない

---

## 保留

- Ground chunking をいつ MVP 後に有効化するか
- Ground 専用 profile（`GroundSurfaceProfile` SO）を導入するか
- `major/minor contour` の2段実装を初回から入れるか
- 橋 / トンネル例外 (`M09`) を MVP の 2枚構成へ含めるか
