# Analyzer: REPL AutoStartWhenEmpty conflicts with default routes

## Summary

Create a Roslyn analyzer that detects when `AddRepl(options => options.AutoStartWhenEmpty = true)` is used with a default route (empty string `""`), which creates an ambiguous situation where there's no way to distinguish between the REPL and the default route.

## Description

When configuring a Nuru CLI app, there's a conflict between:
1. REPL auto-start with empty args (`AutoStartWhenEmpty = true`)
2. Having a default route (route pattern `""`)

If both are present, there's no way to distinguish between:
- User wants the REPL (no arguments provided)
- User wants the default route (no arguments provided)

This analyzer should catch this at compile-time and report a diagnostic error.

## Update 2026-09-28 (triage 477): confirmed bug, current approach

**Confirmed on master:** `interceptor-emitter.cs` `EmitInteractiveFlag` (around lines 984 and 1308-1338)
emits the `AutoStartWhenEmpty` check (`routeArgs.Length == 0` → start REPL) *before* user routes are
matched, so a default route `""` is silently unreachable when both are configured. No diagnostic covers it.

**Implement it the way Nuru does diagnostics today**, not as a standalone `DiagnosticAnalyzer` class:
- Validators live in `source/timewarp-nuru-analyzers/validation/` (`model-validator.cs`,
  `overlap-validator.cs`, `handler-validator.cs`, `service-validator.cs`) and run over the generator model.
  Add the check there (in `model-validator.cs` or a new small validator wired the same way).
- Inputs: `ReplModel.AutoStartWhenEmpty` (`generators/models/repl-model.cs`) and the app's routes, covering
  fluent `.Map("")`, `[NuruRoute("")]` endpoints, and endpoints pulled in via `DiscoverEndpoints`. Only a
  top-level default route conflicts; `""` inside a group does not.
- Descriptor: add to `diagnostics/diagnostic-descriptors.overlap.cs` using the existing scheme. Next free id in
  the routing series is **NURU_R004** (R001-R003 are taken). Severity **Error**. Message along the lines of:
  "REPL AutoStartWhenEmpty makes the default route unreachable; remove the default route or disable
  AutoStartWhenEmpty."
- Tests: generator/analyzer tests in the existing style (see the NURU_R003 tests) for fluent, attributed, and
  DiscoverEndpoints default routes; no diagnostic for a `""` route inside a group; no diagnostic when
  AutoStartWhenEmpty is false or the REPL is not added.
- Document NURU_R004 wherever the other NURU_R diagnostics are documented.

Ignore "NURU001" and the analyzer class name in the checklist below; they predate the validator design.

## Checklist

- [x] Report NURU_R004 from the generator model (`ReplDefaultRouteValidator`), not a standalone analyzer class
- [x] Detect `.AddRepl()` calls with `AutoStartWhenEmpty = true`
- [x] Detect default routes: `[NuruRoute("")]` or `.Map("")` at the top level, including `DiscoverEndpoints()`
- [x] Report diagnostic error when both conditions are met
- [x] Add unit tests for the diagnostic
- [x] Add documentation for NURU_R004

## Results

`ReplDefaultRouteValidator` runs from `ModelValidator` over each app's combined fluent and endpoint routes. NURU_R004 (Error) fires when `ReplModel.AutoStartWhenEmpty` is true and a top-level route has pattern `""` and matches an empty argument list. A `""` route inside a fluent group or a `[NuruRouteGroup]` does not conflict. A `[NuruRoute("")]` with a required positional parameter does not match `routeArgs.Length == 0`, so it does not conflict; an optional parameter still does. The generated interceptor still starts the REPL before user routes when `AutoStartWhenEmpty` is on; the diagnostic makes that unreachable default route a compile error.

### How to validate

**Smoke:** `dotnet run tests/timewarp-nuru-tests/generator/generator-49-nuru-r004-repl-default-route.cs`

**Expect:** 9 passed, 0 failed. Fluent `Map("")`, `Map<T>()` of `[NuruRoute("")]`, `DiscoverEndpoints()`, and a `""` route with an optional parameter each report NURU_R004 with severity Error. A `""` route inside a group, a `""` route with a required parameter, `AutoStartWhenEmpty = false`, and no `AddRepl()` do not report NURU_R004.

### Review disposition

- **Outcome:** clean
- **Effort / roster:** 1 — general only
- **Rounds:** 2
- **Final counts:** bug 0 open, 1 fixed, 0 wontfix; suggestion 0; nit 0
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`
- M1 (false NURU_R004 on `[NuruRoute("")]` with a required parameter) fixed on this id. Re-verified: generator-49, 9 passed, 0 failed.

## Notes

### Problem Example
```csharp
// This configuration creates ambiguity:
NuruApp.CreateBuilder(args)
    .AddRepl(options =>
    {
        options.AutoStartWhenEmpty = true;  // Starts REPL when no args
    })
    .Map("").WithHandler(() => "Hello World")  // Default route
    .AsCommand().Done()
    .Build();

// Or with attribute:
[NuruRoute("", Description = "Say hello world")]
public class HelloCommand { }
```

### Expected Diagnostic
- **ID**: NURU001 (or next available)
- **Severity**: Error
- **Message**: "REPL AutoStartWhenEmpty cannot be used with a default route. Remove either the default route or disable AutoStartWhenEmpty."

### Implementation Notes
- The analyzer needs to check both attribute-based routes (`[NuruRoute]`) and fluent API routes (`.Map()`)
- Should only check `.Map("")` at the top level, not within sub-groups
- The conflict occurs when BOTH conditions are true in the same compilation unit

## Notes for the implementer

- **Commit and push your changes before reporting done.**
- Run the build and test gate in the foreground.
- 2026-09-28: review oracle (ganda task work, tw-implementation-review effort 1). Round 1 general — M1 bug (NURU_R004 false positive on a required parameter). Fixed on this id. Round 2 re-review — disposition clean. Next host nodes: open-pr / done (no apply-review sibling).
