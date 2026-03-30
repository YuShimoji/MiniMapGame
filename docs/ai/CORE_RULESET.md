# CORE_RULESET.md
Ruleset-Version: v18
Status: canonical
Audience: Claude Code, Codex, and any adapter that reads project-local AI rules.

## Purpose
This ruleset exists to keep a single vendor-neutral source of truth for AI-assisted development.
Adapters such as `.claude/CLAUDE.md` and `AGENTS.md` must stay thin and defer here.

## Source-of-truth policy
- The canonical rules live in `docs/ai/*.md`.
- Adapters, prompts, hooks, and helper agents are subordinate.
- Project-local canonical docs (`INVARIANTS`, `USER_REQUEST_LEDGER`, `OPERATOR_WORKFLOW`, `INTERACTION_NOTES`) are factual project memory, not optional decoration.
- If a rule conflicts with project-local canonical docs, first verify whether the docs reflect newer explicit user instruction.

## Core principles
### Artifact-first
Advance the active artifact or its verified delivery path. Docs, cleanup, tests, mocks, and surveys are supporting work unless they clearly unblock the artifact.

### Explain Once Canonicalization
If the user states a durable constraint, workflow pain, invariant, backlog item, or prohibited shortcut, write it into the appropriate canonical doc in the same block. Do not postpone that write to handoff.

### Question Dedup
Before asking, read the canonical rules and project-local canonical docs. Summarize what is already known, then ask only for missing deltas. Do not ask the user to re-explain known context.

### Frontier discipline
Do not re-open rejected, boundary-stopped, or quarantined frontiers as normal next steps. User interest in “looking again” is not automatic approval.

### Selection is not approval
If the user chooses a proposed item for deeper review, that means “evaluate/specify this next”, not “approve implementation”. Keep status semantics strict.

### No pendulum compensation
Do not choose work because the previous sessions were “too much X” and therefore the next one should be “not-X”. Choose work based on the current bottleneck.

### Actor/owner discipline
Every major action has an actor and an owner artifact.
- actor = who performs the work now (`user`, `assistant`, `tool`, `shared`)
- owner = who owns the resulting artifact or judgment
Do not silently slide human-owned creative work into assistant execution.

### Read-only audit phases
REFRESH, REANCHOR, SCAN, AUDIT, and similar phases are read-only by default. They do not write repo state, commit, push, or mutate long-lived files unless the user explicitly asks for that mutation in the current block.

### Write failure hard stop
If a write fails, a readback mismatch occurs, or the result is uncertain, do not commit, push, or claim completion in that block. Repair or clearly stop.

## Canonical doc roles
- `INVARIANTS.md`: non-negotiables, UX/algorithm invariants, role boundaries, prohibited shortcuts
- `USER_REQUEST_LEDGER.md`: durable requests, backlog deltas, unresolved user corrections
- `OPERATOR_WORKFLOW.md`: human/operator workflow, pain points, quality goals, manual vs assisted steps
- `INTERACTION_NOTES.md`: reporting style, ask hygiene, disliked patterns, manual verification conventions

## Evidence discipline
Use visual or artifact evidence whenever relevant. If evidence is stale or unknown, say so. Do not substitute documentation for actual observation when the question is about behavior.
