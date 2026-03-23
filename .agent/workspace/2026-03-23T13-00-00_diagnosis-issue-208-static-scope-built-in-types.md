# Diagnosis: Issue #208 — Source-gen DI emits `app.Terminal` in static field initializers

## Symptom

When a service registered via `ConfigureServices()` has a constructor dependency on `ITerminal`, the source generator emits `app.Terminal` inside a `static readonly Lazy<T>` field initializer. `app` is a method parameter on `ExecuteRouteAsync`, not available at static scope, causing CS8801 compilation error.

## Root Cause

**Two independent emission sites call the same resolver functions, but one site is static scope (no `app` available) and the other is method scope (where `app` is a parameter). The resolver functions always return `app.Terminal` for ITerminal regardless of emission context.**

### The Two Emission Sites

**Site 1: Static field initializers** — `InterceptorEmitter.EmitServiceFields()` (line 325 of `interceptor-emitter.cs`)
```
file static partial class GeneratedInterceptor
{
  // STATIC SCOPE — no "app" variable exists here
  private static readonly Lazy<RepoCleanService> __svc_... =
    new(() => new RepoCleanService(app.Terminal));  // ← CS8801
}
```

**Site 2: Method body** — `ServiceResolverEmitter.Emit()` called from `RouteMatcherEmitter` inside `ExecuteRouteAsync`
```
private static async Task<int> ExecuteRouteAsync(NuruApp app, string[] args)
{
  // METHOD SCOPE — "app" is a parameter here
  ITerminal terminal = app.Terminal;  // ← This works fine
}
```

### The Call Chain for Site 1 (the broken one)

1. `InterceptorEmitter.EmitServiceFields()` (line 325) iterates Singleton/Scoped services
2. Line 353: checks `service.HasConstructorDependencies`
3. Line 355: calls `ServiceResolverEmitter.ResolveConstructorArguments(service, allServices)`
4. `ResolveConstructorArguments()` (line 316) iterates `service.ConstructorParameters`
5. For each param, calls `ResolveParameterExpression()` (line 336)
6. Line 339: checks `param.IsBuiltIn` — **true** for ITerminal
7. Line 341: calls `ResolveBuiltInType(param.TypeName)`
8. `ResolveBuiltInType()` (line 379):
   - Line 386: `IsTerminalType(typeName)` returns **true**
   - Line 387: returns **`"app.Terminal"`**
9. Back in `EmitServiceFields()` line 356-357, emits:
   ```csharp
   private static readonly Lazy<RepoCleanService> __svc_... =
     new(() => new RepoCleanService(app.Terminal));
   ```

### Why It's Wrong

`ResolveBuiltInType()`, `ResolveDepExpression()`, and `ResolveParameterExpression()` are **context-unaware**. They always return the same expressions regardless of whether they're being called for:
- A static field initializer (no `app`, no `configuration` in scope)
- A method body inside `ExecuteRouteAsync` (where `app` and `configuration` are parameters)

These functions were originally written for Site 2 only (method body emission in `Emit()`). Phase 3 added Site 1 (`EmitServiceFields`) and reused `ResolveConstructorArguments()` without accounting for the scope difference.

## Affected Built-in Types

From `ServiceResolverEmitter.ResolveBuiltInType()` (line 379) and `ResolveDepExpression()` (line 415):

| Built-in Type | Expression Returned | Available in Static Scope? |
|---|---|---|
| `ITerminal` | `app.Terminal` | **NO** — `app` is a method parameter |
| `IConfiguration` / `IConfigurationRoot` | `configuration` | **NO** — `configuration` is a local variable in `ExecuteRouteAsync` |
| `NuruApp` | `app` | **NO** — `app` is a method parameter |
| `ILogger<T>` | `NullLoggerFactory.Instance.CreateLogger<T>()` | YES — static factory call |
| `IOptions<T>` | `default!` (fallback) | N/A — handled separately |
| `CancellationToken` | Not in `ResolveBuiltInType` | N/A — detected as built-in by `IsBuiltInServiceType` but not in resolver |

So `ITerminal`, `IConfiguration`, and `NuruApp` are all broken in static field initializers. `ILogger<T>` works because it uses a static factory call that doesn't depend on instance variables.

## Evidence Chain

### File: `interceptor-emitter.cs`
- **Line 325-367**: `EmitServiceFields()` — emits `static readonly Lazy<T>` fields
- **Line 353**: `if (service.HasConstructorDependencies)` — triggers constructor arg resolution
- **Line 355**: `ServiceResolverEmitter.ResolveConstructorArguments(service, allServices)` — calls into the context-unaware resolver
- **Line 356-357**: emits the resolved args into a static field initializer lambda

### File: `service-resolver-emitter.cs`
- **Line 316-330**: `ResolveConstructorArguments()` — entry point, iterates params
- **Line 336-374**: `ResolveParameterExpression()` — dispatches to `ResolveBuiltInType` for built-in params
- **Line 379-409**: `ResolveBuiltInType()` — **the core defect** — returns `"app.Terminal"`, `"configuration"`, `"app"` unconditionally
- **Line 415-462**: `ResolveDepExpression()` — same defect for the legacy code path

### File: `service-extractor.cs`
- **Line 379-394**: `IsBuiltInServiceType()` — classifies `ITerminal`, `IConfiguration`, `NuruApp`, `ILogger`, `IOptions`, `CancellationToken` as built-in
- **Line 298-330**: `ExtractConstructorParameters()` — sets `IsBuiltIn = true` for these types
- This means services with these constructor params will hit the `ResolveBuiltInType` path

## Affected Scope

Any Singleton or Scoped service registered via `ConfigureServices()` whose implementation constructor takes:
- `ITerminal` (resolves to `app.Terminal`)
- `IConfiguration` or `IConfigurationRoot` (resolves to `configuration`)
- `NuruApp` (resolves to `app`)

Transient services are NOT affected because they're instantiated inline in method bodies (Site 2), where `app` and `configuration` are in scope.

## Reproduction Steps

1. Register a Singleton service whose constructor takes `ITerminal`:
   ```csharp
   .ConfigureServices(services =>
   {
     services.AddSingleton<IMyService, MyService>();
   })
   ```
   Where `MyService` has:
   ```csharp
   public MyService(ITerminal terminal) { }
   ```

2. Build the project. The source generator emits:
   ```csharp
   private static readonly Lazy<MyService> __svc_MyService =
     new(() => new MyService(app.Terminal));  // CS8801
   ```

3. Compilation fails with CS8801.

## Contributing Factors

1. **Phase 3 added `EmitServiceFields` as a new call site** for `ResolveConstructorArguments` without recognizing the scope difference
2. **No "emission context" parameter** exists on the resolver functions — they have no way to know whether they're emitting for static scope vs method scope
3. **The existing test suite** (generator-26) uses test services with custom dependencies only — no test registers a Singleton with an `ITerminal` constructor dependency, so this gap was never caught
4. **Built-in types that resolve to static expressions (ILogger)** work fine, masking the pattern — only instance-dependent built-ins (ITerminal, IConfiguration, NuruApp) fail

## Related History

- **Commit `91e92888`**: "feat: implement constructor dependency resolution for source-gen DI" — introduced the bug
- **Phase 3 of Epic #391** (Task #394) — the feature that added `EmitServiceFields` calling `ResolveConstructorArguments`
- **Runtime DI path** (`EmitRuntimeDIInfrastructure`, line 376) handles this correctly — it passes `app` into `GetServiceProvider(app)` and registers built-ins at runtime: `AddSingleton<ITerminal>(services, app.Terminal)`

## Key Insight

The fundamental design issue is that **Singleton/Scoped services with built-in dependencies cannot be static fields** because built-in services (`app.Terminal`, `configuration`) are only available at runtime when `ExecuteRouteAsync` is called with an `app` parameter. Either:

1. These services need to be initialized lazily on first use inside `ExecuteRouteAsync` (not as static fields), OR
2. The static `Lazy<T>` lambda needs to capture the `app` parameter (deferred initialization), OR
3. Services with built-in constructor deps need a different emission strategy than pure-custom-dep services
