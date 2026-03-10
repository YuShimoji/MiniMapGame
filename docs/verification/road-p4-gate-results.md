# Gate-1 Results: P4 Road Manual Verification

## Summary

- Date: 2026-03-09
- Executor: Codex (preflight) / User (Unity manual run)
- Unity:
- Result: `PENDING`
- Blocker for next tasks (B/D/E/W-2): `YES`
- Minimum seed set: `12011`
- Recommended seed set: `12011`, `28473`, `53190`

See also: `docs/verification/road-p4-gate-runbook.md`

## Preflight (Automated)

- [x] `origin/master` と同期済み（pull済み）
- [x] Preset assets 4種が存在（Coastal/Rural/Grid/Mountain）
- [x] Theme assets 2種が存在（Dark/Parchment）
- [x] RoadProfile assets 3種が存在（Modern/Rural/Historic）
- [x] `SceneBootstrapper.AutoBindRoadProfiles` のマッピング実装を確認
  - Coastal(Organic) / Grid -> Modern
  - Rural / Mountain -> Rural

## Case Matrix

記録ルール:

- `Render / Markings / Intersection / Setback / AutoBind / Theme Sync` は `OK` or `NG`
- `Result` は全項目 `OK` のとき `PASS`、1項目でも `NG` があれば `FAIL`
- Dark / Parchment は同一 seed のペアで記録する
- 推奨: 同じ生成状態のまま Theme を切り替えて隣接ケースを埋める

| Case ID | Preset | Theme | Seed | Render | Markings | Intersection | Setback | AutoBind | Theme Sync | Result | Notes |
|---------|--------|-------|------|--------|----------|--------------|---------|----------|------------|--------|-------|
| C01 | Coastal | Dark | 12011 |  |  |  |  |  |  |  |  |
| C02 | Coastal | Parchment | 12011 |  |  |  |  |  |  |  |  |
| C03 | Rural | Dark | 12011 |  |  |  |  |  |  |  |  |
| C04 | Rural | Parchment | 12011 |  |  |  |  |  |  |  |  |
| C05 | Grid | Dark | 12011 |  |  |  |  |  |  |  |  |
| C06 | Grid | Parchment | 12011 |  |  |  |  |  |  |  |  |
| C07 | Mountain | Dark | 12011 |  |  |  |  |  |  |  |  |
| C08 | Mountain | Parchment | 12011 |  |  |  |  |  |  |  |  |

`Render/Markings/Intersection/Setback/AutoBind/Theme Sync`: `OK` or `NG`

追加 seed を回す場合:

- 上の 8 行を複製し、Seed を `28473` または `53190` に置換して追記する
- 追加ケースの Case ID は `C09` 以降を採番する

## Findings

Severity:

- `CRITICAL`: 次タスクへ進めない重大破綻
- `WARNING`: 軽微ではないが局所的。修正後に同ケース再検証

| ID | Severity | Case ID | Description | File/Area | Action |
|----|----------|---------|-------------|-----------|--------|
| F-001 |  |  |  |  |  |

## Decision

- Go/No-Go:
- Next task:
- Comment:

判定メモ:

- `Go`: 全ケース `PASS`、Findings に未解決項目なし
- `No-Go`: `FAIL` が 1 件でもある、または Findings が未解決
