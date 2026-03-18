# Debug Setup & Verification Guide

## Quick Start

### 1. Bootstrap Test Scene

1. Unity Editor を開く
2. メニュー: `MiniMapGame > Bootstrap Test Scene`
3. 以下が自動生成される:
   - MapManager + Renderer/Spawner群
   - 7プリセット (Coastal / Rural / Grid / Mountain / Island / Downtown / Valley)
   - 2テーマ (Dark / Parchment)
   - 3ロードプロファイル (Modern / Rural / Historic)
   - UI (MapControlUI / MiniMap / PlayerHUD)
   - Interior系 (InteriorController / InteriorRenderer / InteractionManager / ExplorationProgress)
   - カメラ + プレイヤー + ライティング
4. Play ボタンでマップが自動生成される

### 2. Runtime Controls

| キー | 機能 |
|------|------|
| **F1** | コントロールパネルの表示/非表示 |
| **Tab** | デバッグオーバーレイ切替: Off → Analysis → Terrain → Off |
| **WASD / 矢印** | プレイヤー移動 |
| **E** | インタラクション (Interior内: Discovery収集 / ドア操作 / 階段移動) |
| **I** | 探索メニュー表示/非表示 (Exploration Records) |
| **PageUp / PageDown** | フロア移動 (Stairwell付近で有効) |
| **Escape** | 建物から退出 |
| **マウス** | カメラ操作 |

### 3. Control Panel (F1)

- **プリセット切替**: 港湾都市 / 田舎町 / NYCグリッド / 山道
- **シード入力**: 任意のint値を入力して再生成
- **ランダムボタン**: ランダムシードで再生成
- **建物密度スライダー**: 0-100%
- **テーマ切替**: ダーク / 羊皮紙
- **統計表示**: ノード数、エッジ数、建物数、解析結果、地形情報、装飾内訳

---

## Debug Overlay Modes (Tab)

### Analysis Mode (Tab x1)

既存の道路解析を可視化:

| マーカー | 色 | 意味 |
|----------|-----|------|
| 赤い円 | Red | デッドエンド (行き止まり) |
| オレンジ曲線 | Orange | チョークポイント (ボトルネック道路) |
| 緑の十字 | Green | 交差点 |
| 青い四角 | Blue | プラザ (広場) |

### Terrain Mode (Tab x2)

地形生成の内部構造を可視化:

#### Hill Clusters (丘クラスタ)
中心点に**色付き円 + 方向矢印**で表示。

| 色 | ClusterType | 特徴 |
|----|-------------|------|
| 赤 | Ridge | 直線状に並ぶ尾根 (3-6丘) |
| 緑 | MoundGroup | 円形に集まる丘群 (3-5丘) |
| 青 | ValleyFramer | 平行な2尾根で谷を形成 |
| 黄 | Solitary | 独立した単独丘 |

- **矢印の向き** = クラスタの主軸方向 (Ridge: 尾根の走る方向, ValleyFramer: 谷の方向)

#### Hill Outlines (丘の輪郭)
各丘の実際のサイズ・形状を**楕円**で表示。SlopeProfile別に色分け。

| 色 | SlopeProfile | 断面形状 |
|----|-------------|----------|
| 灰 | Gaussian | 標準ベルカーブ |
| 赤 | Steep | 急峻な崖 |
| 緑 | Gentle | なだらかな丘陵 |
| 琥珀 | Plateau | 平頂＋急斜面 |
| 紫 | Mesa | 平頂＋垂直壁 |

#### Decoration Positions (装飾配置)
全装飾の位置を**小さな色付き点**で表示。

| 色 | DecorationType | 配置条件 |
|----|---------------|----------|
| 黄 | StreetLight | 道路沿い |
| 緑 | Tree | 道路沿い |
| 茶 | Bench | 道路沿い |
| 灰 | Bollard | 道路沿い |
| 灰緑 | Rock | 高地 + 急斜面 |
| 暗灰 | Boulder | 急斜面 |
| 明緑 | GrassClump | 平坦な低地 |
| ピンク | Wildflower | 低地 + 水辺 |
| 暗緑 | Shrub | 丘の縁 (遷移帯) |
| 黄茶 | Fence | 道路沿い (Rural/Mountain) |
| 暗茶 | Stump | Tree付近 (Rural) |
| 黄色 | SignPost | 交差点 (Mountain/Rural) |

---

## Verification Checklist

各プリセットで最低2-3シードを試し、以下を確認する。

### H1: Hill Clustering (丘クラスタ)

- [ ] Terrain Modeで複数のクラスタ（色付き円+矢印）が表示される
- [ ] Ridge: 丘が直線状に並んでいる
- [ ] MoundGroup: 丘が円形にまとまっている
- [ ] ValleyFramer: 2列の尾根の間に谷がある
- [ ] クラスタの矢印が地形の主軸と一致している
- [ ] クラスタ同士が重ならず適度に分散している

### H2: Decoration Expansion (装飾拡張)

- [ ] F1パネルの統計に12種類の装飾内訳が表示される
- [ ] Rock/Boulder: 高地・急斜面に配置されている
- [ ] GrassClump: 平坦な低地に配置されている
- [ ] Wildflower: 水辺の低地に配置されている
- [ ] Shrub: 丘の中腹（遷移帯）に配置されている
- [ ] Fence: 道路沿い（Rural/Mountain）に配置されている
- [ ] SignPost: 交差点付近（Mountain/Rural）に配置されている
- [ ] Grid(NYC)プリセットでは地形装飾が少ない（hillDensity=0）

### H3: Slope Profiles (傾斜プロファイル)

- [ ] F1パネルにSlopeProfile分布が表示される
- [ ] Terrain Modeで楕円の色が複数種類ある（灰/赤/緑/琥珀/紫）
- [ ] Steep: 地面の高低差が急激
- [ ] Gentle: なだらかな傾斜
- [ ] Plateau: 頂上が平ら
- [ ] Mesa: 頂上が平らで壁が垂直

### Q1-Q6: Quick Visual Wins

- [ ] Q1: 地面に等高線が見える（高低差のある場所で確認）
- [ ] Q2: 地面メッシュが滑らか（ポリゴンの角が目立たない）
- [ ] Q3: 細かいグリッド + 太い粗グリッド（5倍間隔）が見える
- [ ] Q4: 同じ階数の建物でも高さに微妙な差がある
- [ ] Q5: 斜面が周囲より暗く着色されている
- [ ] Q6: 建物の表面反射に微妙なバリエーションがある

### Per-Preset Expectations

| プリセット | 期待される地形 | 装飾傾向 |
|-----------|--------------|----------|
| **Coastal** (港湾都市) | hillDensity=30%, 海岸あり, 河川あり | 道路装飾が多い, 地形装飾は少なめ |
| **Rural** (田舎町) | hillDensity=75%, 河川あり | Fence/Stump/SignPostが出やすい |
| **Grid** (NYC) | hillDensity=0%, 平坦 | 道路装飾のみ, 地形装飾なし |
| **Mountain** (山道) | hillDensity=95%, maxElev=40m | Rock/Boulder多数, SignPost/Fenceあり |

---

## Gate-1: P4道路手動検証（必須）

このGateは **例外なし** で実施する。完了前に B/D/E/W-2 へ進まない。

### 実施マトリクス（最低要件）

| 軸 | 要件 |
|----|------|
| Preset | Coastal / Rural / Grid / Mountain |
| Theme | Dark / Parchment |
| Seed | 最低1シード（推奨3シード） |

最低8ケース（4×2）を実施し、推奨では24ケース（4×2×3）を実施する。

### チェック項目（各ケース共通）

- [ ] 道路が描画される（欠損・透明化なし）
- [ ] 車線標示がTierに応じて不自然でない
- [ ] 交差点（degree>=3）で円盤拡張が破綻しない（穴・Z-fight・極端な重なりなし）
- [ ] 建物が道路にめり込まない（RoadProfile連携セットバックが機能）
- [ ] Coastal/Grid は Modern、Rural/Mountain は Rural に自動バインドされる
- [ ] Theme切替時に道路の marking/curb 色が追従する

### 判定ルール

- **PASS**: 全ケースで重大破綻なし、FAIL 0件
- **FAIL**: 1件でも破綻あり
- FAIL時は修正タスク化し、修正後に同一ケースを再実行する

### 記録先

結果は `docs/verification/road-p4-gate-results.md` に記録する。
実行手順と推奨 seed は `docs/verification/road-p4-gate-runbook.md` を参照する。

---

## SP-032: 地表合成レンダリング検証（Slice 1-4 実装済み、検証待ち）

Ground carrier mesh + CPU semantic masks (2xRGBA8) + GridGround.shader 13ステップ合成。
Slice 5 の手動検証項目。各プリセットで最低2-3シードを試す。

### 基本動作

- [ ] 同一seedで semantic mask 結果が一致する
- [ ] Theme切替時に mask は再生成されず、色だけ変化する
- [ ] Generate再実行時の GC / フレーム落ちが許容範囲
- [ ] Console に `[GroundSemanticMaskBaker]` ログが出る（bake時間）
- [ ] `MapManager.Clear()` 後に texture / material instance が残らない

### Hillshade / Contour

- [ ] hillshade が地形の起伏読解を助ける（Mountain で顕著に確認）
- [ ] hillshade が主照明条件に依存しすぎず常に読図性を維持する
- [ ] contour が平地でうるさすぎず、斜面で読める
- [ ] Grid(平坦)プリセットで contour がほぼ見えない（正常）

### Semantic Influence

- [ ] 水辺 tint が水上ではなく岸側に出る
- [ ] 道路 influence が道路本体を汚しすぎない
- [ ] building halo が広がりすぎない
- [ ] 交差点 boost が degree>=3 ノード周辺でわずかに明るい

### Elevation Gradient

- [ ] 標高グラデーション（低→中→高）が視認できる
- [ ] 急斜面ティントが斜面部分に適切に適用される

### Water Visual / Depth Gradient

- [ ] Dark theme で水面が ground から十分に読み分けられる
- [ ] Theme 切替だけで water の base / shallow / deep / foam 色が追従する
- [ ] Coastal で「中央が深い / 岸が浅い」グラデーションが見える
- [ ] Rural で浅瀬・蛇行部の色差が見える
- [ ] Grid で直線寄りの運河が毎回見え、道路横断部が水面に沈まない
- [ ] Mountain で谷筋の渓流が見え、深浅差が完全な単色にならない

### Z-fighting / Regression

- [ ] Ground / Road / Water の Z-fighting が発生しない
- [ ] 道路描画・建物配置・水面表示に副作用がない
- [ ] Grid preset で地形が平坦でも shading が破綻しない
- [ ] 4プリセット x 2テーマで大破綻がない

### 道路検証 (Gate-1 統合 — 旧 SP-028)

> Gate-1 は SP-032 統合検証に吸収 (2026-03-12 決定)。
> blocker 5件は修正済み (commit 099bf56)。以下を SP-032 と同時に確認する。

- [ ] 道路が欠損なく不透明に表示される (Render)
- [ ] tier ごとの標示 (中央線・破線・エッジ線) が破綻しない (Markings)
- [ ] degree>=3 交差点の円盤拡張に穴・重なり・Z-fight がない (Intersection)
- [ ] 建物が道路メッシュへ明確にめり込まない (Setback)
- [ ] Coastal/Grid→Modern、Rural/Mountain→Rural が自動バインドされている (AutoBind)
- [ ] Dark/Parchment 切替で道路の marking/curb 色が追従する (Theme Sync)

### Per-Preset 期待値 (SP-032)

| プリセット | hillshade | contour | moisture | road/building | water |
| ---------- | --------- | ------- | -------- | ------------- | ----- |
| Coastal | 中 (0.55) | 中 (0.25) | 高 (0.45) | 中 | 海岸の浅瀬 + 河口寄りの深場 |
| Rural | 中 (0.5) | 低 (0.2) | 最高 (0.5) | 低 | 緩やかな川、浅瀬で色差あり |
| Grid | 低 (0.25) | 最低 (0.1) | 低 (0.2) | 最高 (0.4/0.35) | 直線寄りの運河、橋で横断 |
| Mountain | 最高 (0.7) | 最高 (0.35) | 高 (0.4) | 低 | 谷筋の渓流、狭い流路 |

---

## SP-060/061/062: Interior Interaction 検証

建物入場→Discovery収集→ドア操作→階移動→Tab探索メニュー→退出→マーカー→セーブ/ロード の一連の体験を検証する。

### 前提条件

- Bootstrap済みシーンで Play
- 建物が表示されている状態でマップ上の建物に近づく
- 建物入場は BuildingInteraction コンポーネント経由（建物をクリック）

### 建物入場・退出 (InteriorController)

- [ ] 建物クリック → Interior 描画が表示される
- [ ] カメラが建物俯瞰モードに切り替わる
- [ ] プレイヤーが Entrance 部屋にテレポートする
- [ ] 建物外の exterior が引き続き表示される（BuildingFade shader による屋根フェード）
- [ ] Escape キー → 建物退出
- [ ] 退出後にプレイヤーが元の位置に復帰する
- [ ] カメラが通常の Follow モードに復帰する
- [ ] 退出後に Interior オブジェクトがクリアされる（Hierarchy で確認）
- [ ] 連続入場/退出（3回以上）でクラッシュ・メモリリークなし

### Discovery 収集 (SP-060 DiscoveryInteractable)

- [ ] 家具オブジェクト（Container/Document/Photo/Note）が Interior 内に描画される
- [ ] Discovery の近くで E キー → 「Press E to collect」プロンプトが表示される
- [ ] E キー押下 → Discovery が消える / 収集済みになる
- [ ] 同じ Discovery を再度収集できない（IsAvailable = false）
- [ ] Console にエラーなし

### ドア操作 (SP-060 DoorInteractable)

- [ ] 施錠ドアが赤色インジケーターで表示される
- [ ] 開放ドアが緑色インジケーターで表示される
- [ ] 施錠ドアは通行を物理的にブロックする（blockingCollider）
- [ ] Container 型 Discovery を収集 → 対応するドアが自動解錠（鍵-ドア 1:1 紐づけ）
- [ ] 解錠後に blockingCollider が除去され通行可能になる
- [ ] 隠しドアがプレイヤー近接（1.2m以内）で出現する

### 階移動 (SP-062 StairInteractable)

- [ ] Stairwell 部屋に StairUp/StairDown インタラクタブルが存在する
- [ ] 階段の近くで E キー → フロア切替が発生する
- [ ] 上の階に移動: 現在フロア非表示 → 上階のみ表示
- [ ] 下の階に移動: 現在フロア非表示 → 下階のみ表示
- [ ] テレポート先が移動先フロアの Stairwell 位置になる
- [ ] 最上階で StairUp なし / 最下階で StairDown なし（境界値正常）
- [ ] フロアラベル（1F/2F/B1 等）が正しい

### 探索進捗 (SP-061 ExplorationProgressManager)

- [ ] 建物入場時に BuildingExplorationRecord が作成される
- [ ] 地上階 (floor 0) が visited としてマークされる
- [ ] 階移動で該当フロアが visited としてマークされる
- [ ] Discovery 収集が record に反映される（collectedDiscoveries 増加）
- [ ] 全 Discovery 収集 + 全フロア踏破 → IsComplete = true

### 探索メニュー (SP-061 ExplorationMenuUI)

- [ ] I キー → 探索メニュー表示
- [ ] 訪問済み建物一覧が表示される
- [ ] 各建物のフロア踏破数 (n/n)、Discovery 収集数 (n/n) が表示される
- [ ] 鍵/ドア状態が表示される
- [ ] 完了建物と未完了建物が区別できる

### マップマーカー (SP-061 BuildingSpawner)

- [ ] 建物退出後にマップ上にマーカーが表示される
- [ ] 未完了建物と完了建物でマーカーが区別される

### セーブ/ロード連携 (SP-061 SaveManager)

- [ ] 建物探索後にセーブ → ロード → 探索記録が復元される
- [ ] ロード後にマップマーカーが正しく表示される
- [ ] ロード後に同じ建物に再入場 → 前回の収集済み状態が維持される

### SP-020 Layer 1: Discovery テキスト表示 (DiscoveryTextSystem)

- [ ] Discovery 収集時に Toast でテキストが表示される（type名ではなく文章）
- [ ] テキストが建物カテゴリに応じて異なる内容になる（Residential vs Commercial etc.）
- [ ] Common テキスト: 白色で表示、2秒で消える
- [ ] Uncommon テキスト: 青みがかった色 (0.4, 0.7, 1.0) で表示、2秒で消える
- [ ] Rare テキスト: 金色 (1.0, 0.85, 0.3) で表示、4秒で消える
- [ ] 同一建物内で同じテキストが重複出現しない（セッション重複除去）
- [ ] 別の建物に入り直すと重複カウンタがリセットされる
- [ ] Console に `[DiscoveryTextSystem] Loaded X entries` ログが出る
- [ ] Console にエラーなし（JSON パース失敗等）

### 建物近接フィードバック

- [ ] 建物に近づくと色が変化する（emission ハイライト）
- [ ] [E] キーヒントが表示される
- [ ] 離れるとハイライトが消える

---

## Troubleshooting

### Bootstrap後にプレイしてもマップが表示されない
- `MapManager` の `activePreset` が null でないか確認
- Console でエラーが出ていないか確認

### デバッグオーバーレイが表示されない
- Tab キーを押して切替（Off → Analysis → Terrain → Off の3段階）
- `AnalysisVisualizer` の `mapManager` 参照が設定されているか確認

### F1 の Control Panel が表示されない
- Play 中に Game ビューへフォーカスした状態で `F1` を押す
- Hierarchy に `UICanvas/MapControlPanel` があるか確認する
- Console の先頭例外が `SceneBootstrapper.SetupInteractionUI` / UI 生成周辺なら bootstrap が途中停止している
- Gate-1 実行前に `docs/verification/road-p4-gate-runbook.md` の補足も参照する

### 装飾が全く配置されない
- プリセットの `decorationDensity` が 0 でないか確認
- Grid プリセットは hillDensity=0 なので地形装飾は出ない（正常動作）

### steepnessBias が効いていない
- 既存プリセット .asset ファイルは steepnessBias=0 がデフォルト
- Inspector で各プリセットの steepnessBias を 0.5 に設定する

### `git push` が失敗する
- 本環境は HTTPS + Git Credential Manager (GCM) を利用
- 再確認手順は `docs/git-auth-troubleshooting.md` を参照
