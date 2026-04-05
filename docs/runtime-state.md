# Runtime State

> このファイルは作業セッション間の継続性を保つための状態追跡。
> 人間が読むドキュメントではなく、AI セッションの再開用。

## Current Position

- project: MiniMapGame
- branch: master
- session: 12 (2026-04-06)
- lane: Rendering Model Reassessment
- slice: SP-041 patch/glyph 構造転換
- phase_within_slice: 4段階の ImageData 試行は全て不採用。Canvas 2D パス描画 API への構造転換設計中

## Counters

- blocks_in_session: 2
- blocks_since_user_visible_change: 0 (observed-surface.js 新規作成 = direct)
- blocks_since_visual_audit: 0 (検証待ち)
- consecutive_excise_blocks: 0
- consecutive_cleanup_or_evidence_only: 0

## Active Artifact

- active_artifact: browser-preview observed surface layer (構造転換中)
- artifact_surface: ブラウザ (browser-preview/index.html)
- last_change_relation: corrective (文脈整理 — 4段階不採用の固定)

## Visual Evidence

- visual_evidence_status: failed — 4段階全て不採用 (shader直訳 / レイヤー分離 / glyph散布 / surface classification)
- last_visual_audit_path: ユーザー目視 2026-04-06 (Organic/Grid/Mountain/Rural x Parchment/Dark)

## Quantitative

- spec_entries: 40 (SP-041 追加)
- done: 23
- partial: 11 (SP-041 追加)
- todo: 3
- legacy: 1
- merged: 2
- deprecated: 1

## Branch State

- local master: diverged from origin/master (9 local, 2 remote)
- browser-preview: restored from origin/master + modified (observed-surface.js 新規追加)
- 状態: rebase + push 済み (session 12 完了時点)

## Pending Questions

- 最終成果物像 (ゲーム単体 vs 動画制作Pipeline) → 未定義
- patch extraction のアルゴリズム選定 (連結成分 / 閾値 / α-shape) → Step 0 で決定
- archetype 別 glyph 配置ルールの詳細設計 → Step 1-3 で逐次決定
