# Runtime State

> Compact continuation state for AI sessions. Human-facing project context
> lives in `docs/project-context.md`.

## Current Position

- project: MiniMapGame
- branch: master
- session: 15 (2026-06-15)
- lane: Rendering Model Reassessment
- slice: SP-041 patch/glyph structure transition
- phase_within_slice: repository maintenance completed; Canvas 2D patch/glyph structure remains the next visual implementation direction

## Active Artifact

- active_artifact: browser-preview observed surface layer
- artifact_surface: browser-preview/index.html
- last_change_relation: Codex project-local model/version pins were removed and a MkDocs Material local documentation view was added without changing the active visual direction

## Visual Evidence

- visual_evidence_status: failed for the old ImageData path
- rejected_attempts: shader translation, separated alpha layers, unguided glyph scatter, enum surface classification
- recovered_local_evidence: `browser-preview/preview-*.png` files from the local Phase C decoration pass are preserved but stale relative to SP-041
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

## Repository Maintenance Handoff

- remote_sync: `origin/master` was already equal to local `master` before the maintenance changes
- codex_config: `.codex/config.toml` no longer pins a model or Codex version; `.codex/environments/environment.toml` was removed from tracked files and `.codex/environments/` is ignored
- local_docs_view: `mkdocs.yml` serves `docs/` with MkDocs Material for local tree-pane review and temporary browser translation
- nav_helper: `tools/generate-doc-nav.py` prints a non-destructive nav candidate from Markdown placement and lists root-level Markdown outside `docs_dir` for review only
- validation: `python -m mkdocs build` and `python -m mkdocs build --strict` both completed with exit code 0
- source_safety: existing tracked Markdown files were not meaningfully rewritten for the docs view; `docs/index.md` is the only new Markdown entrypoint

## Resume Checks

- Start by reading `docs/ai/AGENT_RULES.md`, then `docs/project-context.md`,
  this file, `docs/spec-index.json`, and `docs/specs/observed-surface-synthesis.md`.
- For local documentation review, run `python -m mkdocs serve` and open
  `http://127.0.0.1:8000/`; browser translation is temporary reading aid only.
- Verify the browser preview after any visual change by opening
  `browser-preview/index.html`.
- Do not use the recovered Phase C screenshots as current acceptance evidence;
  regenerate screenshots after any patch/glyph implementation.
