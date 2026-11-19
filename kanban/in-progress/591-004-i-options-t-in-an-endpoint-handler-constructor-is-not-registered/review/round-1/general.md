# Round 1 — general
**Date:** 2026-11-19
**Scope reviewed:** `cdbd12eb..eccf6d71` (source generator emitters/extractors/validator, generator-69 and new generator-77 tests, migration guide, configuration page, migrate skill, changelog) and the kitchen `## Results` / `### How to validate`.

## Summary

The change stores the `IOptions<T>` section key and validator on endpoint `Handler` constructor bindings (reusing `HandlerExtractor.CreateServiceBinding`), registers one transient `IOptions<T>` per distinct type in the source-gen mediator container ahead of `AddGeneratedMediator`, and binds inline under `UseDirectInvoke()`. Runtime-DI apps are excluded on both paths, so `services.Configure<T>` still wins there, and `ServiceValidator` already skips runtime-DI apps, so the new `onlyBoundOptionsResolve: true` (NURU050 for snapshot/monitor) only affects source-gen apps. This matches the restatement and the task; no misread.

Checked and not a defect:
- Deduplicating registrations by `ParameterTypeName` is safe: `[ConfigurationKey]` is read from the options type `T` (`TryGetOptionsInfo` → `innerType.GetAttributes()`), so one type always has one section key and one validator.
- The mediator container already registers `IConfiguration` (`__mediatorConfiguration`) before the new registrations, so the factory's `GetRequiredService<IConfiguration>` resolves the running app's configuration including `--Section:Key` overrides.
- Generated local names (`__resolvedValue`, `____resolvedValidator`, `__optsValue`) are valid and scoped to the lambda or route method.
- generator-77 uses `Map<T>()` and the module-initializer pattern, is not in `CiTestExcludes`, so CI compiles it into the multi assembly; type names are `E591`-prefixed to avoid collisions.

No gates were run by this review (recorded-gates contract). The implementer's record covers build, the two changed test files, the sample smoke, and audit.

## Issues

### Issue 1 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs:324
- Description: The new `configurationExpression` parameter of `EmitOptionsBinding` is never passed by any caller (lines 87, 262, and `interceptor-emitter.services.cs:236` all take the default `"configuration"`). The mediator registration instead declares a local named `configuration`. The file's new Design region also says the expression may be "an IConfiguration resolved from a service provider", which is done by naming the local, not by this parameter. Verified with `grep -rn "configurationExpression\|EmitOptionsBinding(" source/timewarp-nuru-analyzers` (reason: confirm the parameter is dead).
- Suggestion: Drop the parameter, or pass the resolved expression from `EmitEndpointOptionsRegistrations` and drop the local.
- Status: open

### Issue 2 — Severity: nit
- File: kanban/in-progress/591-004-i-options-t-in-an-endpoint-handler-constructor-is-not-registered/task.md:56
- Description: `## Results` calls generator-77 a "Standalone runfile (`#if !JARIBU_MULTI`)". It is not in `CiTestExcludes` or the standalone list, so CI compiles it into the JARIBU_MULTI assembly; only its top-level `RunAllTests()` is gated. The multi-mode run is not recorded (the record defers it to CI, which is acceptable).
- Suggestion: Reword to "runs standalone and in the CI multi assembly".
- Status: open
