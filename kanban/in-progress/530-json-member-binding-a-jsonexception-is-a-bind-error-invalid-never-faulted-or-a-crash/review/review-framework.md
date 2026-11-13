# Review framework — task 530

**Date:** 2026-11-14
**Host task:** kanban/in-progress/530-json-member-binding-a-jsonexception-is-a-bind-error-invalid-never-faulted-or-a-crash/
**Diff scope:** branch `task/530-json-member-binding-a-jsonexception-is-a-bind` vs `origin/master` (`origin/master...HEAD`). Product delta is commit `6e507acb` plus the executable bit in `c01bcaa1`: `json-args-emitter.assign.cs`, `json-args-emitter.validation.cs`, `nuru-bind-error.cs`, `documentation/user/features/agent-invocation.md`, `tests/timewarp-nuru-mcphost-tests/mcphost-06-json-member-bind-errors.cs` (+343/−6).
**Plan / brief:** A `JsonException` (and the serializer's `NotSupportedException` / `InvalidOperationException`, plus `FormatException`) while deserializing a JSON object or array member becomes a `NuruBindError` (`InvalidValue`) for that member: exit 1 / `error=Invalid` in process, through the daemon, and over McpHost. A handler exception stays `Faulted`. The Nuru beta cut and the Roslynk follow-up stay later steps.
**Effort:** 2 (general only; by-diff budget, not extra specialists)
**Reviewer roster:** general
**Session IDs:** review session id not exposed (print mode). Turn cap 120 (--max-turns; this cursor session uncapped).

## Budget (by-diff)

- Lines changed: 730
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- This round did not run `./bin/dev build`, `./bin/dev test`, `ganda repo audit`, or the git hooks
