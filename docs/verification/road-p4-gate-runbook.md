# Gate-1 Runbook: P4 Road Manual Verification

## 目的

P4 道路レンダリング刷新の手動検証を、最小の迷いで再現可能に実施する。
対象は以下の確認:

- 道路描画
- 車線標示
- 交差点円盤拡張
- 建物セットバック
- プリセットごとの自動 RoadProfile バインド
- Theme 切替時の道路色同期

`FAIL` が 1 件でもあれば、B / D / E / W-2 へ進まない。

---

## 前提

- Unity Editor で `MiniMapGame > Bootstrap Test Scene` が実行済み
- Play 開始後に地図が自動生成される
- Game ビューをアクティブにした Play 中に `F1` で Control Panel を開ける
- 必要なら `F2` で Verification Checklist を開ける
- `docs/verification/road-p4-gate-results.md` を同時に開いて記録する

---

## 推奨 seed

### 最低要件（8ケース）

- `12011`

### 推奨フル確認（24ケース）

- `12011`
- `28473`
- `53190`

同一 seed を Dark / Parchment のペアで使う。
問題の再現が不安定な場合のみ追加 seed を使い、`Notes` に明記する。

---

## 最短の回し方

1. Unity で `MiniMapGame > Bootstrap Test Scene`
2. Play 開始
3. `F1` で Control Panel を開く
4. seed に `12011` を入力
5. `Coastal` を選択して生成
6. `Dark` で確認し、`C01` を記録
7. 同じ生成状態のまま `Parchment` を押し、`C02` を記録
8. 同じ流れで `Rural -> C03/C04`, `Grid -> C05/C06`, `Mountain -> C07/C08`

推奨の 24 ケースを回す場合は、上記 4-8 を seed ごとに繰り返す。

---

## ケース実行順（推奨）

### Seed `12011`

- `C01` Coastal / Dark / `12011`
- `C02` Coastal / Parchment / `12011`
- `C03` Rural / Dark / `12011`
- `C04` Rural / Parchment / `12011`
- `C05` Grid / Dark / `12011`
- `C06` Grid / Parchment / `12011`
- `C07` Mountain / Dark / `12011`
- `C08` Mountain / Parchment / `12011`

### 追加 seed（推奨）

結果ファイルの `Notes` に `seed=28473`, `seed=53190` を追記して同形式で追加行を複製する。

---

## 各ケースの確認項目

### 1. Render

`OK` 条件:

- 道路が欠損しない
- 不透明で表示される
- tier ごとの幅感が極端に崩れない

### 2. Markings

`OK` 条件:

- tier ごとに標示の密度と種類が不自然でない
- 中央線・破線・エッジ線が破綻しない

### 3. Intersection

`OK` 条件:

- degree>=3 の交差点で円盤拡張が破綻しない
- 穴、極端な重なり、目立つ Z-fight がない

### 4. Setback

`OK` 条件:

- 建物が道路メッシュへ明確にめり込まない
- RoadProfile 連携の最低セットバックが機能している

### 5. AutoBind

`OK` 条件:

- Coastal / Grid -> Modern 相当
- Rural / Mountain -> Rural 相当

### 6. Theme Sync

`OK` 条件:

- Dark / Parchment 切替で道路の marking / curb 色が追従する
- geometry を再生成しなくても色更新が見える

---

## 記録ルール

- `Render / Markings / Intersection / Setback / AutoBind / Theme Sync` は `OK` か `NG`
- `Result` は全項目 `OK` のときのみ `PASS`
- 1 項目でも `NG` があれば `FAIL`
- `Notes` には再現条件、目立つ位置、追加 seed の有無を書く

---

## Findings 記法

`docs/verification/road-p4-gate-results.md` の Findings は次で埋める。

- `ID`: `F-001`, `F-002`, ...
- `Severity`: `CRITICAL` / `WARNING`
- `Case ID`: `C01` など
- `Description`: 何がどう壊れているかを短く
- `File/Area`: 想定箇所
- `Action`: 修正タスク化 / 再検証 / 調査

例:

| ID | Severity | Case ID | Description | File/Area | Action |
|----|----------|---------|-------------|-----------|--------|
| F-001 | WARNING | C03 | Tier1 車線標示が Rural で過密に見える | `Road.shader` / `RoadProfile_Rural` | パラメータ見直し後に C03/C04 再検証 |

---

## NG 時の切り分けヒント

### Render が NG

- `MapRenderer.RenderEdges`
- road material 割当
- `Road.shader`

### Markings が NG

- `RoadProfile` tier 設定
- `MapRenderer.ApplyProfileToMaterial`
- `Road.shader`

### Intersection が NG

- `MapRenderer.RenderIntersections`
- `intersectionRadiusFactor`
- 交差点用 material 生成

### Setback が NG

- `BuildingPlacer`
- `RoadProfile.TotalWidth` 参照

### AutoBind が NG

- `SceneBootstrapper.AutoBindRoadProfiles`
- preset asset の `roadProfile`

### Theme Sync が NG

- `ThemeManager.ApplyRoadMaterials`
- road material instance / shared material の扱い

---

## 完了判定

### PASS

- 最低 8 ケースで `FAIL = 0`

### FAIL

- 1 件でも `FAIL`
- Findings を記録し、修正タスク化して同一ケースを再実行

---

## 補足

- Theme ペアは**同一生成状態**で切り替えて比較するのが最短
- 追加 seed を使う場合も、Dark -> Parchment の順を維持すると比較しやすい
- 結果テンプレートは `docs/verification/road-p4-gate-results.md` を使う

### F1 パネルが出ないとき

- まず Console の先頭例外を確認する
- `SceneBootstrapper` の UI 構築例外が出ている場合、bootstrap が途中停止している
- `MapControlUI` の入力は Play 中かつ Game ビューがフォーカスされている必要がある
- `UICanvas/MapControlPanel` が Hierarchy にあるか確認する
