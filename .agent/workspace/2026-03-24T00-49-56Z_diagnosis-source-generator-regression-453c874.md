# Diagnostic Report: Source Generator Regression from 453c874

**Date:** 2026-03-24T00:49:56Z
**Commit under analysis:** `453c874ac1dce3bb242ef61fc70ba2580225c1cc`
**Commit message:** `fix: refactor AOT DI to properly register framework types as services (#208)`
**Commit date:** 2026-03-24 02:34:35 +0700
**Parent commit:** `b9d7547b` (clean baseline)
**Subsequent commits:** `9f0c8405`, `346b44a3` (neither addresses this regression)

---

## 1. Symptom

All samples that lack registered services fail to compile after commit 453c874.
Pipeline samples that use behaviors additionally fail with a second error.

**Observed errors (reproduced locally):**

```
# fluent-hello-world-lambda (no services, no behaviors):
NuruGenerated.g.cs(110,5): error CS0103: The name 'EnsureServicesInitialized'
  does not exist in the current context

# fluent-pipeline-basic (no services, has behaviors):
NuruGenerated.g.cs(126,5): error CS0103: The name 'EnsureServicesInitialized'
  does not exist in the current context
NuruGenerated.g.cs(202,40): error CS1061: 'Lazy<LoggingBehavior>' does not
  contain a definition for 'HandleAsync'
NuruGenerated.g.cs(273,40): error CS1061: 'Lazy<LoggingBehavior>' does not
  contain a definition for 'HandleAsync'
```

These errors surface whenever `verify-samples` runs, which is a mandatory step
in the CI pipeline (`workflow-command.cs:105` pipeline:
`clean -> build -> verify-samples -> test`), triggered by `.github/workflows/workflow.yml`.

---

## 2. Root Cause A: `EnsureServicesInitialized` Called but Never Emitted

### Mechanism

`EmitMethodBody()` unconditionally emits a call to `EnsureServicesInitialized`:

```
interceptor-emitter.cs:890-891
    sb.AppendLine("    // Initialize services with app and configuration");
    sb.AppendLine("    EnsureServicesInitialized(app, configuration);");
```

However, the `EnsureServicesInitialized` method body is only emitted inside
`EmitServiceFields()`, which has an early return guard:

```
interceptor-emitter.cs:343
    if (cachedServices.Length == 0 && frameworkServiceTypes.Count == 0)
      return;
```

When an app has zero registered services (no `ConfigureServices` call), both
`cachedServices` and `frameworkServiceTypes` are empty. `EmitServiceFields`
returns without emitting the `EnsureServicesInitialized` method definition,
the `__fw_ITerminal` field, or the `__fw_NuruApp` field. The generated class
then calls a method that does not exist.

### Call chain producing the defect

1. `InterceptorEmitter.Emit()` calls `EmitServiceFields(sb, sourceGenServices)` at line 47
2. `sourceGenServices` is derived from `model.Apps.Where(a => !a.UseMicrosoftDependencyInjection).SelectMany(a => a.Services)` (lines 37-40)
3. For a no-service app, this sequence is empty
4. `EmitServiceFields` materializes it, finds zero cached services and zero framework types, returns at line 343
5. Later, `EmitAppInterceptorMethod` -> `EmitExecuteRouteAsyncMethod` -> `EmitMethodBody` unconditionally emits the call at line 891
6. Result: CS0103

### Pre-453c874 state

Before this commit, services used `Lazy<T>` fields emitted directly in
`EmitServiceFields`. There was no separate `EnsureServicesInitialized` method,
and `EmitMethodBody` did not call any initialization method. The call site was
introduced in 453c874 without a corresponding guard or fallback emission.

---

## 3. Root Cause B: Behavior Emitter Calls `HandleAsync` on `Lazy<T>` Instead of `.Value`

### Mechanism

Commit 453c874 changed service fields from `Lazy<T>` to plain nullable fields,
but behavior fields were intentionally kept as `Lazy<T>`. The commit
incorrectly removed `.Value` from behavior field access in three places:

**behavior-emitter.cs diff from 453c874:**
```diff
-      sb.AppendLine($"{ind}await {fieldName}.Value.HandleAsync(__typedContext_{behaviorIndex}, async () =>");
+      sb.AppendLine($"{ind}await {fieldName}.HandleAsync(__typedContext_{behaviorIndex}, async () =>");

-      sb.AppendLine($"{ind}await {fieldName}.Value.HandleAsync(__behaviorContext, async () =>");
+      sb.AppendLine($"{ind}await {fieldName}.HandleAsync(__behaviorContext, async () =>");

-      return $"{fieldName}.Value";
+      return fieldName;
```

Meanwhile, `EmitBehaviorFields` still emits `Lazy<T>` fields:

```
behavior-emitter.cs:41-42
  sb.AppendLine(
    $"  private static readonly global::System.Lazy<{behavior.FullTypeName}> {fieldName} =
       new(() => new {behavior.FullTypeName}({constructorArgs}));");
```

The generated code declares `__behavior_LoggingBehavior` as `Lazy<LoggingBehavior>`
but calls `.HandleAsync()` directly on the `Lazy<T>` wrapper, which has no such method.

### Evidence from generated output

```
# Generated field (behavior-emitter.cs:41-42):
  private static readonly Lazy<LoggingBehavior> __behavior_LoggingBehavior =
    new(() => new LoggingBehavior());

# Generated call site (behavior-emitter.cs lines 305/310 post-453c874):
  await __behavior_LoggingBehavior.HandleAsync(__behaviorContext, async () =>
```

`Lazy<LoggingBehavior>` has no `HandleAsync` method. Must be
`__behavior_LoggingBehavior.Value.HandleAsync(...)`.

---

## 4. Evidence Chain with File/Line References

| Evidence | File | Line(s) | Commit |
|----------|------|---------|--------|
| `EnsureServicesInitialized` call emitted unconditionally | `interceptor-emitter.cs` | 890-891 | `453c874` |
| `EnsureServicesInitialized` method emitted inside conditional block | `interceptor-emitter.cs` | 325-378 (guard at 343) | `453c874` |
| `EmitServiceFields` early return when no services | `interceptor-emitter.cs` | 343 | `453c874` |
| Service fields changed from `Lazy<T>` to nullable | `interceptor-emitter.cs` | 350-377 | `453c874` |
| `.Value` removed from behavior HandleAsync calls | `behavior-emitter.cs` | 305, 310 | `453c874` |
| `.Value` removed from behavior service resolution | `behavior-emitter.cs` | 400-401 | `453c874` |
| Behavior fields still emitted as `Lazy<T>` | `behavior-emitter.cs` | 41-42 | Unchanged |
| `.Value` removed from handler-invoker service resolution | `handler-invoker-emitter.cs` | 440-441 | `453c874` |
| `.Value` removed from service-resolver-emitter | `service-resolver-emitter.cs` | 94-97 | `453c874` |
| New `FrameworkServices` class introduced | `models/framework-services.cs` | 1-120 | `453c874` (new file) |
| CI workflow calls `verify-samples` | `workflow-command.cs` | 105, 128-129 | Pre-existing |
| CI workflow.yml triggers on `samples/**` changes | `.github/workflows/workflow.yml` | 8-9, 20-21 | Pre-existing |

### Compilation errors reproduced

```
# Reproduced on dev branch (HEAD = 346b44a3) at 2026-03-24T00:49Z

$ ganda runfile cache --clear
$ dotnet run samples/fluent/01-hello-world/fluent-hello-world-lambda.cs -- --version
  -> CS0103: EnsureServicesInitialized does not exist

$ dotnet run samples/fluent/05-pipeline/fluent-pipeline-basic.cs -- test
  -> CS0103: EnsureServicesInitialized does not exist
  -> CS1061: 'Lazy<LoggingBehavior>' does not contain 'HandleAsync'
```

### Generated file evidence

- `artifacts/generated/fluent-hello-world-lambda/.../NuruGenerated.g.cs`:
  - Line 110 calls `EnsureServicesInitialized(app, configuration);`
  - No `__fw_*` fields emitted
  - No `EnsureServicesInitialized` method defined
- `artifacts/generated/fluent-pipeline-basic/.../NuruGenerated.g.cs`:
  - Line 67: `private static readonly Lazy<LoggingBehavior> __behavior_LoggingBehavior = ...`
  - Line 202: `await __behavior_LoggingBehavior.HandleAsync(...)` (missing `.Value`)

---

## 5. Affected Scope

### Samples affected by Root Cause A (every sample without registered services)

All fluent samples, most endpoint samples, and all hybrid samples that do not
call `ConfigureServices` with at least one service registration. This includes
the majority of the sample library.

Specific categories confirmed broken:
- `samples/fluent/01-hello-world/` (no services)
- `samples/fluent/02-calculator/` (no services)
- `samples/fluent/03-syntax/` (no services)
- `samples/fluent/04-async/` (no services)
- `samples/endpoints/02-calculator/` (has services but via endpoint DI)
- `samples/endpoints/03-syntax/` (no services)
- `samples/endpoints/04-async/` (no services)

### Samples affected by Root Cause B (any sample with pipeline behaviors)

- `samples/fluent/05-pipeline/fluent-pipeline-basic.cs`
- `samples/fluent/05-pipeline/fluent-pipeline-retry.cs`
- `samples/fluent/05-pipeline/fluent-pipeline-exception.cs`
- `samples/fluent/05-pipeline/fluent-pipeline-filtered-auth.cs`
- `samples/fluent/05-pipeline/fluent-pipeline-combined.cs`
- `samples/hybrid/02-unified-pipeline/`
- Any user app registering behaviors via `AddBehavior<T>()`

### Workflows affected

- `verify-samples` command (invoked by `workflow` command)
- CI/CD pipeline (`workflow.yml` -> `dev workflow` -> `verify-samples`)
- Any PR or push to master touching `source/**` or `samples/**`

---

## 6. Reproduction Steps

```bash
# 1. Checkout the dev branch at or after 453c874
git checkout dev

# 2. Clear runfile cache (stale cache masks the issue)
ganda runfile cache --clear

# 3. Reproduce Root Cause A (CS0103: EnsureServicesInitialized)
dotnet run samples/fluent/01-hello-world/fluent-hello-world-lambda.cs -- --version
# Expected: version string
# Actual:   CS0103 compile error

# 4. Reproduce Root Cause B (CS1061: Lazy<T> has no HandleAsync)
dotnet run samples/fluent/05-pipeline/fluent-pipeline-basic.cs -- test
# Expected: pipeline output
# Actual:   CS0103 + CS1061 compile errors

# 5. Reproduce full CI failure
dotnet run tools/dev-cli/dev.cs -- verify-samples
# Expected: all samples pass
# Actual:   multiple samples FAILED

# 6. Inspect generated code to see the defect
cat artifacts/generated/fluent-hello-world-lambda/TimeWarp.Nuru.Analyzers/\
  TimeWarp.Nuru.Generators.NuruGenerator/NuruGenerated.g.cs
# Look for: EnsureServicesInitialized call with no method definition

cat artifacts/generated/fluent-pipeline-basic/TimeWarp.Nuru.Analyzers/\
  TimeWarp.Nuru.Generators.NuruGenerator/NuruGenerated.g.cs
# Look for: Lazy<T> field + .HandleAsync without .Value
```

---

## 7. Contributing Factors and Related History

### Factor 1: Refactor scope exceeded mechanical transformation

The commit message says "refactor AOT DI to properly register framework types
as services." The intent was to move from `Lazy<T>` service fields to
`EnsureServicesInitialized`-based deferred init. The refactor correctly
converted service fields but simultaneously touched behavior fields (which
remained `Lazy<T>`), removing `.Value` across all field access patterns without
distinguishing service fields from behavior fields.

### Factor 2: Conditional emission vs unconditional call-site

The `EnsureServicesInitialized` method and the framework fields (`__fw_*`) are
emitted inside `EmitServiceFields`, which is gated on having at least one
service or framework dependency. The call to `EnsureServicesInitialized` in
`EmitMethodBody` was added without a corresponding guard, creating a structural
mismatch between definition and usage.

### Factor 3: No test coverage for zero-service apps

The CI test suite (`tests/ci-tests/`) appears to focus on apps with routes,
services, and endpoints. Simple zero-service fluent samples are only covered by
the `verify-samples` build check, which runs later in the pipeline after unit
tests. This means the regression passes unit tests but fails at the sample
verification stage.

### Factor 4: Stale runfile cache masks the regression

Per project convention, `ganda runfile cache --clear` must be run before testing
when source generator code changes. Without clearing the cache, previously
compiled samples continue to work with old generated code, delaying discovery of
the regression.

### Factor 5: Follow-up commits did not address the issue

Two commits followed 453c874:
- `9f0c8405`: "Complete task 449: Refactor AOT DI for proper framework service registration" (task bookkeeping)
- `346b44a3`: "fix: replace non-intercepted App.RunAsync calls with direct handler invocation" (different issue)

Neither commit addresses the `EnsureServicesInitialized` or `Lazy<T>.HandleAsync`
regressions.

### Factor 6: Single commit touched 11 files across 4 emitter layers

The commit modified `interceptor-emitter.cs`, `behavior-emitter.cs`,
`handler-invoker-emitter.cs`, `service-resolver-emitter.cs`, plus models and
validators. The breadth of changes across interconnected emitters increased the
risk of inconsistent transformations.

---

## Summary

Commit `453c874` introduced two independent compilation-breaking regressions in
the source generator:

1. **`EnsureServicesInitialized` call emitted unconditionally** while its method
   definition is conditional on having services. All apps without registered
   services produce CS0103.

2. **`.Value` removed from `Lazy<T>` behavior fields** despite behaviors still
   being declared as `Lazy<T>`. All apps with pipeline behaviors produce CS1061.

Both defects break the `verify-samples` CI step and would block any PR or push
to master that touches source or samples.
