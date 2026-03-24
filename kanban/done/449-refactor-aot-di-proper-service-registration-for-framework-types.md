# Refactor AOT DI: proper service registration for framework types

## Description

Refactor source-gen DI to properly handle framework types (`ITerminal`, `IConfiguration`, `NuruApp`, `CancellationToken`) as registered services rather than special properties. This fixes issue #208 and provides a clean, consistent DI architecture.

**GitHub Issue:** #208

## Checklist

- [x] Update `NuruApp.CreateBuilder()` to auto-register framework services:
  - `ITerminal` → from `app.Terminal`
  - `NuruApp` → the app instance itself
  - `IConfiguration` → when `.AddConfiguration()` is called
  - `CancellationToken` → per-invocation (scoped)

- [x] Add `FrameworkServiceTypes` constant in source generator with known framework service type names

- [x] Remove `IsBuiltIn` flag from `ConstructorParameter` record in `service-definition.cs`

- [x] Remove `IsBuiltInServiceType()` from `ServiceExtractor`

- [x] Remove `ResolveBuiltInType()` from `ServiceResolverEmitter` — treat all types uniformly through service resolution

- [x] Update `InterceptorEmitter.EmitServiceFields()`:
  - Emit static nullable fields WITHOUT initializers
  - Add `bool __servicesInitialized` flag
  - Emit `EnsureServicesInitialized(NuruApp app, IConfiguration configuration)` method

- [x] Update `EmitExecuteRouteAsync()`:
  - Call `EnsureServicesInitialized(app, configuration)` at start
  - Handle Scoped services as local variables
  - Handle Transient services inline

- [x] Update `ServiceValidator` to recognize framework services as always-available

- [x] Update tests in `generator-26-constructor-dependency-resolution.cs` for new pattern

- [x] Verify `tools/dev-cli` builds and runs correctly with shared endpoints

- [x] Update `NuruApp` class — keep `Terminal` property for backward compatibility but DI is primary path

## Notes

### Problem Statement

Current architecture has `ITerminal` as a special property on `NuruApp` (`app.Terminal`). The source generator emits `app.Terminal` in static field initializers, but `app` only exists in method scope (`ExecuteRouteAsync`). This causes CS8801 compilation errors.

### Root Cause

Field initializers run before any method executes. They cannot reference method parameters like `app`. The generator was emitting:

```csharp
// BROKEN: 'app' doesn't exist at static field initialization time
private static readonly Lazy<RepoCleanService> __svc = new(() => new RepoCleanService(app.Terminal));
```

### Solution Architecture

**Framework types become proper services:**
- `ITerminal`, `IConfiguration`, `NuruApp`, `CancellationToken` are auto-registered by the framework
- Source generator treats them like any other registered service
- No special `app.Terminal` resolution logic

**Initialization method pattern:**
```csharp
file static partial class GeneratedInterceptor
{
  private static ITerminal? __svc_ITerminal;
  private static NuruApp? __svc_NuruApp;
  private static RepoCleanService? __svc_RepoCleanService;
  private static bool __servicesInitialized = false;
  
  private static void EnsureServicesInitialized(NuruApp app)
  {
    if (__servicesInitialized)
      return;
    
    // Framework services first
    __svc_NuruApp = app;
    __svc_ITerminal = app.Terminal;
    
    // User services in dependency order (topological sort)
    __svc_RepoCleanService = new RepoCleanService(__svc_ITerminal);
    
    __servicesInitialized = true;
  }
}
```

**Lifetime handling:**
| Lifetime | Emission Strategy |
|----------|-------------------|
| Singleton | Static nullable field, initialized once in `EnsureServicesInitialized` |
| Scoped | Local variable in `ExecuteRouteAsync`, fresh each invocation |
| Transient | Created inline each time needed |

### Benefits

- Pure source-gen (no `IServiceProvider`, no reflection)
- AOT compatible
- Consistent DI resolution path for all dependencies
- Users can override framework service registrations if needed
- Cleaner architecture — services are services

### Related

- Issue #208: CS8801 in generated code
- Epic #391: Full DI support (source-gen and runtime)
- Task #394: Constructor dependency resolution (introduced the bug)
- Task #448: Shared dev-cli endpoints (exposed the bug)

## Results

### What was implemented
- Created `FrameworkServices` class as centralized registry for framework service types (ITerminal, IConfiguration, NuruApp, ILogger, CancellationToken)
- Removed `IsBuiltIn` flag from `ConstructorParameter` — all types now resolved uniformly
- Removed `IsBuiltInServiceType()` from `ServiceExtractor` and `ResolveBuiltInType()` from `ServiceResolverEmitter`
- Refactored `EmitServiceFields()` to emit nullable static fields without initializers
- Added `EnsureServicesInitialized(NuruApp app, IConfigurationRoot configuration)` method for deferred initialization
- Framework services initialized when `app` is available in `ExecuteRouteAsync`
- Used app identity check (`__fw_NuruApp == app`) for re-initialization in multi-test CI scenarios
- Fixed diamond dependency resolution via interface+implementation lookup

### Files changed
- `source/timewarp-nuru-analyzers/generators/models/framework-services.cs` (new)
- `source/timewarp-nuru-analyzers/generators/models/service-definition.cs`
- `source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/behavior-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/dependency-graph-builder.cs`
- `source/timewarp-nuru-analyzers/validation/service-validator.cs`

### Key decisions
- Framework services are resolved via static nullable fields initialized in `EnsureServicesInitialized()`, not field initializers
- Re-initialization triggered by app identity change, not a boolean flag (supports CI multi-mode tests)
- Diamond dependency resolution uses both ServiceTypeName and ImplementationTypeName lookup

### Test outcomes
- 1120 passed, 0 failed, 7 skipped
- `tools/dev-cli/dev.cs --help` works without CS8801
- Build: 0 warnings, 0 errors
