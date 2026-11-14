# Review framework — task 531

**Date:** 2026-11-14
**Host task:** kanban/in-progress/531-decide-whether-withnurutools-on-an-aspnet-builder-chain-is-supported-then-fix-the-generator-or-the-doc/
**Diff scope:** Task product delta is commit `19657a81` (`fix(generator): only NuruApp.CreateBuilder starts a Nuru app (531)`) on branch `task/531-decide-whether-withnurutools-on-an-aspnet-builder`: `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.expressions.cs` and `tests/timewarp-nuru-mcphost-tests/mcphost-07-host-builder-chain.cs`. The branch also contains `origin/feature/overnight-2026-11-14` (through `55696ba5`); that stack is outside this review. `origin/master...HEAD` is not the task delta.
**Plan / brief:** `## Goal (implementer restatement)` in `task.md`. `WithNuruTools` on an ASP.NET chain is supported; the generator was treating every `CreateBuilder` as `NuruApp.CreateBuilder`.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** implement oracle session not reported; review oracle session not reported (Cursor, Grok 4.7)

## Budget (by-diff)

- Lines changed: 318
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
