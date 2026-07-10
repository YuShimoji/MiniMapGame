# Project Context

## Current Position

- Project: MiniMapGame
- Environment: Unity 6.3 LTS (6000.3.6f1) / C# / URP 17.3.0 / InputSystem 1.18.0
- Branch strategy: trunk-based (`master`)
- Active lane: Rendering Model Reassessment
- Active slice: SP-041 patch/glyph structure transition
- Active artifact: browser-preview observed surface layer, then Unity runtime transfer if the visual model proves useful
- Current bottleneck: the map visual model still lacks convincing observed-surface density; more implementation on the old ImageData path does not address that bottleneck
- Repository readiness: local Unity import/script compilation is green after the 2026-07-10 preflight repair; runtime visual acceptance remains separate

## Canonical Resume Chain

Normal resume uses only this chain:

1. `docs/ai/AGENT_RULES.md`
2. `docs/project-context.md`
3. `docs/runtime-state.md`
4. `docs/spec-index.json`
5. Task-specific spec files referenced by `docs/spec-index.json`

Session notes, refresh notes, audit reports, delegation prompts, and archived
decision logs are historical material. Do not use them as active resume
entrypoints.

## Document Authority Guardrails

- `SPEC.md` is a legacy aggregate specification. It may be opened only when
  `docs/spec-index.json` points to a legacy entry or when performing
  historical/spec migration work. It does not define current position,
  next work, final product scope, or acceptance targets.
- `docs/INVARIANTS.md` is a supplemental invariant catalog, not a resume
  entrypoint. Read it only for tasks touching deterministic generation,
  pipeline ordering, rendering/data separation, or when a task spec points to
  it.
- `docs/delegation-prompts.md` is an inert utility. Its task descriptions and
  read order are not active instructions unless regenerated from the current
  resume chain.
- Specs with `legacy`, `merged`, `deprecated`, or `postponed` status in
  `docs/spec-index.json` are not active work sources.

## Active Context

- SP-040 remains the visual north star.
- SP-041 defines the observed-surface layer below SP-040.
- Four ImageData-based attempts have been rejected as implementation direction:
  shader translation, separated alpha layers, unguided glyph scatter, and enum
  surface classification.
- Next visual direction: field-driven patch/glyph synthesis using Canvas 2D path
  operations such as `Path2D`, `clip`, `createPattern`, and
  `globalCompositeOperation`.
- `browser-preview` is a fast direction probe. It is not the final quality gate
  and not a shader implementation source of truth.
- Unity PlayMode/manual verification is still needed for final runtime behavior,
  but it is not the active lane until the visual model is clarified.

## Recovered Local Context

- The merged local branch carried `df0e0c3`, which ports C# `DecorationPlacer`
  behavior into `browser-preview/map-gen.js` and draws generated decoration
  details from `browser-preview/renderer.js`.
- That decoration work is preserved as browser-preview detail, but it does not
  override the active SP-041 direction. The next useful visual move is still
  field-driven patch/glyph structure, not more ImageData tuning.
- The same local commit carried an older note that the final deliverable was
  both browser preview and Unity game. That note is retained here only as
  historical context; the current canonical final product definition remains
  undefined until the user defines it explicitly.
- Local preview screenshots from the Phase C decoration pass are retained under
  `browser-preview/preview-*.png`. Treat them as stale evidence predating the
  SP-041 direction change, not as proof that the current observed-surface model
  is acceptable.

## Development Readiness (2026-07-10)

- `origin/master` was fetched and pulled at `da37e60`; local and remote were
  ahead/behind `0/0` before this readiness batch began.
- A Unity 6000.3.6f1 batch import exposed three compile blockers: a missing UI
  namespace import, a missing LINQ import, and a missing `BuildingSpawner`
  parameter in `SceneBootstrapper`. All three were repaired, and the final
  batch import/script compile exited with code 0.
- Unity generated 17 previously missing `.meta` files. Asset metadata coverage
  is now complete; these files must travel with the source changes so a fresh
  clone does not generate new GUIDs.
- `tools/validate-project.ps1` now runs the MkDocs strict build, browser-preview
  JavaScript checks, canonical lane/slice/spec-reference consistency, Unity
  `.meta` coverage, and project-version Unity compile as one preflight.
- The browser screenshot path rendered seed 42 for Organic, Grid, Mountain, and
  Rural through the installed Chrome channel. This proves the preview opens and
  renders; it does not make the stale SP-041 visual direction acceptable.
- There are no project-owned EditMode or PlayMode test assemblies. Player
  build, PlayMode behavior, and current SP-041 visual acceptance remain
  unverified and must not be inferred from compile success.
- Before this batch, the public GitHub repository had no README, deployed Pages
  site, or initialized Wiki. This delivery adds a thin README entrypoint;
  GitHub Pages from the existing MkDocs source remains the recommended external
  view but is not yet enabled.

## Collaboration Workflow

- A supervisor-to-developer prompt carries one decision-bearing slice, not an
  entire subsystem and not a chain of trivial edits.
- The developer closes implementation, in-scope fixes, automated verification,
  evidence, and canonical state synchronization in the same batch.
- Reversible technical decisions continue without incremental approval. The
  hard stops are destructive change, dependency addition, persistence/auth/API
  contract change, canonical conflict, and unapproved human-owned direction.
- High-ambiguity visual or product work uses two or three comparable low-cost
  directions before production work. Two consecutive NG results on the same
  model trigger a model/evaluation reset instead of another micro-tuning pass.
- External project views derive from these canonical files; they do not own a
  separately maintained copy of current status.

## Current Non-Goals

Do not automatically return to these areas without a fresh explicit request or
a verified blocker on the active visual path:

- Quest expansion or SP-001 Phase 3 work
- Interior integration/manual verification
- Unity manual verification as a substitute for visual-model repair
- Existing ImageData-based surface classification tuning
- Audio, menus, new gameplay systems, or production pipeline work

## Final Deliverable

The final product definition is currently undefined and requires explicit user
definition before it is used as an acceptance target.

Do not use prior session language about a fixed session length or a closed game
loop as an active acceptance criterion. Existing timer or quest code may remain
as implementation state, but it does not define the final product target.

## Effective Decision Log

| Date | Decision | Status | Reason |
|------|----------|--------|--------|
| 2026-07-10 | Use one decision-bearing slice per Development Packet and close code, related fixes, verification, evidence, and state sync together | active | Prevents both subsystem-sized blind implementation and follow-up prompt fragmentation |
| 2026-07-10 | Gate high-ambiguity creative work with comparable low-cost directions; reset the model after two consecutive NG results | active | Preserves human creative authority and prevents production-scale rework from collapsing into endless micro-adjustments |
| 2026-07-10 | Treat external project views as derived publication of canonical docs, not a hand-maintained Wiki/status copy | active | Removes update drift while keeping repository-external visibility possible |
| 2026-06-15 | Do not pin Codex models or generated environment versions in project-local config | active | Model/version selection belongs to the Codex app or global settings; project-local pins caused startup fragility |
| 2026-06-15 | Use MkDocs Material under the `docs/` boundary for local document review | active | Enables tree-pane browsing and browser translation without copying, translating, summarizing, or moving canonical Markdown |
| 2026-04-27 | Tighten documentation authority boundaries | active | Prevent old aggregate specs, supplemental invariant catalogs, merged plans, and delegation templates from acting as hidden resume or next-work sources |
| 2026-04-06 | Use `browser-preview` as a fast visual direction probe before Unity transfer | active | Iteration speed is needed for palette, layering, and readability decisions; Unity remains the final runtime check |
| 2026-04-06 | Reject the current ImageData-based observed-surface direction | active | The attempts reduce visual density to per-pixel color decisions and lose patch structure, anisotropy, and internal detail |
| 2026-04-06 | Move SP-041 toward field-driven patch/glyph synthesis | active | Observed-surface density needs fields, grammars, and repeated marks rather than scalar surface classes |
| 2026-04-06 | Keep SP-040 as the parent visual direction and SP-041 as the observed-surface child spec | active | SP-040 owns visual goals and layer responsibilities; SP-041 owns the surface-density method |
| 2026-03-26 | Remove the old tactical GameLoop implementation and use SP-001/GameSession-based code as the replacement path | active | HP, encounter, extraction, and old UI code no longer match the exploration direction |
| 2026-03-26 | Remove the MiniGame subsystem | active | The subsystem was disconnected from the current exploration flow |
| 2026-03-18 | Keep Discovery text as objective spatial/environmental description | active | Text should avoid human actors, proper nouns, timeline claims, and subjective perception |
| 2026-03-11 | Use WASD third-person movement and no NavMesh movement path | active | Exploration control is direct movement; the old NavMesh model caused editor/runtime friction |

## Handoff Snapshot

- Main lane: Rendering Model Reassessment
- Current slice: SP-041 patch/glyph structure transition
- Next useful implementation target: Canvas 2D patch/glyph structure drawing in
  `browser-preview`
- First task-specific spec to read: `docs/specs/observed-surface-synthesis.md`
- Reference implementation file: `browser-preview/observed-surface.js`, retained as
  context but not as a successful model
- Recovered implementation file: `browser-preview/map-gen.js` now includes
  generated decorations from the local Phase C work
- Local preflight entrypoint: `tools/validate-project.ps1`; the latest Unity
  6000.3.6f1 batch import/script compile completed with exit code 0
- Local documentation review entrypoint: `python -m mkdocs serve`, then open
  `http://127.0.0.1:8000/`; this view is not a new source of truth
- External Project Hub: README entrypoint included; GitHub Pages deployment is
  recommended but not configured
- Open design points: patch extraction algorithm, archetype-specific glyph
  placement rules, final product definition, external Project Hub activation
