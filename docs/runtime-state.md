# Runtime State

> このファイルは作業セッション間の継続性を保つための状態追跡。
> 人間が読むドキュメントではなく、AI セッションの再開用。

## Current Position

- project: MiniMapGame
- branch: master
- session: 9 (2026-04-02)
- lane: Advance (ブラウザ鳥瞰図プレビュー構築)
- slice: browser-preview Phase A+B (道路網+建物+地形+水域)
- phase_within_slice: Phase A+B 完了。Phase C (装飾) 未着手

## Counters

- blocks_in_session: 1
- blocks_since_user_visible_change: 0 (ブラウザプレビュー Phase A+B = ユーザー可視)
- blocks_since_visual_audit: 0 (スクリーンショット 4 枚取得)
- consecutive_excise_blocks: 0
- consecutive_cleanup_or_evidence_only: 0

## Active Artifact

- active_artifact: ブラウザ鳥瞰図マッププレビューツール
- artifact_surface: browser-preview/index.html (ブラウザで直接開く)
- last_change_relation: direct (新規ツール構築)

## Quantitative

- spec_entries: 38
- done: 23
- partial: 10
- todo: 3
- legacy: 1
- merged: 1
- test_files: 0
- mock_files: 0
- impl_files: ~110 (C# Unity) + 3 (browser-preview JS/HTML)
- todo_fixme_hack: 0

## Visual Evidence

- visual_evidence_status: fresh (session 9)
- last_visual_audit_path: browser-preview/preview-*.png (4 枚: organic, grid, mountain, rural)
- blocks_since_visual_audit: 0

## Browser Preview Status

- Phase A (道路網+建物): 完了。4 Generator 全移植、BuildingPlacer、MapAnalyzer
- Phase B (地形+水域): 完了。TerrainGenerator、ElevationMap、WaterGenerator、WaterTerrainInteraction
- Phase C (装飾): 未着手。DecorationPlacer (355行) の移植が必要
- 移植行数: map-gen.js ~1,100行、renderer.js ~280行
- 描画要素: 道路 (3 tier Bezier)、建物 (4形状+ランドマーク)、丘陵ヒートマップ、海岸ポリゴン、河川 (可変幅+深度)

## Pending Questions

- 「5分間遊んで楽しいか」は AI session 7 の定式化。ユーザー本来の意図か → HUMAN_AUTHORITY
- ブラウザプレビューと Unity 版の最終的な関係 → HUMAN_AUTHORITY
- SP-001 Quest 基盤の優先度 (ブラウザ方向転換後) → HUMAN_AUTHORITY

## Session 9 実施内容

### ブラウザ鳥瞰図プレビュー Phase A+B (Advance)

- C# パイプライン棚卸し: Data 24本/Core 14本/MapGen 5本 = 43ファイル 3,921行を全読了
- 移植判定: 41 MUST / 2 SKIP (Interior専用)
- browser-preview/map-gen.js: SeededRng, MapGenUtils, SpatialHash, 4 Generators, BuildingPlacer, MapAnalyzer, TerrainGenerator, ElevationMap, WaterGenerator, WaterTerrainInteraction を JS 移植
- browser-preview/renderer.js: Canvas 2D 描画 (道路/建物/丘陵ヒートマップ/海岸/河川)
- browser-preview/index.html: ツールバー UI (seed/type/toggles/export)
- Playwright スクリーンショット 4 枚で全タイプ動作確認

### INVARIANTS.md 実内容化 (Docs)

- Architecture Invariants 5項目を新規記述 (Deterministic Reproduction, Pipeline Step Independence, Data/Logic-Rendering分離, Strategy Pattern, Parameter Centralization)
- Visual Iteration Invariants 3項目を新規記述 (反復耐性, ワークフロー, 安全ルール)
- UX/Algorithmic Invariants, Responsibility Boundaries, Prohibited Shortcuts をプロジェクト固有内容に更新

### AI 起源フレーズの特定

- 「5分間遊んで楽しいか」が AI session 7 の drift 診断 (refresh-2026-03-19.md) で定式化され、project-context.md に転記されていた事実を特定・報告
