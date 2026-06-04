# MiniMapGame Claude Entry

This file is a thin adapter for Claude Code.
It is not a project status document and must not contain current tasks,
decision logs, handoff notes, or long directory inventories.

## Required Read Order

Read these files in order before resuming work:

1. `docs/ai/AGENT_RULES.md`
2. `docs/project-context.md`
3. `docs/runtime-state.md`
4. `docs/spec-index.json`
5. The specific spec file(s) referenced by `spec-index.json` for the task

Do not add more startup documents during normal resume. Supplemental catalogs,
legacy aggregate specs, delegation prompts, session notes, and archived logs
are read only when the current task explicitly requires them.

## Canonical Boundary

- Canonical agent rules live in `docs/ai/AGENT_RULES.md`.
- Current project state lives in `docs/project-context.md`.
- Runtime continuation state lives in `docs/runtime-state.md`.
- Specification status lives in `docs/spec-index.json`.
- Task-specific specs live in `docs/specs/*.md`.
- Session notes, refresh notes, audits, delegation prompts, and archived
  decision logs are not resume entrypoints.
- `SPEC.md` is a legacy aggregate specification, not a current project-status
  or next-work authority.
- `docs/INVARIANTS.md` is a supplemental invariant catalog, not a normal
  resume entrypoint.

If this adapter conflicts with the canonical files above, follow the
canonical files and update this adapter later.
