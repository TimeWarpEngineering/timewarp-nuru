# Review framework — task 482-001

**Date:** 2026-10-05
**Host task:** kanban/to-do/482-001-make-endpointbuilderbuild-register-the-open-route-before-building/
**Diff scope:** commits `b31a0255` and `55c3c2ce` vs parent `95e6422c`. 123 insertions, 10 deletions (133 lines) across the generator, `EndpointBuilder` docs, the builder-02 runfile, and this kitchen. Do not review other tasks that are also on this branch relative to `origin/master`.
**Plan / brief:** Finding R-1 from task 482. `EndpointBuilder.Build()` must register the open route the way `Done()` does, then build the app. A chain that already called `Done()` must not register that route twice. `.Map("ping").WithHandler(() => 0).Build()` must run.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok 01a10b0a-d1af-74d2-9457-5aad508055e5 (2026-10-05)

## Budget (by-diff)

- Lines changed: 133
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
