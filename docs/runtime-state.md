# Runtime State

> Compact continuation state for AI sessions. Human-facing project context
> lives in `docs/project-context.md`.

## Current Position

- project: MiniMapGame
- branch: master
- session: 16 (2026-07-10)
- lane: Rendering Model Reassessment
- slice: SP-041 patch/glyph structure transition
- phase_within_slice: repository sync/readiness and collaboration workflow repair completed; Canvas 2D patch/glyph structure remains the next visual implementation direction

## Active Delivery Packet

- packet: repository-readiness-and-collaboration-workflow-2026-07-10
- packet_state: local implementation and validation complete; external Project Hub activation remains a separate user-owned choice
- source_anchor: `da37e60` was local and remote HEAD before this packet
- autonomy_result: three compile blockers, missing Unity metadata, portable preflight, and prompt workflow rules were handled in one batch
- last_runtime_evidence: Unity 6000.3.6f1 batch import/script compile returned 0 after repairs
- external_publish_state: Pages not configured; README entrypoint included in this delivery and GitHub Pages recommended

## Active Artifact

- active_artifact: browser-preview observed surface layer
- artifact_surface: browser-preview/index.html
- last_change_relation: repository readiness and collaboration workflow were repaired without changing the active SP-041 visual direction

## Visual Evidence

- visual_evidence_status: failed for the old ImageData path
- rejected_attempts: shader translation, separated alpha layers, unguided glyph scatter, enum surface classification
- recovered_local_evidence: `browser-preview/preview-*.png` files from the local Phase C decoration pass are preserved but stale relative to SP-041
- browser_smoke_evidence: seed 42 rendered successfully for Organic, Grid, Mountain, and Rural through the installed Chrome channel; smoke output is under ignored `Logs/browser-preview-smoke/` and is not SP-041 visual acceptance
- final_runtime_evidence: pending Unity verification after a viable visual model exists

## Quantitative

- spec_entries: 40
- done: 22
- partial: 11
- todo: 0
- postponed: 3
- legacy: 1
- merged: 2
- deprecated: 1

## Pending Questions

- final product definition: undefined, requires explicit user definition
- patch extraction algorithm: unresolved
- archetype-specific glyph placement rules: unresolved
- external Project Hub: whether to add pinned documentation dependencies, GitHub Actions deployment, and enable GitHub Pages

## Repository Maintenance Handoff

- remote_sync: `git fetch --prune` and `git pull --ff-only origin master` completed; local and remote were both `da37e60` with ahead/behind `0/0` before current local changes
- compile_repairs: added `MiniMapGame.UI`, added `System.Linq`, and passed `BuildingSpawner` into `SetupInteriorSystem`; final Unity batch compile returned 0
- asset_metadata: Unity generated 17 missing `.meta` files; metadata coverage is now complete and must be committed with the associated assets
- preflight: `tools/validate-project.ps1` combines MkDocs strict build, browser JavaScript checks, canonical lane/slice/spec-reference consistency, metadata coverage, and Unity import/script compilation
- browser_capture: `browser-preview/screenshot.js` is path-portable, checks all four generator types through an installed Chromium channel, uses an ignored output directory by default, refuses implicit Playwright installation, and completed a four-preset smoke capture
- local_docs_view: `mkdocs.yml` presents a Project Hub with active direction and historical/reference boundaries; it remains a derived view
- external_view: before this batch the public repository had no README, Pages deployment, initialized Wiki, workflows, releases, or visible Projects; this delivery adds a thin README without duplicating mutable status
- automated_tests: no project-owned EditMode or PlayMode test source/assembly exists; Player build and PlayMode smoke remain unverified

## Resume Checks

- Start by reading `docs/ai/AGENT_RULES.md`, then `docs/project-context.md`,
  this file, `docs/spec-index.json`, and `docs/specs/observed-surface-synthesis.md`.
- Run `tools/validate-project.ps1` before closing an implementation batch; use
  skip flags only for intermediate fault isolation.
- For local documentation review, run `python -m mkdocs serve` and open
  `http://127.0.0.1:8000/`; browser translation is temporary reading aid only.
- Verify the browser preview after any visual change by opening
  `browser-preview/index.html`.
- Do not use the recovered Phase C screenshots as current acceptance evidence;
  regenerate screenshots after any patch/glyph implementation.
