# Review framework — task 497

**Date:** 2026-11-15
**Host task:** kanban/in-progress/497-cscheck-pilot-fuzz-route-parsing-with-random-argv/
**Diff scope:** branch `task/497-cscheck-pilot-fuzz-route-parsing-with-random-argv` vs merge-base `b41073ac` (local master), HEAD `947e2216`. Review scope is the task 497 product diff (691 lines in the by-diff budget): `Directory.Packages.props`, `tests/ci-tests/Directory.Build.props`, `tests/timewarp-nuru-tests/routing/routing-41-argv-fuzz-property.cs`, and this kitchen `task.md`. The range also contains the merge of `origin/master` (task 520 telemetry benchmark); that merge is outside this review.
**Plan / brief:** CsCheck 4.9.1 pilot, test-only. A property feeds generated argv into a Nuru app and asserts `RunAsync` never throws and always ends with a defined exit code. Pin the seed, print a replayable seed on failure, do not use `Faster` in build-blocking tests, and record what the pilot found.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** reviewer grok (Grok 4.7), body tw-implementation-review, print mode. Session not reported. Turn cap 120; cursor session uncapped (no `--max-turns` flag).

## Budget (by-diff)

- Lines changed: 691
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
