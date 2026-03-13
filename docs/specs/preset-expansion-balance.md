# SP-035: プリセット拡充・バランス調整

**Status**: done
**Category**: data
**実装率**: 100%

## 概要

既存 4 プリセットの俯瞰シルエット差を広げ、`Island` / `Downtown` / `Valley` を
既存ジェネレータの組み合わせだけで追加する。

本タスクではゲームコード側の分岐追加は行わず、`MapPreset` ScriptableObject と
`MapPresetCreator` の基準値更新だけで対応する。

## 実装結果

### 既存 4 プリセットの再調整

| Preset | 主目的 | 主な変更 |
| ------ | ------ | -------- |
| Coastal | waterfront / ring road の視認性向上 | 道路曲率を上げつつ建物密度を少し下げ、低い沿岸丘陵を追加 |
| Rural | farmland / river corridor を広く見せる | 建物密度を下げ、丘陵を rolling hills 寄りに再調整 |
| Grid | Downtown と役割分離 | canal を残したまま中密度まで落とし、微地形だけ残してフラット寄りへ |
| Mountain | high-relief silhouette 強化 | stream を保持しつつ建物をさらに削減し、標高と steepness を引き上げ |

### 新規 3 プリセット

| Preset | Generator | 狙い | 主な値 |
| ------ | --------- | ---- | ------ |
| Island | Organic | 小型 shoreline-dominant map | `620x620`, coast on, ring road on, historic road profile |
| Downtown | Grid | 高密度・低起伏の都市核 | `720x520`, buildingDensity `0.98`, maxElevation `1.2` |
| Valley | Mountain | river-cut chokepoint corridor | `640x720`, river on, steepnessBias `0.94` |

## Generator-specific field applicability

- `hasRingRoad` は `OrganicGenerator` のみで参照される。
- `arterialRange` は `OrganicGenerator` / `RuralGenerator` では実効する。
- `arterialRange` は `GridGenerator` / `MountainGenerator` では参照されないため、
  preset asset 上では「設計意図の保持」と「将来 UI 表示用の値」として残す。

## SPEC CAPTURE

### 1. Island は「全周海岸」ではなく asset-only の島風 preset とする

現行 `WaterGenerator.DetermineCoastSide()` / `GenerateCoast()` は 1 辺 coastline 前提のため、
コード変更なしでは true island を構成できない。

そのため今回の `Island` は次の仕様で定義する:

- 小さい正方形ワールド
- `Organic` + `hasCoast=true`
- 高めの coastline 支配率と環状道路で「小型の離島港」らしく読む

true island（全方向 coastline / 閉領域 coast）は follow-up タスク扱いとする。

### 2. Grid と Downtown を役割分離する

既存 `Grid` が高密度すぎると `Downtown` 追加後の見分けが弱くなるため、
`Grid` は「読みやすい中密度の canal grid」へ戻し、
`Downtown` を flat / compact / very dense な極値プリセットに寄せる。

### 3. Valley は Mountain の亜種として「狭い通路」を world shape で作る

専用 generator を追加せずに chokepoint 感を出すため、`Mountain` generator を使いながら:

- `worldWidth` を狭める
- `worldHeight` を少し伸ばす
- `hasRiver=true` にして水系 carving を追加
- `curveAmount` を base Mountain より少し抑え、縦方向 corridor の可読性を確保する

## 運用メモ

- `MapControlUI` の既定ボタンは 4 種のまま。追加 3 種は Inspector 手動割当または将来の UI 拡張で使用する。
- `MapPresetCreator` は clean project 向けに 7 種すべての基準値を生成する。
