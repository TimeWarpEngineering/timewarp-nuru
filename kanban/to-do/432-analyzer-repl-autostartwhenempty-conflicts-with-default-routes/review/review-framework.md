# Review framework — task 432

**Date:** 2026-09-28
**Host task:** kanban/to-do/432-analyzer-repl-autostartwhenempty-conflicts-with-default-routes/
**Diff scope:** branch `task/432-analyzer-repl-autostartwhenempty-conflicts-with-de` vs `origin/master` (`f679a9cb`, `68f82fa0`, `2844de0f`). Product surface: `ReplDefaultRouteValidator`, `DiagnosticDescriptors.ReplAutoStartConflictsWithDefaultRoute`, `ModelValidator` wiring, empty-pattern location retention in `NuruGenerator.GetFluentRouteLocation`, `generator-49-nuru-r004-repl-default-route.cs`, CI standalone wiring, `AnalyzerReleases.Unshipped.md`, and `documentation/developer/design/resolver/specificity-algorithm.md`.
**Plan / brief:** Report NURU_R004 (Error) from the generator model when `ReplModel.AutoStartWhenEmpty` is true and a top-level default route (`""`, no group prefix) is registered via fluent `Map("")`, `Map<T>()` of `[NuruRoute("")]`, or `DiscoverEndpoints()`. A `""` route inside a group does not conflict. No diagnostic when AutoStartWhenEmpty is false or `AddRepl()` is absent. Document NURU_R004 next to NURU_R001–R003.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor implementer-cursor profile, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
