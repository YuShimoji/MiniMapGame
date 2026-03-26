# Runtime State

> このファイルは作業セッション間の継続性を保つための状態追跡。
> 人間が読むドキュメントではなく、AI セッションの再開用。

## Current Position

- project: MiniMapGame
- branch: master
- session: 9 (2026-03-26 nightshift)
- lane: Advance (SP-001 Quest基盤) + Excise (MiniGame削除) + リファクタ
- slice: SP-001 Phase 1 (実装完了・Unity検証待ち)
- phase_within_slice: Phase 1 全実装完了。Phase 2 (クエスト拡充+HUD) 未着手

## Counters

- blocks_in_session: 3
- blocks_since_user_visible_change: 0 (Quest基盤 = direct)
- blocks_since_visual_audit: 3
- consecutive_excise_blocks: 0
- consecutive_cleanup_or_evidence_only: 0

## Active Artifact

- active_artifact: 5分セッション型マップ探索ゲーム
- artifact_surface: Unity Editor Play Mode (BootstrapTestScene)
- last_change_relation: direct (Quest基盤実装)

## Quantitative

- spec_entries: 38
- done: 23
- partial: 9
- todo: 3
- legacy: 1
- merged: 2
- deprecated: 1
- test_files: 0
- mock_files: 0
- impl_files: ~107 (MiniGame 7削除 + Quest 4追加)
- todo_fixme_hack: 0

## Visual Evidence

- visual_evidence_status: unknown
- last_visual_audit_path: (none)
- blocks_since_visual_audit: (never)

## Pending Questions

- 最終成果物像 (ゲーム単体 vs 動画制作Pipeline) → 未定義
- SP-011 legacy ラベル: InteriorController は現役だが SP-011 は legacy。ラベル見直しが必要か
- SceneBootstrapper 分割: 1465行、Editor-only。分割の実益は限定的
