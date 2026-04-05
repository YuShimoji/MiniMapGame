# Runtime State

> このファイルは作業セッション間の継続性を保つための状態追跡。
> 人間が読むドキュメントではなく、AI セッションの再開用。

## Current Position

- project: MiniMapGame
- branch: master
- session: 10 (2026-04-05)
- lane: Advance (SP-001 Quest Phase 2 + SP-040 文書化)
- slice: SP-001 Phase 2 (クエスト拡充+HUD) — プリセット別フィルタ実装中
- phase_within_slice: Phase 2 残: プリセット別フィルタ実装 → Unity検証

## Counters

- blocks_in_session: 1
- blocks_since_user_visible_change: 0 (プリセット別フィルタ + SP-040 = direct)
- blocks_since_visual_audit: 6
- consecutive_excise_blocks: 0
- consecutive_cleanup_or_evidence_only: 0

## Active Artifact

- active_artifact: 5分セッション型マップ探索ゲーム
- artifact_surface: Unity Editor Play Mode (BootstrapTestScene)
- last_change_relation: direct (プリセット別フィルタ + 30件クエスト + SP-040ビジュアル方針)

## Quantitative

- spec_entries: 39
- done: 23
- partial: 10
- todo: 3
- legacy: 1
- merged: 2
- deprecated: 1
- test_files: 0
- mock_files: 0
- impl_files: ~109 (MiniGame 7削除 + Quest 5追加 + QuestHUD)
- todo_fixme_hack: 0

## Visual Evidence

- visual_evidence_status: unknown
- last_visual_audit_path: (none)
- blocks_since_visual_audit: (never)

## Pending Questions

- 最終成果物像 (ゲーム単体 vs 動画制作Pipeline) → 未定義
- SP-011 legacy ラベル: InteriorController は現役だが SP-011 は legacy。ラベル見直しが必要か
- ~~プリセット別クエストフィルタの方式~~: QuestDefinition.allowedPresets で解決済み (session 10)
