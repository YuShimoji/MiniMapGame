# MiniMapGame セッション引き継ぎコンテキスト (2026-04-06)

## プロジェクト概要

MiniMapGame は手続き型マップ探索ゲーム。プロシージャル都市を5分間探索し、建物内部で断片的テキストを発見するセッション型ゲーム。

- repo: `C:\Users\thank\Storage\Game Projects\MiniMapGame`
- branch: master (trunk-based)
- Unity 6.3 LTS (6000.3.6f1) / URP 17.3.0 / C#
- 正本: `docs/project-context.md`, `docs/runtime-state.md`, `docs/spec-index.json`, `CLAUDE.md`

---

## 現在の最重要課題

**relief だけでは最終形に届かない。**

browser-preview 優先方針自体は維持するが、課題は単なる色・陰影調整ではない。  
ユーザーが求めているのは、裏山・河川帯・人工広場・住宅密集のような **上空観測密度** である。

このため、SP-040 の下位に **SP-041: Observed Surface Synthesis** を追加した。
今後は「航空写真の模写」ではなく **Field / Grammar / Glyph** 方式で、
forest / yard / river / housing の surface archetype を設計する。

**検証優先順位**: browser-preview で archetype / density / layering を確認 → Unity に移植 → Unity PlayMode 検証

---

## 今セッションの経緯

### 1. drift 調査 (ユーザー指摘)

前回セッション (session 10, 2026-04-05) でグラフィック修正が本来の作業軸だったにもかかわらず、Quest実装 (SP-001) に逸脱していた。

**原因**: session 7 (2026-03-19) の REFRESH drift診断で「なぜ探索するか？」の欠如を理由に体験プロトタイプ方針D (5分タイマー+クエスト) が採択され、グラフィック作業が後回しにされた。SP-032 Slice 5 (4preset x 2theme の手動検証) は未完のまま放置された。

### 2. 「Web上での見え方を優先」方針の発覚

ユーザーが「Web上での見え方を優先する方針はどうなっていますか？」と指摘。全文書を検索したが記録がなかった。

**ユーザーの真意**: Unity上での表示のクオリティに全く目処が立たないため、まず Web上でのレンダリングでまともなクオリティを実現しなければならない。最悪、画像のみ回収できるようにする。

これは3案の比較 (A: 共有画像可読性 / B: WebGL副ターゲット / C: WebGL主ターゲット) のいずれとも異なる、**検証インフラの問題**。

### 3. browser-preview の復元

`browser-preview/` ディレクトリ (map-gen.js + renderer.js + index.html) がコミット `075295a` で origin/master に作られていたが、ローカルブランチに統合されていなかった。

**分岐構造**:
- origin/master: `57abb40` → `9138b93` (AI config) → `075295a` (browser-preview)
- local master: `57abb40` → Quest実装 9コミット → `15e321b`

`git checkout origin/master -- browser-preview/` で復元。

### 4. renderer.js の SP-040 準拠改修

GridGround.shader の 13 ステップ合成ロジックを Canvas 2D に移植:
- 標高グラデーション (3色: base → mid → high)
- Hillshade (4-tap 勾配 + 2D ライト方向ドット積)
- 曲率強調 (ラプラシアン)
- 等高線 (worldElev / interval の frac)
- Moisture / Road / Building influence tint (距離場ベース)
- Dark / Parchment テーマ切替
- プリセット別強度 (GroundSurfacePresetDefaults.cs と同一値)

### 5. ユーザー検証結果: **不合格**

ブラウザで確認した結果、**クオリティが使えないレベル**。

目視で確認された問題:
- **全体が暗すぎる**: Parchment テーマなのにほぼ黒い画面
- **標高グラデーションが読めない**: 3色遷移がほぼ見えない
- **Hillshade が暗方向に過剰適用**: 乗算が全体を潰している
- **等高線が視認不能**: コントラスト不足で埋もれている
- **道路と地表のコントラスト不足**: 道路が地表に溶け込んでいる
- **建物が地表に埋没**: 色差がなく区別できない

### 6. 新しい問題定義

ユーザーの追加要求により、問題は「画面が暗い」ことだけではないと確定した。

不足しているもの:
- 裏山を特徴づける林冠密度
- 鉄道 / 車庫 / 資材置き場のような広い人工地形の用途感
- 川と川沿いの帯構造
- 家屋群の異様な情報密度

結論:
- Google Maps 的な擬似標高表示は目標外
- 画像生成的な詳細描写にも逃げない
- **Field / Grammar / Glyph** で観測密度を作る

---

## 未コミット変更

```
staged (origin/master から復元):
  new file: browser-preview/index.html
  new file: browser-preview/map-gen.js
  new file: browser-preview/renderer.js

modified (今セッション):
  browser-preview/index.html   -- Theme ドロップダウン追加
  browser-preview/renderer.js  -- SP-040 準拠に全面改修 (問題あり)
  docs/project-context.md      -- ビジュアル品質確立の優先順位を追記
  docs/specs/map-visual-direction.md -- §3.5 追加 (Web優先方針)
```

---

## renderer.js の問題箇所 (次セッションで修正すべき点)

### 色値の問題

テーマカラーは Unity の Theme_Dark.asset / Theme_Parchment.asset から移植したが、[0-1] float を [0-255] int に変換する際にスケーリングが正しくない可能性がある。

現在の Parchment baseColor: `[158, 163, 122]` (= 0.62, 0.64, 0.48 * 255)
→ Unity 上で (0.62, 0.64, 0.48) は中間的な黄緑。ブラウザ上ではこれが暗すぎる。

**仮説**: Unity shader はリニア空間で計算しディスプレイがガンマ補正するが、Canvas 2D は sRGB 空間で直接計算している。リニア→sRGB 変換 (ガンマ≈2.2) を入れずに乗算しているため、hillshade の暗方向が二重に効いている。

### Hillshade 過剰

`hillshade = clamp((-dEdx * nlx + -dEdy * nly) * 4.0 + 0.5, 0, 1)` の乗算結果 `color *= lerp(1.0, hillshade, strength)` が Canvas 2D の sRGB 空間で暗すぎる結果を生む。

**対策案**:
1. 色計算をリニア空間で行い、最終出力時に sRGB に変換する
2. hillshade の乗算強度を弱める (4.0 → 2.0 等)
3. baseColor を明るめに調整 (sRGB 空間に合わせて)

### Influence tint の影響範囲

moisture/road/building の影響範囲が広すぎ、地表が一様に暗くなっている可能性。
- moistureRadius: 40 → 25 に縮小検討
- roadInfluenceRadius: 20 → 12 に縮小検討
- building haloRadius: 15 → 10 に縮小検討

### パフォーマンス

レンダリングに 550ms かかっている。influence 計算が O(pixels * features) でボトルネック。
対策: influence を低解像度で事前計算し、最終合成時にサンプリング。

---

## プロジェクト全体の状態

### 主要仕様のステータス

| ID | タイトル | 状態 | pct |
|----|----------|------|-----|
| SP-001 | ゲームループ (Sandbox+Quest) | partial | 90% |
| SP-032 | 地表合成レンダリング | partial | 85% |
| SP-040 | マップビジュアルディレクション | partial | 40% |
| SP-060 | Interior Interaction | partial | 95% |
| SP-061 | Exploration Progress | partial | 95% |
| SP-062 | Floor Navigation | partial | 95% |

spec-index: 39 エントリ (done 23 / partial 10 / todo 3 / deprecated 1 / legacy 1 / merged 2)

### ブランチ状態

master と origin/master が diverge:
- local: 9 commits ahead (Quest実装 + SP-040)
- origin: 2 commits ahead (AI config + browser-preview)
- 共通祖先: `57abb40`
- push 前に `git pull --rebase` が必要

### session 10 で追加されたコミット (local のみ)

```
15e321b feat: SP-040ビジュアル方針文書化 + SP-001 Phase 2プリセット別フィルタ完了
126f0bc docs: session 9 最終状態同期
8257492 feat(SP-001): Phase 2 クエスト拡充
a79afa2 fix: Quest進捗管理バグ2件修正
e33baec docs: session 9 状態同期
00e70c6 refactor: BuildingInteraction キャッシュ化
c8af8e5 feat(SP-001): Quest保存/復元
1c237b6 feat(SP-001): Quest基盤実装
2b4fe4c excise: MiniGame 7ファイル完全削除
```

---

## 重要な設計資産

### browser-preview (Canvas 2D プレビューツール)

- `browser-preview/index.html`: UI (Seed/Type/Theme/Toggles/Export PNG)
- `browser-preview/map-gen.js`: C# MapGen パイプラインの JS 移植 (1387行)
  - 4 Generator (Organic/Grid/Mountain/Rural)
  - BuildingPlacer, TerrainGenerator, ElevationMap, WaterGenerator
  - 全て seed 決定論的
- `browser-preview/renderer.js`: Canvas 2D レンダラー (現在問題あり)

### Unity 側の対応コード

- `Assets/Shaders/GridGround.shader`: 13 ステップ地表合成 (247行)
- `Assets/Scripts/Runtime/GroundSurfacePresetDefaults.cs`: プリセット別強度
- `Assets/Scripts/Runtime/GroundSemanticMaskBaker.cs`: CPU マスク生成
- `Assets/Scripts/Runtime/ThemeManager.cs`: テーマ適用 (8色をマテリアルに)
- `Assets/Resources/Themes/Theme_Dark.asset`, `Theme_Parchment.asset`

### テーマカラー参照

**Parchment (Unity [0-1] float)**:
- baseColor: (0.62, 0.64, 0.48) / midColor: (0.68, 0.66, 0.52) / highColor: (0.74, 0.70, 0.58)
- slopeColor: (0.60, 0.56, 0.46) / moistureTint: (0.48, 0.58, 0.55)
- roadTint: (0.70, 0.66, 0.54) / buildingTint: (0.68, 0.64, 0.54)
- contourColor: (0.50, 0.48, 0.38) / gridLineColor: (0.56, 0.54, 0.42)

**Dark (Unity [0-1] float)**:
- baseColor: (0.28, 0.33, 0.24) / midColor: (0.38, 0.34, 0.26) / highColor: (0.48, 0.44, 0.38)
- slopeColor: (0.34, 0.30, 0.26) / moistureTint: (0.18, 0.28, 0.32)
- roadTint: (0.38, 0.36, 0.33) / buildingTint: (0.36, 0.33, 0.30)
- contourColor: (0.18, 0.22, 0.16) / gridLineColor: (0.22, 0.26, 0.20)

### プリセット別強度 (GroundSurfacePresetDefaults)

| Preset | hillshade | contour | moisture | road | building |
|--------|-----------|---------|----------|------|----------|
| Mountain | 0.7 | 0.35 | 0.4 | 0.25 | 0.2 |
| Rural | 0.5 | 0.2 | 0.5 | 0.2 | 0.15 |
| Grid | 0.25 | 0.1 | 0.2 | 0.4 | 0.35 |
| Organic | 0.55 | 0.25 | 0.45 | 0.3 | 0.25 |

---

## 決定済みの制約

1. **3D生成複雑化禁止**: 木漏れ日/建物陰影は方向性メモ止まり。既存パイプラインの副産物としてのみ採用 (SP-040 §9, memory)
2. **Unity Terrain 移行禁止**: procedural mesh 維持 (SP-040 §5.5)
3. **ビジュアル品質基準**: 「フルスクリーンで美しいか」ではなく「スクリーンショット・縮小表示でも地図として読めるか」 (SP-040 §3.5)
4. **browser-preview 優先**: Unity PlayMode 検証に先立ち Canvas 2D で可読性を確立する (SP-040 §3.5)

---

## Session 12 追加コンテキスト (2026-04-06)

### 4段階の ImageData 試行は全て不採用として固定

ユーザー判断: 「このクオリティでは使えない。作業がこれまでの延長線上でしかなく、航空写真に近いものは再現できなさそう」

**不採用の共通理由**: 全てが「ピクセル単位で色を計算する」ImageData 操作に閉じている。256x172 セルに enum + density scalar 1個では多スケール・異方性・内部構造を原理的に表現不能。SP-041 の Field/Grammar/Glyph は一度も正しく実装されていない。

| 段階 | 試行内容 | 失敗の本質 |
|------|----------|-----------|
| 1. shader直訳 | GridGround.shader → pixel-by-pixel | sRGB/リニア不一致 + Canvas 2D に乗算/fog がない |
| 2. レイヤー分離 | hillshade/contour/moisture を個別α合成 | 薄い情報を重ねても密度にならない |
| 3. glyph散布 | ellipse/rect を乱数配置 | field/grammar なしの散布 = 地理的意味のないノイズ |
| 4. surface classification | セルを enum + density scalar で色 lerp | 1セル1ラベル1スカラー → 内部構造が消失 |

### 方向転換: patch/glyph 構造描画

ImageData ピクセル操作 → Canvas 2D パス描画 API (Path2D, clip, createPattern, globalCompositeOperation multiply/overlay) に転換。

### 反映済み文書

- `docs/project-context.md`: CURRENT SLICE を「SP-041 patch/glyph 構造転換」に更新、DECISION LOG に転換決定追加、HANDOFF 更新
- `docs/runtime-state.md`: session 12、visual evidence を failed に変更
- `docs/specs/observed-surface-synthesis.md`: Section 8.1 を「試行済み・不採用」に変更

---

## 次セッションで最初にやるべきこと

### ロードマップ: SP-041 patch/glyph 構造転換

observed-surface.js を全面書き換え。3段パイプライン (computeFields → extractPatches → renderPatches) を構築し、Canvas 2D パス描画で archetype を実装する。

| Step | 内容 | 検証ポイント |
|------|------|-------------|
| 0 | パイプライン骨格 | パッチ境界線のみ表示して形を目視確認 |
| 1 | Forest archetype (canopy mass + holes + edge roughness) | clip + fill + destination-out で「緑の塊に穴」が出るか |
| 2 | Residential archetype (roof swarm + parcel gaps + clutter) | buildings[] 実座標で屋根群と隙間のリズムが見えるか |
| 3 | Riparian archetype (3層帯 + confluence wedge) | 川が「線ではなく帯」として読めるか |
| 4 | 統合検証 (4 preset x 2 theme) | 地図として読めるか |
| 5 | archetype 別トグル UI | 各層の効きを独立確認 |

### 再利用する既存資産

- **renderer.js**: ribbon polygon (川), multi-pass stroke (道路), clip+gradient (海岸) のパターン
- **map-gen.js**: buildings[] (position/width/height/angle), waterBodies[].pathPoints, hills[], ElevationMap
- **observed-surface.js**: classifySurface() の Field 計算ロジック (typeField/densityField/elevField/slopeField)

### やらないこと

- Industrial / Yard Patch (生成データに rail 概念がない)
- Unity への移植 (browser-preview で品質確立が先)
- ImageData ベースの微調整
- 解像度増加で乗り切る試み

---

## ユーザーから受けたフィードバック

### 文書化の後回し・失念は重大な過失
以前のセッションで「Web上での見え方を優先」方針が文書化されなかった。今回のセッションでも最初は「記録がありません」で止まろうとした。これは判断停止であり、既存文書から暫定解釈を導いて文書化すべきだった。

### 「記録がない」で止まるな
文書に明記がなくても、既存の仕様・文脈・実装から最善の暫定解釈を置くこと。ユーザーに「どういう意味ですか？」と投げ返さないこと。

### drift への警戒
グラフィック修正が作業軸だったのに Quest 実装に進んだのは drift。作業軸の変更にはユーザーの明示的な承認が必要。

---

## ファイル構成 (主要パスのみ)

```
browser-preview/
  index.html          -- ブラウザプレビュー UI
  map-gen.js          -- C# MapGen の JS 移植 (1387行)
  renderer.js         -- Canvas 2D レンダラー (要修正)

Assets/
  Shaders/            -- GridGround/Road/Water/BuildingFade
  Scripts/Runtime/    -- MapManager, ThemeManager, GroundSemanticMaskBaker, etc.
  Scripts/Data/       -- MapTheme, MapPreset, RoadProfile, etc.
  Resources/Themes/   -- Theme_Dark.asset, Theme_Parchment.asset
  Resources/Presets/  -- Preset_Coastal/Rural/Grid/Mountain/...

docs/
  project-context.md  -- プロジェクト状態の正本
  runtime-state.md    -- セッション間継続用の状態追跡
  spec-index.json     -- 全仕様エントリ (39件)
  specs/
    map-visual-direction.md    -- SP-040 ビジュアル方針 (北極星)
    ground-surface-compositing.md -- SP-032 地表合成実装仕様
    game-loop-sandbox-quest.md -- SP-001 クエストシステム
```
