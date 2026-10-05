# Make EndpointBuilder.Build register the open route before building

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding R-1 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

`EndpointBuilder.Build()` forwards to `ParentBuilder.Build()` and never calls `Done()`. The generator registers a route only in `IrRouteBuilder.Done()`. `DispatchBuild` ignores a receiver that is not an `IrAppBuilder`, so `.Map(...).WithHandler(...).Build()` drops the open route and does not mark the app built. `RunAsync` then hits the stub that throws.

`Done()` stays the documented way to finish a route. `Build()` on the endpoint must not look like a second, working way to finish the chain and then discard the route.

Evidence: `source/timewarp-nuru/builders/endpoint-builder.cs` (`Build`), `source/timewarp-nuru-analyzers/generators/ir-builders/ir-route-builder.cs` (`Done` / `RegisterRoute`), `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs` (`DispatchBuild`). Parent record: `review/runtime-core.md` R-1.

## Checklist

- [ ] Calling `Build()` on an `EndpointBuilder` that has a handler registers that route, then builds the app
- [ ] A chain that already called `Done()` still builds once and does not register the route twice
- [ ] Add a generator or runtime test that fails today on `.Map("ping").WithHandler(() => 0).Build()`

## How to validate

Smoke:

```bash
dotnet test tests/timewarp-nuru-tests/timewarp-nuru-tests.csproj --filter FullyQualifiedName~EndpointBuilderBuild
# or the new test name this task adds
```

Expect: The new test passes. `.Map("ping").WithHandler(() => 0).Build()` produces an app whose `ping` route runs. A chain that calls `Done()` then `Build()` still has exactly one `ping` route.

## Session

- Created: 524751 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
