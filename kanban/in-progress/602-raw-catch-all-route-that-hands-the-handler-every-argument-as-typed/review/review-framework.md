# Review framework

## Budget (by-diff)

- Lines changed: 1410
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-27
**Host task:** kanban/in-progress/602-raw-catch-all-route-that-hands-the-handler-every-argument-as-typed/
**Diff scope:** branch `task/602-raw-catch-all-route-that-hands-the-handler-every` vs `master` (merge base), commits ea8c010f and 5f9d194d
**Plan / brief:** `{**args}` raw catch-all (and `[Parameter(IsCatchAll = true, IsRaw = true)]`), NURU_S011, docs, skill, changelog
**Effort:** 3 (by-diff). The roster axis is general only.
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle db32ea30-4109-48f3-b176-2a2ff984f511

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to
  the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
