# Review framework — task 598

**Date:** 2026-11-18
**Host task:** kanban/in-progress/598-move-dev-cli-content-gh-and-dotnet-shell-calls-to-the-amuru-beta-9-typed-builders/
**Diff scope:** branch `task/598-move-dev-cli-content-gh-and-dotnet-shell-calls-to` vs merge-base `9bb70be1` (master). 35 files, +360/−165. Product paths: `Directory.Packages.props`, `source/Directory.Build.props`, DevCli content and readme, `changelog.md`, `tools/dev-cli` endpoints, tests, benchmarks. Kanban move of `task.md` is in the same range and is not product behavior.
**Plan / brief:** Move every AMURU0001 `Shell.Builder("gh"|"dotnet")` / `Shell.Run` site onto the Amuru 3.0.0-beta.9 typed `Gh.*` / `DotNet.*` builders, pin both packages, document the DevCli content dependency, and bump the repo version to 4.0.0-beta.16 so `dev release` can ship DevCli after merge.
**Effort:** 2 (by-diff, 525 lines). Roster is general only.
**Reviewer roster:** general
**Session IDs:** grok `01a1ed4a-a831-71b2-8465-f26eb670ce0b`

## Budget (by-diff)

- Lines changed: 525
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
