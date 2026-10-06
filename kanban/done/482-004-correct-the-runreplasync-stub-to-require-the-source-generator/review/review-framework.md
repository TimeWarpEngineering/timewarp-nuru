# Review framework — task 482-004

**Date:** 2026-10-05
**Host task:** kanban/to-do/482-004-correct-the-runreplasync-stub-to-require-the-source-generator/
**Diff scope:** commit `eb8d9860` (78 insertions, 7 deletions). Product files: `source/timewarp-nuru/nuru-app.cs`, `tests/timewarp-nuru-tests/repl/repl-48-run-repl-async-stub-message.cs`. Do not review other tasks that are also on this branch relative to `origin/master`.
**Plan / brief:** Finding R-6 from task 482. The `RunReplAsync` fallback must tell the caller to enable the source generator and that `AddRepl()` was called. The XML exception doc and the thrown string agree. A test asserts the thrown message.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok 01a10b69-695f-7993-8369-4a7454a72d28 (2026-10-05)

## Budget (by-diff)

- Lines changed: 85
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
