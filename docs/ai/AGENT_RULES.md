# AGENT_RULES.md
Ruleset-Version: v21
Status: canonical

## Purpose

This is the single vendor-neutral source of truth for AI-assisted work in
MiniMapGame. Adapter files such as `AGENTS.md` and `CLAUDE.md` must stay thin
and defer here.

## Resume Order

Read only this chain for normal resume:

1. `docs/ai/AGENT_RULES.md`
2. `docs/project-context.md`
3. `docs/runtime-state.md`
4. `docs/spec-index.json`
5. Task-specific spec files referenced by `spec-index.json`

Do not read session notes, refresh notes, audit reports, delegation prompts,
or archived logs as resume entrypoints unless the current task explicitly
requires historical investigation.

Do not expand the normal resume chain. If an always-needed rule cannot fit in
this file or `docs/project-context.md`, summarize the rule there instead of
adding another required startup document.

## Canonical Roles

- `docs/project-context.md`: current product/work state, active lane, active
  slice, effective decisions, and prohibited automatic returns.
- `docs/runtime-state.md`: compact machine-readable continuation state.
- `docs/spec-index.json`: specification registry and lifecycle status.
- `docs/specs/*.md`: task-specific product/technical specifications.
- `docs/INVARIANTS.md`: supplemental invariant catalog. It is not a normal
  resume entrypoint. Read it only when the active task touches generation
  determinism, pipeline ordering, rendering/data separation, or a
  task-specific spec explicitly points to it.
- `docs/archive/*`: historical context only, not active guidance.
- `SPEC.md`: legacy aggregate specification. Use it only when
  `docs/spec-index.json` explicitly references a legacy entry or when doing
  historical/spec migration work. It must not choose next work, override
  `docs/project-context.md`, or override task-specific specs.

Any other document that claims to be canonical, source of truth, required
startup reading, or a resume entrypoint is stale unless it is named in this
section. Fix that document before relying on it.

## Work Discipline

- Advance the active artifact or its verified delivery path.
- Do not choose work by pendulum logic, such as compensating for a previous
  session being too docs-heavy or too implementation-heavy.
- Do not reopen rejected, quarantined, boundary-stopped, or explicitly
  postponed work as a normal next step.
- Do not reopen `legacy`, `merged`, `deprecated`, or `postponed` spec entries
  as active work unless the user explicitly requests historical cleanup or
  `spec-index.json` is first updated to make that work active.
- Treat selection for deeper review as review only, not approval to implement.
- During refresh, reanchor, scan, or audit work, do not mutate long-lived files
  unless the user explicitly asks for that mutation.
- Treat `docs/delegation-prompts.md` as inert examples. They do not define
  current work, current read order, or current ownership. A delegation prompt
  is usable only after re-reading the normal resume chain including
  `docs/runtime-state.md`.

## Delivery Slice Protocol

- One delegation prompt should define one decision-bearing slice: one outcome
  the user can evaluate, not an entire subsystem and not one trivial edit.
- Regenerate the prompt from the canonical resume chain. Use
  `docs/delegation-prompts.md` only as a packet template, never as current
  authority.
- Keep implementation, in-scope related fixes, automated verification,
  evidence capture, and canonical state updates in the same delivery batch.
  Do not create follow-up prompts just for documentation, commit preparation,
  or minor corrections discovered inside the approved slice.
- Continue through reversible technical choices inside the slice and record
  material assumptions. Stop only for destructive change, dependency addition,
  persistence/authentication/API contract change, canonical specification
  conflict, or unapproved human-owned product or creative direction.
- Batch any required questions at the earliest meaningful decision point. Do
  not turn each implementation detail into a separate approval gate.
- Before production-scale work with high visual or product ambiguity, present
  two or three low-cost, genuinely different directions under comparable
  conditions. Selection authorizes only the selected direction and stated
  fidelity, not an unstated final-product definition.
- If the same visual or conceptual model receives two consecutive user NG
  results, stop micro-tuning it. Revisit the model, inputs, or evaluation axis
  before producing another variant.
- Report at the initial snapshot, at a decision-changing evidence checkpoint,
  and at close. Avoid status chatter that does not change user understanding.

## Questions And Ownership

- Before asking, verify whether the answer is already in the canonical chain.
- Ask only for missing deltas that affect scope, product direction,
  architecture, irreversible changes, or human-owned judgment.
- Every major action should have an actor and owner artifact:
  `user`, `assistant`, `tool`, or `shared`.
- Do not silently move human-owned creative judgment, manual verification, or
  final acceptance into assistant-owned execution.

## Evidence Discipline

- Use visual or artifact evidence whenever behavior or quality is in question.
- If evidence is stale or missing, say so plainly.
- Do not substitute documentation for observation when the question is about
  actual runtime behavior.

## External Project Views

- README, GitHub Pages, Wiki, Issues, and Projects are entrypoints or derived
  views, never project-state sources of truth.
- Do not manually duplicate active lane, slice, progress, or next work into an
  external surface. Link to or publish the canonical documents instead.
- Prefer automated publication from `docs/` over a separately edited Wiki.
  Publication failure must be visible, but does not block product development
  unless external delivery is the requested slice.

## Write Safety

- If a write fails, a readback mismatch occurs, or tool output is uncertain in
  a way that affects correctness, stop before commit, push, or completion
  claims.
- If project documents conflict, resolve the canonical chain first instead of
  accumulating another corrective note.

## Manual Verification And Reporting

- Run all available automated checks before requesting manual verification,
  and consolidate remaining checks into one coherent gate.
- Put manual verification items in normal text.
- Ask only for `OK / NG` or a short result code when requesting manual
  verification.
- Do not mix manual verification with next-direction choice in one ask.
- Avoid broad re-explanation prompts when canonical context already exists.
