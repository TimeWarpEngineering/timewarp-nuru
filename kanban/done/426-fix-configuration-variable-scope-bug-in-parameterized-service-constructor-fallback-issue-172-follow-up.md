# Replace runtime DI fallback with compile-time constructor resolution (issue 172 follow-up)

## Description

Task 425 fixed issue #172 (parameterized service constructors) by silently falling back to MS DI runtime (`GetRequiredService<T>()`) when a service has constructor dependencies. This is architecturally wrong.

**The core problem:** Nuru's source generator exists to eliminate MS DI for slim AOT apps. The task 425 implementation silently pulls in `ServiceCollection`, `BuildServiceProvider()`, and the entire MS DI container when a service has constructor params - without the user opting in. The user thinks they have a lean AOT app but they don't.

**The correct approach:** The source generator already knows the full dependency graph at compile time via `ConstructorDependencyTypes`. It should resolve dependencies statically and emit direct constructor calls. If it can't resolve a dependency, it should emit NURU051 as an error - not silently switch to MS DI.

## What task 425 got right (keep)

- `HasConstructorDependencies` and `ConstructorDependencyTypes` on `ServiceDefinition` - correct detection
- `ExtractConstructorDependencies()` in `service-extractor.cs:274-285` - correctly identifies constructor params
- NURU051 diagnostic for missing/unresolvable dependencies
- Test file structure in `generator-20-parameterized-service-constructor.cs`

## What task 425 got wrong (replace)

### 1. Silent MS DI fallback in `service-resolver-emitter.cs:93-99`

**Current (wrong):**
```csharp
if (service.HasConstructorDependencies)
{
  // Silently pulls in entire MS DI runtime
  sb.AppendLine($"...GetRequiredService<{typeName}>(GetServiceProvider{suffix}(app, configuration));");
}
```

**Should be:**
```csharp
if (service.HasConstructorDependencies)
{
  // Resolve each dependency to its compile-time source, then emit new T(dep1, dep2)
  // e.g., new FormatterService(configuration)
  // e.g., new ServiceB(new ServiceA())
  // e.g., new ServiceC(__serviceA.Value)  // singleton via Lazy<T>
}
```

### 2. Auto-fallback infrastructure in `generator-model.cs:55-62`

**Remove:**
- `NeedsRuntimeDIForConstructorDependencies` property
- The `|| NeedsRuntimeDIForConstructorDependencies` from `NeedsRuntimeDIInfrastructure`

These exist solely to trigger runtime DI infrastructure emission for the silent fallback.

### 3. Auto-fallback in `interceptor-emitter.cs:376-380`

**Remove** the `hasConstructorDependencies` check that triggers runtime DI infrastructure emission for apps that didn't opt in.

## Implementation

### Step 1: Replace emitter logic in `service-resolver-emitter.cs`

In the `HasConstructorDependencies` branch (line 93-99), instead of `GetRequiredService<T>()`:

1. For each dependency type in `service.ConstructorDependencyTypes`, resolve to its compile-time equivalent:
   - `IConfiguration` / `IConfigurationRoot` -> `configuration` (local variable from `AddConfiguration()`)
   - `ITerminal` -> `app.Terminal`
   - `NuruApp` -> `app`
   - Registered service (transient) -> `new ImplementationType()`
   - Registered service (singleton/scoped) -> `{lazyFieldName}.Value`
   - `ILogger<T>` -> logger factory resolution (existing pattern)
   - Unresolvable -> emit NURU051 error (already exists)

2. Emit: `new {ImplementationTypeName}(resolvedDep1, resolvedDep2, ...)`

This follows the same resolution logic already used by `ResolveServiceForCommand()` in `handler-invoker-emitter.cs:369-452` and the existing built-in checks in `ServiceResolverEmitter.EmitServiceResolution()`.

### Step 2: Remove auto-fallback infrastructure

1. **`generator-model.cs`** - Remove `NeedsRuntimeDIForConstructorDependencies`. Revert `NeedsRuntimeDIInfrastructure` to only check `UsesMicrosoftDependencyInjection`.

2. **`interceptor-emitter.cs:376-380`** - Remove the `hasConstructorDependencies` check in `EmitRuntimeDIInfrastructure()`. Only emit runtime DI for apps that explicitly call `UseMicrosoftDependencyInjection()`.

### Step 3: Handle recursive/transitive dependencies

When resolving a constructor dependency that is itself a registered service with constructor dependencies, recursively resolve. Example:

```csharp
// ServiceC(IServiceB b) where ServiceB(IServiceA a) where ServiceA() is parameterless
new ServiceC(new ServiceB(new ServiceA()))
```

For singletons with dependencies, the `Lazy<T>` field initializer needs to resolve dependencies too - or the resolution happens inline and the Lazy wraps the full expression.

### Step 4: Update tests

In `generator-20-parameterized-service-constructor.cs`:

1. **Existing test** - Update to verify generated code uses `new FormatterService(configuration)` NOT `GetRequiredService`
2. **Add: service depending on another registered service** - `ServiceB(IServiceA a)` where both are registered
3. **Add: mixed mode** - Parameterless + parameterized services in same app
4. **Add: no AddConfiguration scenario** - Service with non-config dependency, no `AddConfiguration()`
5. **Add: transitive dependencies** - `ServiceC(IServiceB b)` where `ServiceB(IServiceA a)`

### Step 5: Verify NURU051 still works

Ensure that when a constructor dependency is NOT resolvable at compile time (not registered, not built-in), NURU051 fires as an error telling the user to register the dependency or use `UseMicrosoftDependencyInjection()`.

## Checklist

- [ ] Replace `GetRequiredService<T>()` emit with `new T(resolvedDeps...)` in `service-resolver-emitter.cs`
- [ ] Build dependency resolution logic (map each dep type to its compile-time source)
- [ ] Handle transitive dependencies (deps that themselves have deps)
- [ ] Remove `NeedsRuntimeDIForConstructorDependencies` from `generator-model.cs`
- [ ] Remove auto-fallback `hasConstructorDependencies` check from `interceptor-emitter.cs`
- [ ] Update existing test to verify compile-time resolution (no MS DI)
- [ ] Add test: service depending on another registered service
- [ ] Add test: mixed parameterless + parameterized in same app
- [ ] Add test: no `AddConfiguration()` with parameterized service
- [ ] Add test: transitive dependency chain
- [ ] Verify NURU051 fires for unresolvable dependencies
- [ ] Run full CI test suite

## Additional Findings from Task 425 Review

### Constructor selection is order-dependent (MEDIUM severity, separate concern)

`service-extractor.cs:274-285` uses `FirstOrDefault()` to pick a constructor. If a class has both parameterless and parameterized constructors, behavior depends on declaration order. Consider preferring parameterless constructor first, or selecting the greediest satisfiable constructor. Defer to a separate task if needed.

### Configuration variable scope bug (now moot)

The original bug (referencing `configuration` when `AddConfiguration()` isn't called) becomes moot with this approach since we resolve each dependency individually rather than delegating to `GetServiceProvider`.

## Notes

- Follow-up to task 425 and issue #172
- Key reference: `handler-invoker-emitter.cs:369-452` (`ResolveServiceForCommand`) already has resolution logic for built-in types, registered services, loggers, etc. - reuse this pattern
- `service-resolver-emitter.cs` lines 42-76 already resolve built-in types (IConfiguration, ITerminal, NuruApp, IOptions) - extend this pattern for constructor deps
