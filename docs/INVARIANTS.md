# Invariants

Status: supplemental

This file is not a normal resume entrypoint and must not be treated as the
project source of truth. Normal resume uses only the chain in
`docs/ai/AGENT_RULES.md`.

Use this file as a task-specific invariant catalog only when the active task
touches deterministic generation, pipeline ordering, rendering/data
separation, UX invariants, or when a task spec explicitly points here.
If this file conflicts with `docs/ai/AGENT_RULES.md`,
`docs/project-context.md`, `docs/runtime-state.md`, `docs/spec-index.json`,
or an active task-specific spec, fix the conflict before relying on this file.

破ってはいけない条件・責務境界・UX不変量を保持する補助カタログ。

## Architecture Invariants

### Deterministic Reproduction

- 全生成は同一 int seed で決定論的再現。同じ seed + 同じ preset = 同じマップ
- SeededRng (XOR-shift) が唯一の乱数源。Math.random() / UnityEngine.Random は使用禁止
- RNG 呼び出し順序はパイプラインステップ順で厳密。ステップ内の条件分岐による RNG 消費は許容するが、ステップ順の変更は seed 互換性を破壊する

### Pipeline Step Independence

- MapManager.Generate (C#) / generateMap (JS) のパイプラインは明示的なステップ順で構成
- 各ステップは前ステップの出力データのみに依存。ステップ間でグローバル状態を共有しない
- ステップの追加は末尾 or 既存ステップ間に挿入可能。ただし RNG 消費順に影響する場合は新 seed 系列を検討
- ステップの削除・差し替えは、後続ステップの入力データが満たされる限り可能

### Data/Logic と Rendering の分離

- 生成ロジック (Data/Core/MapGen) は描画方法に依存しない。Canvas 2D / WebGL / Unity Mesh のいずれでも同じ生成コードが動く
- C# 版: MonoBehaviour 依存は Runtime 層 (MapManager, MapRenderer 等) のみ
- JS 版: map-gen.js (生成) と renderer.js (描画) は完全分離。renderer は mapData オブジェクトを受け取るだけ

### Strategy Pattern for Generators

- IMapGenerator (C#) / GENERATORS オブジェクト (JS) で Generator を差し替え可能
- 新 GeneratorType 追加時は Generate() の入出力インターフェースを守る
- 既存 Generator のパラメータ変更は非破壊的 (seed 互換性は別途判断)

### Parameter Centralization

- 全調整可能パラメータは MapPreset / WaterProfile / RoadProfile に集約 (C#: ScriptableObject, JS: PRESETS オブジェクト)
- ジェネレータ内のマジックナンバーはプリセット拡張対象候補。ただし直ちに SO 化するのではなく、ビジュアル反復で調整対象になった時点で昇格

## Visual Iteration Invariants

### 「見え方から逆算して構造を再構築する」プロセスの耐性

- ブラウザプレビュー (browser-preview/) は、マップ生成結果を即座に視覚確認し、パラメータや配置ロジックを反復調整するためのツール
- この反復でコードが壊れない条件:
  1. **生成と描画の分離**: 描画方法の変更が生成ロジックに波及しない
  2. **ステップ独立性**: 一つのステップの変更が他ステップの入出力を壊さない
  3. **パラメータ集約**: 調整対象が散在せず、preset で一元管理
  4. **seed 再現性**: 変更前後で同じ seed の出力を比較できる

### ビジュアル反復ワークフロー

1. seed を固定してマップを生成
2. ブラウザプレビューで出力を確認
3. パラメータまたはロジックを調整
4. 同一 seed で再生成し、差分を目視確認
5. 満足したら別 seed でも破綻しないことを確認

### 構造再構築時の安全ルール

- ジェネレータの追加・差し替え: Strategy パターンに従い、入出力インターフェースを守る
- パイプラインステップの追加: 前後ステップとのデータ依存を明示
- データ構造の拡張: 既存フィールドは削除しない。新フィールドは optional (デフォルト値あり)
- RNG 消費順の変更: seed 互換性が破壊されることを承知の上で実施。意図しない RNG 順変更は禁止

## UX / Algorithmic Invariants

- WASD 三人称操作モデル (NavMesh 削除済み)
- カメラは Perspective (3D orbit/pan/zoom)。Interior 時のみ Orthographic
- Discovery テキストは空間客観描写 (人間要素・固有名詞・時間軸・主観知覚を排除)
- 建物近接フィードバックは色変化 + emission 方式

## Responsibility Boundaries

- AI は執筆しない。制作システム整備が役割
- EPUB/DOCX 出力はスコープ外
- OAuth / Electron 配布は現フェーズ対象外

## Prohibited Interpretations / Shortcuts

- rejected を「工程不要」と解釈しない
- ユーザー未指定の固有名詞・方式を勝手に採用しない
- 振り子判断 (「前回 UI が多かったから次はコンテンツ」) で作業を選ばない
- Math.random() / UnityEngine.Random を生成パイプライン内で使用しない

## 運用ルール

- ユーザーが一度説明した非交渉条件は、同一ブロック内でここへ固定する
- `project-context.md` の DECISION LOG には理由を短く残し、ここには条件そのものを残す
