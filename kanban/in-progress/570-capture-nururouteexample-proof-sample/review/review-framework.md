# Review framework — task 570

## Budget (by-diff)

- Lines changed: 447
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

**Date:** 2026-11-21
**Host task:** kanban/in-progress/570-capture-nururouteexample-proof-sample/
**Diff scope:** branch `task/570-capture-nururouteexample-proof-sample` vs base `master` (merge-base `2beac66d`). Three commits ahead, one behind (master commit `4434889f` touches this `task.md` with no content change). Product delta: `Directory.Packages.props` plus `proof/` (runfile, capture script, committed `--help` / `--capabilities` / `--json-args` output).
**Plan / brief:** Capture a small sample pinned to published Nuru 4.0 beta that declares `[NuruRouteExample("add 2 3", Description = ...)]`, commit real `--help` and `--capabilities` output, and runtime-check documented `--json-args` including stdin `-`.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** implementer claude `d96f45fb-6348-4705-a660-2fb3ab1ffa14` (2026-11-21); review grok `01a1fbfe-3684-7332-8e67-b2bcea883f19` (2026-11-21)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
