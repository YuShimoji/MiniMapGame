# SP-040: マップビジュアルディレクション

**Status**: partial
**Category**: system
**上位参照元**: SPEC.md, CLAUDE.md Key Decisions
**下位実装仕様**: SP-032 (地表合成レンダリングパイプライン)

---

## 1. Executive Summary

本プロジェクトのマップ描画が目指す最終画は **hybrid orthophoto relief** である。
航空写真風の連続した地表感を持ちつつ、道路・水辺・建物・地形輪郭が clean edge で読め、
高低差や地勢がグラデーションと陰影で直感的に把握できる「可読性を保った擬似正射・擬似地形陰影」を北極星とする。

本文書 (SP-040) はマップ描画の **上位方針** を定める。
個別の実装仕様（地表合成パイプラインの shader 構成、mask チャンネル割り当て、CPU 前処理責務）は SP-032 を参照。
SP-040 は思想・制約・責務分離・評価軸・ロードマップを扱い、SP-032 はそれを地表レイヤーで具現化する実装仕様である。
将来の道路ビジュアル仕様・建物ビジュアル仕様もこの SP-040 の下位に位置する。

---

## 2. Current-State Diagnosis

### 2.1 現在の SSOT 構造

| 要素 | SSOT | 責務 |
|------|------|------|
| 道路ネットワーク | `MapEdge` (nodes, tier, layer) + `RoadProfile` SO | ジェネレータが生成、`MapRenderer` がメッシュ化、`Road.shader` が描画 |
| 地形 | `ElevationMap` (hills + carving) | `TerrainGenerator` が生成、全コンポーネントがサンプル |
| 水面 | `WaterBodyData` (typed bodies) + `WaterProfile` SO | `WaterGenerator` が生成、`WaterRenderer` がメッシュ化 |
| 建物 | `MapBuilding` (footprint, floors, category) | `BuildingPlacer` が配置、`BuildingSpawner` がメッシュ化 |
| 地表合成 | `GroundSemanticMaskBaker` → 2枚 packed texture | CPU 前処理で焼き、`GridGround.shader` が合成 |
| テーマ | `MapTheme` SO (色パレット) | `ThemeManager` が全 material に色を差し替え |
| デバッグ | `AnalysisVisualizer` (Tab 切替) | `LineRenderer` で Analysis/Terrain 層を描画 |

### 2.2 構造上のボトルネック

1. **北極星の不在**: 「読みやすい航空地図 / 疑似オルソフォト」という方向性は SP-032 に記載されているが、プロジェクト全体のビジュアル目標として正式に定義されていない。道路・水面・建物の各 shader が独立に「それらしい見た目」を追求しており、統一された画作り基準がない。

2. **木漏れ日・建物陰影の未定義**: 装飾（Tree 等）の下に現れるべき光斑表現と、建物基部の接地感表現が未設計。現状は `BuildingFade.shader` の roof-fade のみで、地面に落ちる影の擬似表現がない。

3. **道路確認性の体系化不足**: 道路は `MapEdge` → Bezier → strip mesh → `Road.shader` で描画されるが、「道路確認性」という観点での評価基準が未定義。line-art 的表示と aerial composite 的表示の切替手段がない。

4. **モバイル予算の未定義**: URP 前提だが、ターゲットプロファイル（端末クラス・FPS 目標・draw call 予算）が明文化されていない。各 shader が `_MAIN_LIGHT_SHADOWS_CASCADE` を含んでおり、モバイルでの予算判断が個別に散在している。

5. **デバッグ表示と本番表示の境界が曖昧**: `GridGround.shader` の dual-scale grid は SP-032 で「Ideal で debug overlay に降格予定」とされているが、現状は常時表示。art style と debug overlay が同一 pass で混在している。

---

## 3. Target Visual Taxonomy

### 3.1 候補比較

| 用語 | 定義 | 長所 | 短所 | 適合度 |
|------|------|------|------|--------|
| **topographic orthophoto composite** | 正射投影写真 + 地形情報を合成した画像 | 地形の読みやすさを正面に置く | 「写真」の印象が強く、本プロジェクトの procedural mesh ベースとの乖離がある | 中 |
| **shaded orthophoto with vector edge overlay** | 陰影付き正射画像 + ベクトルエッジ重畳 | エッジの鮮明さを明示。道路・建物の輪郭を独立レイヤーで保証する思想と一致 | 名称が長い。「overlay」が後付け感を与える | 中 |
| **hybrid orthophoto relief** | 正射投影 + 地形陰影 + clean edge のハイブリッド | 地表の連続感、地勢の読みやすさ、エッジの鮮明さを1語で包含。procedural mesh ベースの「擬似正射」にも適用可能 | 既存の地図学用語としてはマイナー | 高 |

### 3.2 推奨デフォルト

**hybrid orthophoto relief** を本プロジェクトの北極星用語として推奨する。

理由:
- 本プロジェクトは写真を合成するのではなく、procedural mesh + semantic mask + compositing shader で擬似的に正射画像を構成する。「hybrid」がこの構成的性質を表す
- 地形陰影（hillshade + contour）と clean edge（独立 mesh geometry）の両方を1語で含む
- 「relief」が地勢の立体感を明示し、平面的な地図との差別化を図る

### 3.3 一行定義

> 可読性を保った擬似正射・擬似地形陰影による航空写真風連続地表。道路・水辺・建物は独立 geometry で clean edge を守り、地表は semantic mask 合成で地勢・湿潤・都市化を読ませる。

### 3.4 これは何ではないか

- **写実の模倣ではない**: フォトリアルな地表テクスチャ、PBR ベースの地面表現、衛星写真の貼り付けを目指さない
- **ミニマルな記号地図ではない**: 色面だけの平坦な地図表現でもなく、整理された情報密度を持つ「上品な地図的画面」を目指す
- **Unity Terrain ベースではない**: splatmap や TerrainLit は使わない。procedural mesh + shader compositing を維持する
- **点群・プリミティブ散布が主表現ではない**: 散布物は補助表現として存在するが、画面の情報密度を担うのは面と線である

---

## 4. Basic Principles

本プロジェクトで主権を持つのは、粒ではなく位相である。

### 4.1 道路は network である

道路は scatter された物体の集合ではなく、中心線・接続・階層（tier）・層（layer）を持つネットワークである。
`MapEdge` の node 接続と `RoadProfile` の幅・車線構成が SSOT であり、`Road.shader` の UV 駆動描画がそれを忠実に可視化する。

### 4.2 地形は field である

地形は単なる色面ではなく、高度・傾斜・曲率・湿潤・稜線・谷線を含む連続場である。
`ElevationMap` が SSOT であり、`GroundSemanticMaskBaker` がその場をテクスチャ空間にラスタライズし、`GridGround.shader` が合成する。

### 4.3 建物は edge object である

建物は装飾物ではなく、輪郭・接地位置・影響圏を持つ境界オブジェクトである。
`MapBuilding` の footprint と floors が SSOT であり、`BuildingSpawner` が独立 geometry として生成し、`BuildingFade.shader` が近接時の屋根透過を制御する。

### 4.4 ground は compositing surface である

ground は背景ではなく、複数のセマンティック情報を合成する合成面である。
高度帯、傾斜、湿潤、道路影響、建物影響、等高線、hillshade を1つの shader pass で重ね合わせる。SP-032 の 13 ステップ合成がこの原則の実装である。

### 4.5 polygon は意味論の source である

polygon（mesh の面）は画面に塗るためだけの存在ではなく、意味論・領域性・属性分布の source である。
道路 mesh は「車線数と幅」を、水面 mesh は「深度と粗さ」を、建物 mesh は「カテゴリと階数」を UV・vertex color で運ぶ。

---

## 5. Negative Definition

以下を明確に禁止する。

1. **点群・小物の量で情報不足をごまかすこと**: 装飾（Tree, Bench 等）は環境の手がかりとして存在するが、画面の情報密度の主役にはしない。地表と線形要素が情報を担う。

2. **道路や建物の輪郭を ground texture に焼き込んで濁らせること**: 道路・建物の最終シルエットは独立 mesh geometry が守る。ground 側は接地感・影響域の補助表現に留める（SP-032「エッジの鮮明さは geometry 側で守る」）。

3. **道路確認性を犠牲にして「雰囲気だけの画」へ寄せること**: 道路の centerline、tier 差、交差点形状が常に視認可能でなければならない。atmospheric な演出が道路の読みを阻害する場合、演出側を抑制する。

4. **モバイル要件を無視して高価な動的影や複雑なポストプロセスへ逃げること**: 陰影表現は baked-like / mask-driven / projected tint を優先する。full cascade shadow や SSAO はモバイル budget 内でのみ検討する。

5. **Terrain への全面移行を前提に提案すること**: 現在の procedural / polygon / generated mesh ベースを維持する。Unity Terrain / TerrainLit / splatmap は採用しない。

6. **「リアルにする」だけの曖昧な方針**: ビジュアル改善の提案は、データ構造とレンダリング責務への接続を必ず伴うこと。「もっとリアルに」「雰囲気を出す」等の曖昧語だけでは不可。

---

## 6. Rendering Architecture

### 6.1 Ground Compositing Layer

**表現**: 地表の連続した色面。標高グラデーション、傾斜ティント、hillshade、等高線、水辺・道路・建物の接地影響。

**データソース**: `ElevationMap` → `GroundSemanticMaskBaker` → `_GroundHeightSlopeTex` + `_GroundSemanticTex`

**baked**: 標高・傾斜・曲率・水辺距離・道路距離・建物距離（CPU 前処理、seed 決定論的）
**動的計算**: hillshade ドット積、等高線 frac 判定、distance fade（shader 内）
**独立メッシュ**: 単一 ground plane（解像度 96x96、ElevationMap 追従）

**実装仕様**: SP-032 参照

### 6.2 Road / Water / Building Linear Geometry Layer

**表現**: 道路の車線・標示・路面質感。水面の深度・波・泡。建物のシルエット・屋根透過。

**データソース**:
- 道路: `MapEdge` + `RoadProfile` SO → `MapRenderer` → strip mesh → `Road.shader`
- 水面: `WaterBodyData` + `WaterProfile` SO → `WaterRenderer` → ribbon/polygon mesh → `Water.shader`
- 建物: `MapBuilding` → `BuildingSpawner` → box/L/cylinder/stepped mesh → `BuildingFade.shader`

**baked**: なし（全て procedural mesh 生成）
**動的計算**: UV 駆動の車線標示、深度ベースの水色ブレンド、屋根 fade のプレイヤー距離判定
**独立メッシュ**: 道路は tier×layer 別 batch、水面はタイプ別、建物は個別 GO

### 6.3 Annotation / Decal Layer

**表現**: 木漏れ日、建物接地影、将来的なラベル・マーカー。

**データソース**: Tree 位置（`DecorationPlacer`）、建物 footprint（`MapBuilding`）

**baked**: TBD（木漏れ日・建物陰影の手法選定による）
**動的計算**: TBD
**独立メッシュ**: decal quad / projected tint（手法選定後に決定）

**現状**: 未実装。本文書 §9 の設計課題として比較検討中。

### 6.4 Screen-Space Readability Layer

**表現**: 近景/遠景の grid overlay、将来的な contour 強調、fog によるフォーカス制御。

**データソース**: カメラ距離、fog パラメータ

**baked**: なし
**動的計算**: dual-scale grid の distance fade、URP fog 合成
**独立メッシュ**: なし（GridGround.shader 内で処理）

**移行計画**: 現在 dual-scale grid は常時表示だが、Ideal 段階で `_GridMode` を導入し debug overlay に降格予定（SP-032 保留事項）。

### 6.5 Debug Visualization Layer

**表現**: Analysis 層（Dead End / Choke / Intersection / Plaza）、Terrain 層（HillCluster / Hill ellipse / Decoration dots）。

**データソース**: `MapAnalysis`、`MapTerrain`

**baked**: なし
**動的計算**: `LineRenderer` による runtime 描画
**独立メッシュ**: LineRenderer GO（Tab 切替で表示/非表示）

**責務**: 生成結果の検証専用。本番描画とは完全に分離。`AnalysisVisualizer` が管理。

---

## 7. Gradient / Mask Strategy

| 項目 | 生成元 | 解像度方針 | 更新頻度 | CPU bake / shader 計算 | SP-032 委譲範囲 |
|------|--------|-----------|----------|----------------------|----------------|
| **Elevation gradient** | `ElevationMap.Sample` → `_GroundHeightSlopeTex.R` | mask 解像度 (512-1024) | seed 変更時のみ | CPU bake (elevation norm) + shader (3色ブレンド) | shader step 3 |
| **Slope tint** | `ElevationMap.SampleSlope` → `_GroundHeightSlopeTex.G` | 同上 | 同上 | CPU bake (slope norm) + shader (0.3 閾値混合) | shader step 4 |
| **Moisture / shore tint** | 水体距離 + 海岸遷移 → `_GroundSemanticTex.R` | 同上 | 同上 | CPU bake (距離場ラスタライズ) + shader (ティント混合) | shader step 8 |
| **Urban influence tint** | 建物 footprint + halo → `_GroundSemanticTex.B` | 同上 | 同上 | CPU bake (footprint + 距離減衰) + shader (ティント混合) | shader step 10 |
| **Road influence tint** | 道路中心線 + `RoadProfile.TotalWidth` → `_GroundSemanticTex.G` | 同上、最狭道路幅で最低 4 texel | 同上 | CPU bake (幅ベースラスタライズ) + shader (半ブレンド) | shader step 9 |
| **Contour readability** | `ElevationMap.Sample` → `_GroundHeightSlopeTex.A` (jitter) | 同上 | 同上 | CPU bake (jitter) + shader (worldElev / interval + frac) | shader step 7 |

### 上位方針

- **macro gradient** (遠景で効く大域グラデーション) は elevation gradient + hillshade で担う
- **local tint** (近景で効く局所変化) は moisture / road / building influence で担う
- mask 解像度は `clamp(nextPow2(max(worldWidth, worldHeight) / 1.25), 512, 2048)`
- Theme 切替時に mask 再生成は行わない。色パラメータのみ差し替え
- Ideal 段階で `_MacroNoiseTex` (低周波色むら + 微細ざらつき) を追加予定

---

## 8. Road Readability Strategy

### 8.1 道路の真実 (SSOT)

道路の SSOT は `MapEdge` のノード接続グラフである。
各 edge は `tier` (主幹/支線/細道) と `layer` (地表/橋/トンネル) を持ち、`RoadProfile` SO が tier ごとの車線数・幅・標示を定義する。

`MapRenderer` が `MapEdge` → Bezier 曲線 → strip mesh に変換し、`Road.shader` が UV 駆動で車線標示・路面質感を描画する。この pipeline において centerline は Bezier 制御点列として暗黙的に存在するが、独立した centerline graph / spline としては明示的に保持されていない。

### 8.2 表現モード

| モード | 表示内容 | 用途 |
|--------|----------|------|
| **Aerial composite** (デフォルト) | `Road.shader` による車線標示付き路面 + ground の road influence tint | 本番プレイ画面 |
| **Line-art** (将来) | centerline + tier 差の太さ + 交差点ノードのマーカー | 道路構造の確認・デバッグ |
| **Analysis** (既存) | `AnalysisVisualizer` の LineRenderer (Intersection / Choke 可視化) | 生成結果の品質検証 |

### 8.3 道路確認性の評価基準

道路確認性は以下の観点で評価する:

1. **tier 差の視認性**: 主幹道路と細道の太さ・色・標示の差が一目で判別できるか
2. **交差点の可読性**: 交差点ディスクと交差パターンが読めるか
3. **階層差の表現**: 橋 / トンネル / 地表道路の層差が視覚的に区別できるか
4. **road influence の範囲**: ground の road influence tint が道路の存在を補助的に示しているか（ただし独立 mesh が主表示）
5. **遠景での連続性**: ズームアウト時に道路ネットワークの全体構造が読めるか

### 8.4 切替戦略 (TBD)

line-art 表示の実装は Ideal 段階。現状は `AnalysisVisualizer` の Tab 切替が代替。
将来的には `_GridMode` 導入と同時に road overlay mode を追加し、composite / line-art を切替可能にする。

---

## 9. Lightweight Shadow & Dappled-Light Options

> **注意: 本セクションは方向性メモである。** マップの詳細度が十分に成熟した段階で初めて検討対象になる。
> これらの表現を実現するために 3D 生成の複雑度を上げてはならない。
> 現在の procedural mesh + semantic mask パイプラインの範囲内で「副産物として」実現できる場合にのみ採用する。
> 木漏れ日や建物陰影を目標にして設計を歪めることは、Negative Definition §5 の禁止事項に準じる。

### 9.1 木漏れ日の軽量表現

| 案 | 手法 | 見た目 | 実装難易度 | パフォーマンス | モバイル適性 | デフォルメ度 | 推奨度 |
|----|------|--------|-----------|---------------|-------------|-------------|--------|
| **A** | **World-space noise + canopy mask** | 地面に現れる不規則な光斑パターン。tree 下のみに出現 | 中 (canopy mask 生成 + shader 追加) | 低コスト (texture sample 1回) | 高 | 高 | **暫定推奨** |
| B | Projected tint (tree canopy 下限定) | tree 位置にソフトな暗化円。光斑なし | 低 (CPU で decal 配置) | 最低コスト | 最高 | 最高 | 次善 |
| C | Quad / decal 的光斑 | tree 下に sprite を敷き詰め。光斑が具体的な形を持つ | 高 (sprite 管理 + 透過描画) | 中コスト (draw call 増) | 中 | 中 | 保留 |
| D | Baked animation 的揺らぎ | 時間変化する光斑。木の葉の影が揺れる印象 | 高 (noise animation + time 依存) | 中コスト | 中 | 低 | Ideal 段階 |

**暫定推奨: 案 A (World-space noise + canopy mask)**

理由: `GroundSemanticMaskBaker` の既存パイプラインに tree canopy mask を追加チャンネルとして焼き込める。`GridGround.shader` に noise modulated darkening ステップを 1 つ追加するだけで実現可能。draw call 増なし。モバイルで安定。「本物の影」ではなく「木下にだけ現れる光斑の印象」として軽量近似する方針に最も合致する。

### 9.2 建物陰影の軽量表現

| 案 | 手法 | 見た目 | 実装難易度 | パフォーマンス | モバイル適性 | デフォルメ度 | 推奨度 |
|----|------|--------|-----------|---------------|-------------|-------------|--------|
| **A** | **Contact darkening (building halo 拡張)** | 建物基部の周囲が微暗化。接地感を生む | 低 (既存 building influence の拡張) | 最低コスト (mask 既存) | 最高 | 高 | **暫定推奨** |
| B | Fake drop shadow (projected quad) | 建物の片側に投影される矩形影 | 中 (quad 生成 + 方向計算) | 低コスト (quad 1枚/建物) | 高 | 中 | 次善 |
| C | Directional tint by face orientation | 建物の面方向に応じた明暗。北面が暗い等 | 低 (shader 内 normal dot) | 最低コスト | 最高 | 高 | 補助として採用可 |
| D | Baked AO-like gradient | 建物基部から上に向かって暗→明のグラデーション | 中 (vertex color 活用) | 低コスト | 高 | 中 | Ideal 段階 |

**暫定推奨: 案 A (Contact darkening)**

理由: SP-032 で既に `_GroundSemanticTex.B` に building influence が焼き込まれている。この既存チャンネルの「接地リングの読ませ方」を調整するだけで contact darkening が実現できる。新規 shader pass や draw call の追加が不要。「完全な時刻依存 shadow」ではなく「接地感・立体感・方向感」を補う程度のデフォルメを優先する方針に合致する。

案 C (Directional tint) は `BuildingFade.shader` 内の Lambert 計算を活かして追加コストなしで補助採用可能。

---

## 10. Mobile Budget Strategy

### 10.1 ターゲットプロファイル

- **エンジン**: URP 17.3.0
- **ターゲット FPS**: 30fps (mobile default) / 60fps (desktop)
- **ターゲット端末**: 数値未確定。方向性として「中価格帯 Android (2023年以降) で 30fps 維持」を目標

### 10.2 Keep / Cut / Ideal-Later

| 要素 | 判定 | 理由 |
|------|------|------|
| **Lambert + ambient lighting** | keep | 全 shader で使用中。最低限の立体感。コスト無視可能 |
| **Hillshade (4-tap normal reconstruction)** | keep | 地勢の可読性の中核。texture sample 4回だが ground は 1 mesh なので影響小 |
| **Semantic mask sampling (2 tex)** | keep | ground compositing の基盤。texture sample 2回 |
| **Contour lines** | keep | frac 計算のみ。コスト無視可能 |
| **Dual-scale grid** | keep (→ Ideal で debug 降格) | fwidth 計算 2回。軽量 |
| **URP fog** | keep | `MixFog` は URP 標準。コスト無視可能 |
| **Road.shader UV markings** | keep | UV 駆動の if/smoothstep。分岐は最大 4 lane で打ち切り |
| **Water.shader wave + specular** | keep (強度調整) | sin 2回 + pow 1回。mobile では `_WaveIntensity` / `_SpecularIntensity` を下げて対応 |
| **`_MAIN_LIGHT_SHADOWS_CASCADE`** | **cut on mobile** | cascade shadow はモバイルで最もコストが高い。mobile では single shadow map または shadow off を推奨 |
| **`_SHADOWS_SOFT`** | **cut on mobile** | soft shadow のフィルタリングは mobile budget を超える |
| **SSAO / screen-space effects** | **cut** | 現在未使用。導入しない |
| **`_MacroNoiseTex`** (低周波ノイズ) | **ideal-later** | texture sample 1回追加。効果は微妙。Ideal 段階で検討 |
| **Wood dappled light** (§9.1) | **ideal-later** | 案 A でも mask チャンネル追加が必要。MVP 後に検討 |
| **Ground chunking** | **ideal-later** | メッシュ分割 + カリング。mobile 最適化の本命だが実装コスト大 |

### 10.3 禁止コスト

- 全面リアルタイム SSAO
- 複数パスのポストプロセス (bloom / DOF / motion blur)
- 高解像度 shadow atlas (2048+ を常時)
- per-object real-time shadow (建物ごと)
- GPU instancing なしでの大量 draw call (装飾 300+ 個別)

---

## 11. Phased Roadmap

### 11.1 MVP (SP-032 Slice 1-5 = 現在の実装)

**何をやるか**:
- Ground carrier mesh + 2枚 CPU semantic mask + GridGround.shader 13 ステップ合成
- 解像度 40 → 96
- Elevation gradient / Slope tint / Hillshade / Curvature / Contour / Moisture / Road / Building influence
- `GroundSurfacePresetDefaults` による GeneratorType 別強度自動選択
- Theme 切替時の色のみ差し替え

**見た目の変化**: 地表が「即席色付けグリッド」から「地勢が読める連続面」に変わる。hillshade で起伏、contour で等高線、influence tint で道路・水辺・建物の接地感が出る。

**何を保留するか**: MacroNoiseTex、GridMode、木漏れ日、建物接地影、line-art 道路表示、ground chunking

**SP-032 との関係**: MVP = SP-032 そのもの。Slice 5 の手動検証が完了条件。

### 11.2 Recommended (SP-032 MVP 完了後)

**何をやるか**:
- `_MacroNoiseTex` 導入 (低周波色むら + 微細ざらつき)
- `_GridMode` 導入 (debug/analysis overlay モード切替)
- Contact darkening の調整 (building influence ティントの暗化方向強化)
- Directional tint の `BuildingFade.shader` への追加 (面方向別明暗)
- 色パレット最終確定 (4 preset × 2 theme の芸風統一)

**見た目の変化**: 地表に微細なテクスチャ感が加わり、建物基部に接地感が生まれる。grid が art style から分離される。全体の色が統一された芸風に収まる。

**何を保留するか**: 木漏れ日、ground chunking、line-art 道路表示、major/minor contour

### 11.3 Ideal (将来 — 方向性メモ)

> これらは「目指す方向」として記録するが、実装判断はマップの詳細度が十分に成熟してから行う。
> 3D 生成の複雑度を上げる方向に引っ張られないよう、既存パイプラインの副産物として実現できる範囲に限定する。

**検討対象**:
- Ground chunking (2x2+ chunk、固定 meters-per-vertex)
- Major/minor contour (2段等高線)
- Line-art 道路表示モード
- `_MacroNoiseTex` の有効活用

**方向性メモとして保持** (実装判断は将来):
- 木漏れ日 (案 A: world-space noise + canopy mask) — §9.1 参照
- Fake drop shadow (建物片側投影影) — §9.2 参照
- 4枚 mask 体制 (SP-032 Ideal 構成)

**制約**: これらの表現のために新規 3D 生成パイプラインを追加しない。既存の semantic mask + shader compositing の範囲内で実現可能な場合にのみ採用する。

**SP-032 との関係**: SP-032 の「Ideal の packed texture 構成」(4枚 mask) に対応。

---

## 12. Keep / Retreat / Discard

| 対象 | 判断 | 理由 |
|------|------|------|
| **ElevationMap + hill cluster 生成** | **keep** | 地形 field の SSOT。全コンポーネントが参照。hybrid orthophoto relief の基盤 |
| **RoadProfile SO + Road.shader** | **keep** | 道路 network の可視化。UV 駆動の車線標示は aerial composite 表現の中核 |
| **WaterProfile SO + Water.shader** | **keep** | 水面の深度・波・泡表現。typed waterBodies によるタイプ別レンダリング |
| **BuildingFade.shader (roof-fade)** | **keep** | 近接時の屋根透過は探索体験に必要 |
| **GroundSemanticMaskBaker + GridGround.shader** | **keep** | ground compositing の実装。SP-032 MVP の成果 |
| **DecorationPlacer / Spawner + LOD** | **keep (補助表現として)** | 環境の手がかり。ただし情報密度の主役ではない |
| **AnalysisVisualizer (LineRenderer)** | **keep (debug 専用)** | 生成結果の検証に不可欠。本番描画とは分離 |
| **Dual-scale grid (GridGround.shader 内)** | **retreat → debug** | 現在は常時表示だが、art style と混在。`_GridMode` 導入で debug overlay に降格 |
| **`_GroundHeightSlopeTex.A` (contour jitter)** | **keep** | 等高線の機械的均一感を弱める。低コスト |
| **Unity Terrain / splatmap** | **discard (採用しない)** | procedural mesh ベースの方針に反する。移行コストが利点を上回る |
| **SSAO / 複雑なポストプロセス** | **discard** | モバイル予算を超える。baked-like 手法で代替 |

---

## 13. Acceptance Criteria

SP-040 の方向性修正が成功したとみなせる条件:

1. **道路ネットワークの確認が容易**: 任意のプリセットで、主幹/支線/細道の tier 差が一目で判別できる。交差点形状が読める。橋/トンネルの層差が視認できる
2. **高低差が一目で読める**: hillshade + elevation gradient + contour により、丘陵・谷・平地の地勢が俯瞰で把握できる
3. **遠景で破綻しない**: ズームアウト時に地表のグラデーションが連続的で、mesh 粗さやティント境界のアーティファクトが目立たない
4. **近景で情報過多にならない**: ズームイン時に contour / grid / influence tint が重なりすぎず、地面が「うるさく」ならない
5. **mobile default で成立する**: cascade shadow off / soft shadow off の状態で、中価格帯 Android で 30fps を維持する
6. **影やグラデーションが効いているが重くない**: hillshade の乗算が自然で、moisture / road / building tint が「塗り残し」に見えない
7. **4 preset x 2 theme で大破綻がない**: Coastal / Rural / Grid / Mountain の各プリセットで、Dark / Parchment の各テーマで、統一された芸風が維持される
8. **道路・水面・建物のエッジが鮮明**: ground の influence tint が道路/水面/建物の輪郭を濁らせていない。独立 mesh geometry が clean edge を守っている
9. **debug overlay と art style が分離**: grid / analysis 表示が本番描画と切替可能で、混在しない

---

## 14. Relationship to SP-032

### 責務分離

| 文書 | 責務 | 扱う範囲 |
|------|------|----------|
| **SP-040** (本文書) | 上位方針 | 北極星、基本思想、ネガティブ定義、5層アーキテクチャ、道路確認性基準、モバイル予算、ロードマップ、完了条件 |
| **SP-032** | 地表合成の実装仕様 | GridGround.shader のプロパティ・ステップ構成、GroundSemanticMaskBaker の mask 構成・解像度・cache、Theme 連携の色パラメータ、検証チェックリスト |

### 重複しない原則

- SP-040 は「何を目指すか」「何を禁止するか」「どの層が何を担うか」を定める
- SP-032 は「地表層をどう実装するか」を定める
- SP-040 が shader のプロパティ名やステップ番号を詳述することはない
- SP-032 が他のレイヤー (road / water / building) の方針を定めることはない

### 将来の子仕様

SP-040 の下位に位置する将来の仕様候補:

- **SP-0xx: 道路ビジュアル仕様** — `Road.shader` の拡張、line-art モード、交差点描画の改善
- **SP-0xx: 建物ビジュアル仕様** — `BuildingFade.shader` の拡張、面方向別明暗、接地影
- **SP-0xx: 水面ビジュアル仕様** — `Water.shader` の拡張、浜辺遷移帯 (W-4)
- **SP-0xx: 装飾ビジュアル仕様** — 木漏れ日、canopy mask、装飾の LOD 戦略
