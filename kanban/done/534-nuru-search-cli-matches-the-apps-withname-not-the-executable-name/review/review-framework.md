# Review framework — task 534

**Date:** 2026-11-14
**Host task:** kanban/in-progress/534-nuru-search-cli-matches-the-apps-withname-not-the-executable-name/
**Diff scope:** product commits `3c4f8f6a` and `64255366` on `task/534-nuru-search-cli-matches-the-apps-withname-not-the` (after merge `08f73d5c`). Search index resolver, `search` and `index clear` `--cli`, readme, `tw-cli-search`, `search-06` expectation, new `search-07-cli-key.cs`. Master merges on this branch are out of scope.
**Plan / brief:** `nuru search --cli` must find an indexed CLI by app name (`WithName` / capabilities name) or by executable file name. Help text, Purpose/Design, user docs, and a regression test agree with that key.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** implementer session not reported (implementer-claude, 2026-11-13). Reviewer session not reported (2026-11-14). Bound cap 120; this Cursor launch is uncapped.

## Budget (by-diff)

- Lines changed: 487
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
