# Make EndpointBuilder.Build register the open route before building

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding R-1 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

`EndpointBuilder.Build()` forwards to `ParentBuilder.Build()` and never calls `Done()`. The generator registers a route only in `IrRouteBuilder.Done()`. `DispatchBuild` ignores a receiver that is not an `IrAppBuilder`, so `.Map(...).WithHandler(...).Build()` drops the open route and does not mark the app built. `RunAsync` then hits the stub that throws.

`Done()` stays the documented way to finish a route. `Build()` on the endpoint must not look like a second, working way to finish the chain and then discard the route.

Evidence: `source/timewarp-nuru/builders/endpoint-builder.cs` (`Build`), `source/timewarp-nuru-analyzers/generators/ir-builders/ir-route-builder.cs` (`Done` / `RegisterRoute`), `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs` (`DispatchBuild`). Parent record: `review/runtime-core.md` R-1.

## Checklist

- [x] Calling `Build()` on an `EndpointBuilder` that has a handler registers that route, then builds the app
- [x] A chain that already called `Done()` still builds once and does not register the route twice
- [x] Add a generator or runtime test that fails today on `.Map("ping").WithHandler(() => 0).Build()`

## How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/builder/builder-02-endpoint-builder-build.cs
```

Expect: The new test passes. `.Map("ping").WithHandler(() => 0).Build()` produces an app whose `ping` route runs. A chain that calls `Done()` then `Build()` still has exactly one `ping` route.

## Session

- Created: 524751 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)
- Implemented: claude-opus-5-5 implement oracle under ganda task work (2026-10-05)
- Review: grok 01a10b0a-d1af-74d2-9457-5aad508055e5 (2026-10-05)

## Results

- `DslInterpreter.DispatchBuild` now completes an open route (`IIrRouteBuilder`) through the
  same `TryDoneRoute` path as `Done()` (including the NURU parameter-mismatch diagnostic), then
  builds the returned app builder. Before, it returned `null` for a route-builder receiver, so the
  route was dropped and the app never marked built.
- `IrRouteBuilder.Done()` registers its route at most once (`IsRegistered` guard), so a route
  completed by both `Done()` and `Build()` cannot be registered twice.
- `EndpointBuilder.Build()` XML doc now states it is equivalent to `.Done().Build()`; `Done()`
  stays the documented way to finish a route.
- New test `tests/timewarp-nuru-tests/builder/builder-02-endpoint-builder-build.cs`
  (`EndpointBuilderBuildTests`, runs in CI multi-mode). Verified the two open-route tests fail
  with `InvalidOperationException` without the generator fix and pass with it.
- CI suite: `dotnet run tests/ci-tests/run-ci-tests.cs` exit 0 — 3767 passed, 12 skipped, 0 failed.

### Review

- Rounds: 1. Roster: general. Effort: 1.
- Round 1: no findings. `DispatchBuild` completes an open route through `TryDoneRoute` (same diagnostic path as `Done()`), then builds the app. `IrRouteBuilder.Done()` registers at most once.
- Final counts: bug 0 open / 0 fixed / 0 wontfix. Suggestion 0. Nit 0. Disposition: **clean**. No wontfix. No escalation.
- Re-ran `dotnet run tests/timewarp-nuru-tests/builder/builder-02-endpoint-builder-build.cs`: 3 passed.
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/builder/builder-02-endpoint-builder-build.cs
dotnet run tests/ci-tests/run-ci-tests.cs
```

Expect: `builder-02` reports 3/3 passed — `.Map("ping").WithHandler(() => 0).Build()` runs
`ping` with exit 0, the open-route handler output appears, and a `Done()`-then-`Build()` chain
lists the `ping` route exactly once in help. The CI runner exits 0 with no failures.

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
- Implementation review: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`. Outcome clean (1 round, general, effort 1). No findings.
