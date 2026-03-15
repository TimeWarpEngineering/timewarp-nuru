# Kanban Task 444 — TimeWarp.ServiceGen: Deep Code Review

## Executive Summary

Task 444 asks us to extract Nuru's compile-time DI infrastructure into a standalone reusable package `TimeWarp.ServiceGen`. Approximately 70% of the required functionality already exists in `timewarp-nuru-analyzers`. The remaining work is a combination of **extraction/decoupling** (remove Nuru-specific built-ins) and **gap-filling** (circular dependency detection, factory overloads, proper scoped lifetime, instance registrations). Twelve concrete issues were found during this review, ranging from a correctness bug (Scoped = Singleton) to missing features called out directly in task 444's requirements list.

## Scope

This is a deep code review of all files directly involved in Nuru's source-generated DI, analyzed in the context of task 444. The prior analysis (`2026-03-07T00-00-00_kanban-444-servicegen-analysis.md`) covered the extraction plan and gap table at a high level. This report goes further and provides specific, actionable findings with exact file/line references.

**Files reviewed in full:**
- `source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs` (374 lines)
- `source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs` (884 lines)
- `source/timewarp-nuru-analyzers/generators/models/service-definition.cs` (122 lines)
- `source/timewarp-nuru-analyzers/validation/service-validator.cs` (387 lines)
- `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.service.cs` (71 lines)
- `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` (lines 310–454 reviewed)
- `kanban/to-do/444-timewarp-servicegen-source-generated-aot-friendly-di-container.md`

---

## Findings

### Finding 1 — CORRECTNESS BUG: Scoped Lifetime Is Identical to Singleton

**File:** `interceptor-emitter.cs:330–355`
**Severity:** High — semantic bug, wrong behavior

Both `Singleton` and `Scoped` services are emitted as `private static readonly Lazy<T>`:

```csharp
ServiceDefinition[] cachedServices =
[
  .. allServices
    .Where(s => s.Lifetime is ServiceLifetime.Singleton or ServiceLifetime.Scoped)
    .DistinctBy(s => s.ImplementationTypeName)
];
// ...
sb.AppendLine(
  $"  private static readonly global::System.Lazy<{service.ImplementationTypeName}> {fieldName} = ...");
```

A `static readonly` field lives for the lifetime of the process. Scoped services are supposed to be created once *per scope* (e.g., per command invocation) and disposed at the end of that scope. The generated code creates them once per process — identical to singletons.

**Impact:** Any user who registers `AddScoped<T>()` expecting per-invocation isolation gets the singleton behavior instead. This silently violates the contract without an error or warning.

**Recommendation:** Either:
1. Emit `NURU05x` error: "Scoped lifetime is not supported in source-gen DI. Use `AddSingleton` or `AddTransient`, or opt into `UseMicrosoftDependencyInjection()`." — This is the correct position for the initial ServiceGen release.
2. Implement proper scope tracking: a thread-local or call-local `Dictionary<Type, object>` that lives for a single `RunAsync` invocation.

---

### Finding 2 — CORRECTNESS BUG: `IConfiguration` Always Marked Available, But Only Exists After `AddConfiguration()`

**File:** `service-validator.cs:124–127` (AddBuiltInServices)
**Also:** `service-resolver-emitter.cs:44–49` (EmitServiceResolution)
**Severity:** High — potential compile error in generated code

The validator always adds `IConfiguration` and `IConfigurationRoot` to the "always-available" built-in set:

```csharp
private static void AddBuiltInServices(HashSet<string> set)
{
    // IConfiguration
    set.Add("global::Microsoft.Extensions.Configuration.IConfiguration");
    set.Add("Microsoft.Extensions.Configuration.IConfiguration");
    set.Add("IConfiguration");
    // ...
}
```

But in `service-resolver-emitter.cs:44–49`, when `IConfiguration` is requested, the emitter outputs:

```csharp
sb.AppendLine($"{indent}{typeName} {varName} = configuration;");
```

The local variable `configuration` only exists in generated code when `AddConfiguration()` was called. If a user's service constructor depends on `IConfiguration` and they did *not* call `AddConfiguration()`, the validator will not fire NURU050 (because it thinks `IConfiguration` is always available), but the generated code will fail to compile because `configuration` is an undefined identifier.

**Recommendation:** Remove `IConfiguration`/`IConfigurationRoot` from `AddBuiltInServices()`. Instead, validate that `AddConfiguration()` was called when `IConfiguration` is found as a dependency. Add a new diagnostic (e.g., `NURU055`) for "IConfiguration requested but AddConfiguration() was not called."

---

### Finding 3 — MISSING FEATURE: No Circular Dependency Detection

**File:** `service-resolver-emitter.cs:314–373` (`ResolveConstructorArguments` and `ResolveDepExpression`)
**Severity:** High — task 444 explicitly requires this; infinite recursion at code-gen time

`ResolveConstructorArguments` calls `ResolveDepExpression` for each dependency. `ResolveDepExpression` recursively calls `ResolveConstructorArguments` for transient services with constructor dependencies:

```csharp
private static string ResolveDepExpression(string depType, ImmutableArray<ServiceDefinition> services)
{
    // ...
    if (depService.HasConstructorDependencies)
    {
        string innerArgs = ResolveConstructorArguments(depService, services);  // recursive!
        return $"new {depService.ImplementationTypeName}({innerArgs})";
    }
}
```

If A depends on B and B depends on A, this will stack overflow the source generator process.

**Recommendation:** Add a `HashSet<string> visitedTypes` parameter threaded through both methods. Before recursing into a dependency, check if it's already in the set. If so, emit diagnostic `SG056` (circular dependency detected) and return `default! /* CIRCULAR */` to allow analysis to continue.

---

### Finding 4 — MISSING FEATURE: Factory Delegate Registrations Rejected

**File:** `service-validator.cs:272–291` (ValidateFactoryRegistrations)
**Also:** `service-extractor.cs:256–269` (IsFactoryRegistration)
**Severity:** Medium — task 444 explicitly requires `AddSingleton<T>(Func<T> factory)`

Factory delegate registrations are detected and immediately rejected with NURU053:

```csharp
if (!service.IsFactoryRegistration) continue;

diagnostics.Add(Diagnostic.Create(
  DiagnosticDescriptors.FactoryDelegateNotSupported, ...));
```

Task 444 explicitly lists "Factory overloads: `AddSingleton<T>(Func<T> factory)`" as a requirement.

**Recommendation:** Implement factory emission. When `IsFactoryRegistration = true`:
- Capture the factory lambda body from the syntax node (it's accessible during extraction in `IsFactoryRegistration()`)
- Store the lambda text on `ServiceDefinition`
- In `EmitServiceFields`, emit: `private static readonly Lazy<T> _serviceField = new(() => <lambda-body>());`
- For transients: emit the factory call inline

This requires adding a `FactoryLambdaBody` property to `ServiceDefinition`.

---

### Finding 5 — MISSING FEATURE: Instance Registrations Not Modeled

**Files:** `service-extractor.cs`, `service-definition.cs`
**Severity:** Medium — task 444 explicitly requires `AddSingleton<T>(T instance)`

`IsFactoryRegistration` detects both `sp => new Foo()` lambdas AND could be confused with instance registrations. The `ServiceDefinition` model has no `InstanceExpression` or `IsInstanceRegistration` property. Passing a pre-existing object to `AddSingleton` is undetected.

**Recommendation:** Add to `ServiceDefinition`:
```csharp
bool IsInstanceRegistration = false,
string? InstanceExpression = null  // the syntax text of the instance
```

In the extractor, distinguish factory lambdas (`sp => expr`) from direct argument expressions (value types, `new`, variable references). For instance registrations, store the expression text and emit it as the `Lazy<T>` initializer body.

---

### Finding 6 — DESIGN ISSUE: Constructor Selection Takes First Public Constructor

**File:** `service-extractor.cs:277–278`
**Severity:** Medium — non-deterministic for multi-constructor types

```csharp
IMethodSymbol? constructor = implementationType.InstanceConstructors
    .FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsImplicitlyDeclared);
```

`FirstOrDefault` over constructors has undefined ordering — the "first" in Roslyn's list may not be deterministic across compilation sessions and certainly does not follow `[ActivatorUtilitiesConstructor]` semantics.

The test file `generator-24-mixed-constructors.cs:75–76` explicitly acknowledges this is unresolved:

```csharp
// NOTE: Current behavior takes the first constructor, which may not always be the desired one.
// TODO: Implement constructor selection logic (e.g., most parameters, attribute-based)
```

**Recommendation for ServiceGen:** Honor `[ActivatorUtilitiesConstructor]` attribute. This is the standard .NET approach (`Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructorAttribute`). When not present, select the constructor with the most parameters (greedy selection, same as `ActivatorUtilities`). Add a `SG057` diagnostic when multiple constructors exist and none is annotated.

---

### Finding 7 — CODE QUALITY: String-Based Type Matching Instead of Symbol Equality

**File:** `service-resolver-emitter.cs:125–143` (FindService)
**Also:** `service-validator.cs:320–324` (IsServiceRegistered)
**Severity:** Medium — brittle in edge cases

Service lookup uses string comparison of fully-qualified names (with a `global::` normalization step). This breaks for:
- Type aliases (`using MyService = Acme.MyService`)
- Same-name types in different assemblies (unlikely but possible)
- Partially-qualified names emitted by less common syntactic paths

The root cause is that `ServiceDefinition` stores `string` type names rather than `ITypeSymbol` references. This is understandable (symbols are not safe to store across generator incremental steps), but the normalization approach is fragile.

**Recommendation:** Validate that all type names stored in `ServiceDefinition` are always in fully-qualified `global::` form. Add a guard in the constructor/factory method. Use `SymbolDisplayFormat.FullyQualifiedFormat` consistently everywhere a type name is stored (this is already done in most paths, but verify the syntactic fallback paths in `ExtractServiceTypesSyntactically` are equally consistent).

---

### Finding 8 — DESIGN ISSUE: Hardcoded Whitelist of Extension Methods

**File:** `service-validator.cs:68–69`
**Severity:** Low-Medium — not extensible; will require code changes for new framework methods

```csharp
private static readonly HashSet<string> WhitelistedExtensionMethods =
    new(StringComparer.Ordinal) { "AddLogging", "AddHttpClient" };
```

Any other well-known extension method (`AddDbContext`, `AddMediatR`, `AddOpenAI`, etc.) will trigger NURU052 even in scenarios where the user has opted into `UseMicrosoftDependencyInjection()` or is aware the registration is opaque.

**Recommendation:** In the short term, suppress NURU052 whenever `UseMicrosoftDependencyInjection = true` (this is already done). In the long term, allow users to suppress with `#pragma warning disable NURU052` (already supported via standard Roslyn). Remove the hardcoded whitelist and just emit NURU052 for all non-analyzable extension methods — the user can suppress per-call or enable runtime DI. The whitelist creates a false sense of security: `AddHttpClient` is whitelisted but the HttpClient typed service is not included in `registeredServices` for dependency validation.

Wait — `service-validator.cs:37–47` shows `HttpClientConfiguration.ServiceTypeName` IS added to `registeredServices`. This partially mitigates the issue for `AddHttpClient`, but `AddLogging`'s registered types (`ILogger<T>`, `ILoggerFactory`) are not similarly tracked; they're handled by the special `IsLoggerType()` bypass instead.

---

### Finding 9 — ARCHITECTURE ISSUE: Nuru-Specific Built-Ins Deeply Embedded

**File:** `service-resolver-emitter.cs:44–66, 157–187` (IsConfigurationType, IsTerminalType, IsNuruAppType)
**Also:** `service-validator.cs:117–143` (AddBuiltInServices)
**Severity:** High — blocks the "standalone package" goal of task 444

These methods are baked into what will become the ServiceGen core:

```csharp
private static bool IsTerminalType(string typeName)
{
    return typeName is "global::TimeWarp.Terminal.ITerminal"
        or "TimeWarp.Terminal.ITerminal"
        or "ITerminal";
}

private static bool IsNuruAppType(string typeName)
{
    return typeName is "global::TimeWarp.Nuru.NuruApp" ...
}
```

ServiceGen is supposed to have "no Nuru or Mediator dependency." These methods would need to be removed, and instead Nuru would register `ITerminal` and `NuruApp` as built-in services via an extension point.

**Recommendation:** Design a `IBuiltInServiceResolver` interface (or simple `Func<string, string?>`) that ServiceGen calls before standard service lookup. Nuru provides the implementation that maps `ITerminal` → `app.Terminal` and `NuruApp` → `app`. ServiceGen itself knows nothing about Nuru types.

---

### Finding 10 — CODE QUALITY: Duplicated Resolution Logic Across Four Emitters

**Files:**
- `service-resolver-emitter.cs` (primary)
- `handler-invoker-emitter.cs` (for endpoint class handler injection)
- `behavior-emitter.cs` (for pipeline behaviors)
- `service-validator.cs` (for validation)

**Severity:** Medium — maintenance burden; bugs in one copy won't be fixed in others

The core "is this type registered / how do I resolve it" logic is replicated across at least four files. Each has slightly different handling (e.g., `behavior-emitter.cs` may not handle `IOptions<T>` the same way `service-resolver-emitter.cs` does).

**Recommendation:** In the ServiceGen extraction, unify all resolution logic into `ServiceResolverEmitter` as the single source of truth. All emitters call into it rather than implementing their own matching. This is part of the extraction work for task 444.

---

### Finding 11 — INCOMPLETE FEATURE: Runtime DI Bridge Missing IConfiguration Registration

**File:** `interceptor-emitter.cs:408–420` (EmitRuntimeDIInfrastructure)
**Severity:** Low-Medium — services that depend on `IConfiguration` will fail at runtime if `GetServiceProvider` is used

The `GetServiceProvider_N()` method registers `ITerminal` and `NuruApp` as singletons, but does NOT register `IConfiguration`. If a user-registered service depends on `IConfiguration` and runtime DI is active, MS DI will throw `InvalidOperationException: No service for type 'IConfiguration' has been registered` at runtime.

```csharp
sb.AppendLine("    // Register Nuru built-ins so they're available for user services");
sb.AppendLine("    ...AddSingleton<ITerminal>(services, app.Terminal);");
sb.AppendLine("    ...AddSingleton<NuruApp>(services, app);");
// IConfiguration is NOT registered here
```

**Recommendation:** After the configuration is built (in `ConfigurationEmitter`), register `IConfiguration` with the runtime DI container:
```csharp
ServiceCollectionServiceExtensions.AddSingleton<IConfiguration>(services, configuration);
```

---

### Finding 12 — KANBAN ACCURACY: Tasks 393 and 394 Appear Implemented

**File:** `kanban/to-do/393-add-di-diagnostics-for-unsupported-patterns.md` and `kanban/to-do/394-implement-constructor-dependency-resolution.md`
**Severity:** Low — bookkeeping issue only

Based on the code reviewed:
- Task 393 (DI diagnostics NURU050-054): **Fully implemented** in `diagnostic-descriptors.service.cs` and `service-validator.cs`
- Task 394 (Constructor dependency resolution): **Fully implemented** in `service-extractor.cs:274–285` and `service-resolver-emitter.cs:314–373`

Both tasks remain in `kanban/to-do/`. They should be moved to `done`.

---

## Summary Table

| # | Finding | Severity | Type | Blocks Task 444? |
|---|---------|----------|------|-----------------|
| 1 | Scoped = Singleton (silent bug) | High | Bug | Yes |
| 2 | IConfiguration always marked available | High | Bug | Yes |
| 3 | No circular dependency detection | High | Missing Feature | Yes |
| 4 | Factory delegates rejected, not implemented | Medium | Missing Feature | Yes |
| 5 | Instance registrations not modeled | Medium | Missing Feature | Yes |
| 6 | Constructor selection takes first, ignores `[ActivatorUtilitiesConstructor]` | Medium | Design Issue | Partial |
| 7 | String-based type matching (not symbol equality) | Medium | Code Quality | No |
| 8 | Hardcoded extension method whitelist | Low-Med | Design Issue | No |
| 9 | Nuru built-ins hard-coded in core resolution | High | Architecture | Yes |
| 10 | Duplicated resolution logic across 4 emitters | Medium | Code Quality | No |
| 11 | Runtime DI bridge missing IConfiguration | Low-Med | Bug | No |
| 12 | Tasks 393/394 done but in kanban/to-do | Low | Bookkeeping | No |

---

## Recommendations

### Priority 1 — Must Fix Before ServiceGen Extraction

These block correct separation of concerns or have silent wrong-behavior bugs:

1. **Finding 9: Decouple Nuru built-ins** — Design `IBuiltInServiceResolver` extension point. Without this, ServiceGen cannot be standalone.

2. **Finding 1: Scoped lifetime** — Emit a compile error (`SG05x`) when Scoped is used without a scope implementation. Do not silently treat it as Singleton.

3. **Finding 3: Circular dependency detection** — Add visited-set guard to `ResolveConstructorArguments`. Stack overflow in the source generator process is a P0 issue.

### Priority 2 — Task 444 Explicit Requirements

These are listed as checkboxes in the task definition and must be implemented:

4. **Finding 4: Factory delegate support** — Add `FactoryLambdaBody` to `ServiceDefinition`, emit factory in `Lazy<T>` initializer.

5. **Finding 5: Instance registration support** — Add `IsInstanceRegistration` + `InstanceExpression` to `ServiceDefinition`.

### Priority 3 — Correctness Fixes

6. **Finding 2: IConfiguration availability guard** — Remove from built-ins; add conditional diagnostic.

7. **Finding 11: IConfiguration in runtime DI bridge** — Register `IConfiguration` in `GetServiceProvider_N()`.

### Priority 4 — Code Quality / Design

8. **Finding 6: Constructor selection** — Honor `[ActivatorUtilitiesConstructor]`, fall back to most-params selection.

9. **Finding 10: Unify resolution logic** — Single `ServiceResolverEmitter` called by all emitters.

10. **Finding 7: Type name consistency** — Validate all stored type names are in `global::` form.

11. **Finding 8: Remove whitelist** — Emit NURU052 uniformly; users suppress via `#pragma`.

### Priority 5 — Bookkeeping

12. **Finding 12** — Move tasks 393 and 394 to `kanban/done/`.

---

## References

- Task 444: `kanban/to-do/444-timewarp-servicegen-source-generated-aot-friendly-di-container.md`
- Earlier gap analysis: `.agent/workspace/2026-03-07T00-00-00_kanban-444-servicegen-analysis.md`
- `service-resolver-emitter.cs`: `source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs`
- `service-extractor.cs`: `source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs`
- `service-definition.cs`: `source/timewarp-nuru-analyzers/generators/models/service-definition.cs`
- `service-validator.cs`: `source/timewarp-nuru-analyzers/validation/service-validator.cs`
- `interceptor-emitter.cs`: `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs`
- `diagnostic-descriptors.service.cs`: `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.service.cs`
- `generator-24-mixed-constructors.cs`: `tests/timewarp-nuru-tests/generator/generator-24-mixed-constructors.cs`
