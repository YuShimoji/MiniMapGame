# E Spec Audit: Road Width Table Alignment

## 目的

`SPEC.md` の道路幅テーブルと `BuildingPlacer` 実装の整合を固定し、
E（道路幅テーブル修正）着手時の手戻りを防ぐ。

## 決定事項

- 2026-03-09: 表記は `max,min` に統一する
- 値は現行実装に合わせて降順（例: `12, 8`）を正とする

## 照合表（仕様 vs 実装）

| Tier | SPEC 道路半幅 [max,min] | Code `TierRoadWidth` | SPEC 建物幅 [max,min] | Code `TierBuildingWidth` | 判定 |
|------|--------------------------|----------------------|------------------------|--------------------------|------|
| 0 | 12, 8 | 12, 8 | 16, 12 | 16, 12 | OK |
| 1 | 8, 5 | 8, 5 | 11, 8 | 11, 8 | OK |
| 2 | 5, 3 | 5, 3 | 7, 5 | 7, 5 | OK |

## 用語整理（誤読防止）

- `道路半幅`: 中心線から片側端までの距離
- `TotalWidth`: `RoadProfile` の全幅（両側）
- `profileHalfW`: `TotalWidth * 0.5`
- `effectiveHw`: `max(仕様半幅max, profileHalfW)` によるセットバック下限

## 参照

- `SPEC.md` §7 BuildingPlacer（道路幅テーブル）
- `Assets/Scripts/Core/BuildingPlacer.cs`
