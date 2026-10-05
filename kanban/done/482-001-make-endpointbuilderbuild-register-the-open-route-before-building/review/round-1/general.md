# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** commits `b31a0255` and `55c3c2ce` vs `95e6422c` (`DslInterpreter.DispatchBuild`, `IrRouteBuilder.Done`, `EndpointBuilder.Build` docs, `builder-02-endpoint-builder-build.cs`)

## Summary

`DispatchBuild` now finishes an open `IIrRouteBuilder` through `TryDoneRoute` — the same path as `Done()`, including the `NURU` parameter-mismatch diagnostic — and then builds the returned app builder. `IrRouteBuilder.Done()` registers a valid route at most once, so `Done()` followed by `Build()` cannot add the route twice. `EndpointBuilder.Build()` still forwards to the parent at runtime; registration stays in the generator, and the XML docs state that this is `.Done().Build()`.

`IrAppBuilder.Map` parents the route on the sealed `IrAppBuilder`, so the existing `is IrAppBuilder` check still marks the app built. `GroupEndpointBuilder` has no `Build()`, so the new branch does not apply to group routes. Re-ran `dotnet run tests/timewarp-nuru-tests/builder/builder-02-endpoint-builder-build.cs`: 3 passed (open route exit 0, handler output `pong-open`, help lists `ping-once-marker` once). The runfile is mode `100755` and is included by the CI multi-assembly glob (not in `CiTestExcludes`).

No defects found.

## Issues

None.
