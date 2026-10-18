# Review framework

## Budget (by-diff)

- Lines changed: 1154
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope — task 508-001

**Date:** 2026-10-18
**Host task:** kanban/to-do/508-001-attributes-only-package-timewarpnurucontracts-for-route-parameter-option-attributes-and-capabilities-types/
**Diff scope:** branch `task/508-001-attributes-only-package-timewarpnurucontracts-for` at `f522c6e2` vs merge-base `5e512c0a` with `master` (`git diff 5e512c0a..f522c6e2`), product paths only
**Plan / brief:** `task.md` `## Goal (implementer restatement)` and `## Requirements` — derive Nuru endpoints from `TimeWarp.Contracts` `[Operation]` requests, mediator dispatch, `IResponseRenderer`, problem rendering, sample and docs
**Effort:** 3
**Reviewer roster:** general (generator correctness), tests (runs builds, unit tests, full CI suite, sample smoke), plan_alignment (requirements, docs, restatement misread check)
**Session IDs:** review oracle (claude-opus-5-5, headless `ganda task work`), three Claude subagents; vendor session ids not reported

## Scope — round 2

**Date:** 2026-10-18
**Diff scope:** fix delta `git diff d769f8b1..e316fd32 -- . ':!kanban'` (22 product files, +1341/−293), plus re-verification of round-1 M1–M21 against `e316fd32`
**Effort:** 3
**Reviewer roster:** general (generator correctness on the fix delta, M2–M7, M9–M14, M18–M20), tests (builds, focused tests, full CI suite after `ganda runfile cache --clear`, sample and benchmark smoke, M1, M15, M16, M21), plan_alignment (requirements, docs, changelog, restatement, M8, M17)
**Session IDs:** review oracle (claude-opus-5-5, headless `ganda task work`), three Claude subagents; vendor session ids not reported

## Scope — round 3

**Date:** 2026-10-18
**Diff scope:** fix delta `git diff 572ecdc1..bb2d2b9f -- . ':!kanban'` (17 product files, about +1290/−140), plus re-verification of the round-2 open findings M20 and M22–M32 against `bb2d2b9f`
**Effort:** 3
**Reviewer roster:** general (generator correctness on the fix delta, M22–M26, M20, M30, M31), tests (builds, focused tests, full CI suite after `ganda runfile cache --clear`, sample and benchmark smoke, M27, M28), plan_alignment (restatement fix-pass acceptance, docs, changelog, diagnostics, M29, M32)
**Session IDs:** review oracle (claude-opus-5-5, headless `ganda task work`), three Claude subagents; vendor session ids not reported

## Scope — round 4

**Date:** 2026-10-18
**Diff scope:** fix delta `git diff bb2d2b9f..50e92396 -- . ':!kanban'` (16 product files, +823/−160), plus re-verification of the round-3 findings M33–M49 against `50e92396`
**Effort:** 3
**Reviewer roster:** general (generator correctness, focused tests, restatement and docs on the fix delta)
**Session IDs:** review oracle (grok-4.7, headless `ganda task work`), session `01a14fcd-3a2e-7e22-abc9-51e42db9f7fc`

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
