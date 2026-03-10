# SP-033: SP-032 MVP実装計画

**Status**: partial
**Category**: system
**実装率**: 18% (設計・タスク分解のみ、未実装)

## 目的

`SP-032` を Gate-1 完了後すぐ着手できる粒度まで分解する。
本書は「何をどの順番で実装するか」「どのファイルをどう触るか」「どこまでを MVP とするか」を固定する。

> **Blocker**: Gate-1（P4道路手動検証）が未完了のため、本書は**実装準備専用**である。

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

### Slice 1: Infrastructure

対象:

- `GroundSemanticMaskSet`
- `GroundSurfacePresetDefaults`
- `MapManager` の runtime material lifecycle

完了条件:

- 生成前後で texture / material の破棄責務が明確
- mask 未生成でも従来表示が壊れない

### Slice 2: CPU Baker

対象:

- `GroundSemanticMaskBaker`
- `RoadCurveSampler`

完了条件:

- 同一 seed で 동일な texture を返す
- road / water / building influence が expected UV 位置に焼かれる

### Slice 3: Ground Shader

対象:

- `GridGround.shader`

完了条件:

- hillshade / contour / moisture / road / building tint が動く
- debug grid が default off になる

### Slice 4: Theme Integration

対象:

- `MapTheme`
- `ThemeManager`

完了条件:

- theme 切替で色だけ更新される
- semantic texture の再生成が発生しない

### Slice 5: Validation Hooks

対象:

- debug docs
- optional logging

完了条件:

- manual validation checklist を実行可能
- 破棄漏れ、GC spike、ズレ検出の観点が揃う

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

## 実装開始前の最終確認

- Gate-1 の結果が `PASS`
- `docs/verification/road-p4-gate-results.md` が記録済み
- `SP-032` と本書の保留事項が増えていない
- `MapRenderer.cs` のユーザー変更と道路 sampling 分離が衝突しない

