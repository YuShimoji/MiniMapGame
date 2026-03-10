# Debug Setup & Verification Guide

## Quick Start

### 1. Bootstrap Test Scene

1. Unity Editor を開く
2. メニュー: `MiniMapGame > Bootstrap Test Scene`
3. 以下が自動生成される:
   - MapManager + Renderer/Spawner群
   - 4プリセット (Coastal / Rural / Grid / Mountain)
   - 2テーマ (Dark / Parchment)
   - 3ロードプロファイル (Modern / Rural / Historic)
   - UI (MapControlUI / MiniMap / PlayerHUD)
   - カメラ + プレイヤー + ライティング
4. Play ボタンでマップが自動生成される

### 2. Runtime Controls

| キー | 機能 |
|------|------|
| **F1** | コントロールパネルの表示/非表示 |
| **Tab** | デバッグオーバーレイ切替: Off → Analysis → Terrain → Off |
| **WASD / 矢印** | プレイヤー移動 |
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

---

## Troubleshooting

### Bootstrap後にプレイしてもマップが表示されない
- `MapManager` の `activePreset` が null でないか確認
- Console でエラーが出ていないか確認

### デバッグオーバーレイが表示されない
- Tab キーを押して切替（Off → Analysis → Terrain → Off の3段階）
- `AnalysisVisualizer` の `mapManager` 参照が設定されているか確認

### 装飾が全く配置されない
- プリセットの `decorationDensity` が 0 でないか確認
- Grid プリセットは hillDensity=0 なので地形装飾は出ない（正常動作）

### steepnessBias が効いていない
- 既存プリセット .asset ファイルは steepnessBias=0 がデフォルト
- Inspector で各プリセットの steepnessBias を 0.5 に設定する

### `git push` が失敗する
- 本環境は HTTPS + Git Credential Manager (GCM) を利用
- 再確認手順は `docs/git-auth-troubleshooting.md` を参照
