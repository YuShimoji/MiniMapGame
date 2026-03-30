# WORKFLOWS_AND_PHASES.md
Ruleset-Version: v18
Status: canonical

## Recommended read order on resume / continue / refresh
1. `docs/ai/CORE_RULESET.md`
2. `docs/ai/DECISION_GATES.md`
3. `docs/ai/STATUS_AND_HANDOFF.md`
4. `docs/ai/WORKFLOWS_AND_PHASES.md`
5. `docs/INVARIANTS.md`
6. `docs/USER_REQUEST_LEDGER.md`
7. `docs/OPERATOR_WORKFLOW.md`
8. `docs/INTERACTION_NOTES.md`
9. runtime / context / registry / boundary docs

## Resume / Continue / Refresh
### Resume
Recover project-local canonical context first, then identify the active artifact and bottleneck.

### Continue
Do not rely on momentum. Re-check whether the current block still matches the bottleneck, actor, and value path.

### Refresh / Reanchor / Scan
These are read-only unless the user explicitly asks for mutation in the current block.
Do not auto-fill newly initialized project docs and commit them as “refresh work”.
Initialization may be prepared, but long-lived writes belong to an explicit write block.

## Task-scout requirements
A scout pass should include, when relevant:
- active artifact and bottleneck
- stale evidence / visual evidence freshness
- user-carried constraints
- re-ask risk
- canonical coverage
- value path risk
- bottleneck substitution risk
- actor risk

## Manual verification pattern
- Put verification items in normal text, not inside the ask field.
- Ask only for `OK / NG` or a short result code.
- Ask for next direction separately.

## Option generation
Each major option should show:
- lane (`Advance`, `Audit`, `Excise`, `Unlock` or another justified lane)
- actor
- owner artifact
- bottleneck addressed
- what becomes possible if done

Avoid options whose main meaning is merely commit / not commit / cleanup only / end.

## Workflow-proof examples
Good workflow-proof tasks:
- validate that the human-authoring path runs once end-to-end
- confirm the operator can use the designed toolchain without improvising new steps
- move a verification target into a debug harness instead of using main content as the experiment bed

## Interaction safety
Do not compress unrelated intents into one ask.
Do not use markdown tables in a short ask field.
Do not present broad re-explanation prompts when canonical context already exists.

## Commit and push hygiene
Commit/push are not primary next-direction choices.
They are follow-through actions after a justified block, not substitutes for strategy.
