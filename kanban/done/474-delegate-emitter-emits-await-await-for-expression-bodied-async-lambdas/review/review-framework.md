# Review framework — task 474

**Date:** 2026-09-26
**Host task:** kanban/to-do/474-delegate-emitter-emits-await-await-for-expression-bodied-async-lambdas/
**Diff scope:** branch `task/474-delegate-emitter-emits-await-await-for-expression` vs `master` (commits a69d3088, 3f633379)
**Plan / brief:** Stop the delegate emitter from prepending `await` to expression bodies of `async` lambdas
(which produced `await await …`). Add a `HasAsyncModifier` flag to `HandlerDefinition`, set it in
`HandlerExtractor`, and emit async expression bodies verbatim. Add generator-47 (runtime) and generator-48
(Roslyn-hosted emission assertions, standalone CI phase).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** ganda task-work review oracle (Cursor implementer-cursor profile, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
