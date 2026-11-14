# Review framework — task 535

**Date:** 2026-11-14
**Host task:** kanban/in-progress/535-add-purpose-and-design-regions-to-public-api-source-files/
**Diff scope:** `81633070` (`docs(source): add Design regions to public-API source files (535)`) against its parent, plus the kitchen land `e95f5752`. Merged `origin/master` history on this branch is out of scope.
**Plan / brief:** Comment-only `#region Design` (and `#region Open Questions` where intent is unsettled) on public-API source files. Goal restatement in `task.md`. Format from `tw-agent-context-regions`.
**Effort:** 3 (general only; by-diff, 1097 lines)
**Reviewer roster:** general
**Session IDs:** review oracle session not reported (cursor, grok 4.7)

## Budget (by-diff)

Round 3 re-review budget.

- Lines changed: 97
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Round 2 scope

**Date:** 2026-11-14
**Diff scope:** `e95f5752..0d3c82e3`. Product delta is the Design regions in `source/timewarp-nuru/telemetry/nuru-telemetry-options.cs` and `source/timewarp-nuru-parsing/message-type.cs` (commit `0d3c82e3`). Round 1 base is `review/round-1/base.sha`.
**Effort:** 1 (general)
**Reviewer roster:** general
**Session IDs:** review oracle session not reported (cursor, grok 4.7). Turn cap 80; cursor session uncapped (no `--max-turns` flag).

## Round 3 scope

**Date:** 2026-11-14
**Diff scope:** `0d3c82e3..17d2f8f7`. Product delta is the Design help clause in `source/timewarp-nuru-parsing/message-type.cs` (commit `17d2f8f7`). Round 2 base is `review/round-2/base.sha`.
**Effort:** 1 (general)
**Reviewer roster:** general
**Session IDs:** review oracle session not reported (cursor, grok 4.7). Turn cap 80; cursor session uncapped (no `--max-turns` flag).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
