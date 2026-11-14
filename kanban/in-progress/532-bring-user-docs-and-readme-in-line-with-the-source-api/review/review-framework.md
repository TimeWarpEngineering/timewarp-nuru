# Review framework — task 532

**Date:** 2026-11-14
**Host task:** kanban/in-progress/532-bring-user-docs-and-readme-in-line-with-the-source-api/
**Diff scope:** Product commit `496d2b3f` (`docs: bring README and user docs in line with the source API (532)`) against its parent. Files: `readme.md`, `documentation/user/features/output-handling.md`, `routing.md`, `telemetry.md`, `terminal-abstractions.md`, `documentation/user/guides/best-practices.md`, `documentation/user/reference/supported-types.md`, `documentation/user/use-cases.md`. Merges of `origin/master` on this branch are other tasks already on master, not this change. The implementer's `## Results` and `### How to validate` are the gate record.
**Plan / brief:** Correct README and user-doc snippets so they follow the source public API (code plus Purpose/Design). Listed defects: `CreateBuilder(args)` / `EnableStaticCompletion`, two-argument `Map`, `WithGroup`, unreachable `Map("")` beside `{*args}`, handler parameter names, telemetry `ActivitySource`. File a code task when the source says the behavior should exist and the package does not deliver it. Logging (527) and MCP/`WithNuruTools` (531) stay out of the broken-snippet target.
**Effort:** 2 (general only; budget roster axes: general)
**Reviewer roster:** general
**Session IDs:** reviewer grok (grok-4.7), body tw-implementation-review, print mode. Session id not reported. Turn cap 120; cursor session uncapped (no `--max-turns` flag).

## Budget (by-diff)

- Lines changed: 590
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
