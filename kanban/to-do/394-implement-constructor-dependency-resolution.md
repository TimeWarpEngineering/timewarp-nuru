# Implement Constructor Dependency Resolution for Source-Gen DI

## Parent

Epic #391: Full DI Support - Source-Gen and Runtime Options

## Description

Enhance the source generator to analyze constructor dependencies of registered services and emit instantiation code in topologically sorted order. This allows source-gen DI to handle services with constructor dependencies while maintaining AOT compatibility.

## Current Problem

```csharp
services.AddSingleton<Settings>();
services.AddSingleton<IRepo, SqlRepo>();  // SqlRepo(Settings, ILogger<SqlRepo>)
```

**Currently generates (FAILS):**
```csharp
private static readonly Lazy<SqlRepo> __svc_SqlRepo =
    new(() => new SqlRepo());  // CS7036: Missing required arguments!
```

**Should generate:**
```csharp
private static readonly Lazy<Settings> __svc_Settings =
    new(() => new Settings());

private static readonly Lazy<SqlRepo> __svc_SqlRepo =
    new(() => new SqlRepo(
        __svc_Settings.Value,
        __loggerFactory.CreateLogger<SqlRepo>()));
```

## Design Decisions

| Item | Decision | Rationale |
|------|----------|-----------|
| NURU055 (circular deps) | Error | Circular dependencies cannot be resolved |
| NURU056 (singleton→transient) | Warning | MS DI allows this silently; warning for unintentional cases (can pragma if intentional) |
| Multiple constructors | Choose one with most resolvable params | Matches MS DI behavior |
| Optional parameters | Use default value if service not registered | Matches MS DI behavior |
| `IOptions<T>` with default | Use default (null) if not registered | Matches MS DI behavior |
| `IEnumerable<T>` dependencies | Tests only, no implementation | Deferred to future work |

## Implementation Steps

### Step 1: Create Dependency Graph Builder

**File:** `source/timewarp-nuru-analyzers/generators/dependency-graph-builder.cs` (new)

- [ ] Create `DependencyGraphBuilder` static class
- [ ] Implement `TopologicalSort()` using Kahn's algorithm
- [ ] Implement `DetectCircularDependencies()` using DFS cycle detection
- [ ] Return services in dependency order (dependencies first)

### Step 2: Add NURU055 and NURU056 Diagnostics

**File:** `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.service.cs`

- [ ] NURU055: Error - "Circular dependency detected: {0}. Services cannot depend on each other."
- [ ] NURU056: Warning - "Service '{0}' ({1} lifetime) depends on transient service '{2}'. Each resolution will get a new instance."

### Step 3: Enhance ServiceDefinition Model

**File:** `source/timewarp-nuru-analyzers/generators/models/service-definition.cs`

- [ ] Add `ConstructorParameter` record:
  ```csharp
  public sealed record ConstructorParameter(
    string ParameterName,
    string TypeName,
    bool HasDefaultValue,
    object? DefaultValue,
    bool IsBuiltIn  // ILogger<T>, IConfiguration, ITerminal, NuruApp, CancellationToken
  );
  ```
- [ ] Add `ImmutableArray<ConstructorParameter> ConstructorParameters` to `ServiceDefinition`

### Step 4: Enhance ServiceExtractor

**File:** `source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs`

- [ ] Support multiple constructors - find constructor with most resolvable parameters
- [ ] Extract optional parameter defaults (`HasDefaultValue`, `DefaultValue`)
- [ ] Identify built-in types (`ILogger<T>`, `IConfiguration`, `ITerminal`, `NuruApp`)
- [ ] Update `ExtractConstructorDependencies()` to return `ConstructorParameter` records

### Step 5: Update ServiceValidator

**File:** `source/timewarp-nuru-analyzers/validation/service-validator.cs`

- [ ] Add circular dependency detection (NURU055)
- [ ] Add lifetime mismatch warning (NURU056) - Singleton/Scoped depending on Transient

### Step 6: Update InterceptorEmitter

**File:** `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs`

- [ ] Modify `EmitServiceFields()` to use topological sort
- [ ] Emit services in dependency order

### Step 7: Update ServiceResolverEmitter

**File:** `source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs`

- [ ] Handle optional parameters with default values
- [ ] Resolve multi-level dependency chains
- [ ] Emit NURU056 warnings for lifetime mismatches

## Files Summary

| File | Action |
|------|--------|
| `generators/dependency-graph-builder.cs` | **Create** |
| `generators/models/service-definition.cs` | **Extend** - add `ConstructorParameter` record |
| `generators/extractors/service-extractor.cs` | **Enhance** - multi-constructor, optional params |
| `diagnostics/diagnostic-descriptors.service.cs` | **Extend** - NURU055, NURU056 |
| `validation/service-validator.cs` | **Extend** - circular deps, lifetime warnings |
| `generators/emitters/interceptor-emitter.cs` | **Modify** - topological sort |
| `generators/emitters/service-resolver-emitter.cs` | **Enhance** - optional params |

## Testing

### Test File: `tests/timewarp-nuru-core-tests/di/constructor-dependency-resolution-01.cs`

- [ ] Single dependency - `SqlRepo(Settings)` → emit Settings first
- [ ] Multiple dependencies - `SqlRepo(Settings, ILogger<SqlRepo>)` → resolve both
- [ ] Multi-level chain - A → B → C → emit C, B, A in order
- [ ] Diamond pattern - A → B, A → C, B → D, C → D → emit D first
- [ ] Circular dependency - A → B → A → NURU055 error
- [ ] Optional parameter with default - `Service(IDependency dep = null)` → use null if not registered
- [ ] Optional with non-null default - `Service(int timeout = 30)` → use 30
- [ ] Multiple constructors - Choose constructor with most resolvable params
- [ ] Singleton depends on Transient - NURU056 warning
- [ ] Mixed built-in and custom - `MyService(IRepo, ITerminal, ILogger)` → resolve correctly
- [ ] Transient with dependencies - Inline `new T(resolvedArgs...)` each time

### Test File: `tests/timewarp-nuru-core-tests/di/ienumerable-dependency-01.cs` (tests only, no implementation)

- [ ] Multiple implementations of same interface - `IEnumerable<IHandler>` → array of all registered
- [ ] Empty collection - No implementations registered → empty array
- [ ] Mixed lifetimes - `IEnumerable<IService>` with Singleton and Transient impls

## Generated Code Example

**Input:**
```csharp
services.AddSingleton<Settings>();
services.AddSingleton<IDbConnection, SqliteConnection>();  // SqliteConnection(Settings)
services.AddSingleton<IRepo, SqlRepo>();                   // SqlRepo(IDbConnection, ILogger<SqlRepo>)
services.AddSingleton<IService, MyService>();              // MyService(IRepo, Settings)
```

**Output (topologically sorted):**
```csharp
// Level 0: No dependencies
private static readonly Lazy<Settings> __svc_Settings =
    new(() => new Settings());

// Level 1: Depends on Level 0
private static readonly Lazy<SqliteConnection> __svc_SqliteConnection =
    new(() => new SqliteConnection(__svc_Settings.Value));

// Level 2: Depends on Level 1
private static readonly Lazy<SqlRepo> __svc_SqlRepo =
    new(() => new SqlRepo(
        __svc_SqliteConnection.Value,
        __loggerFactory.CreateLogger<SqlRepo>()));

// Level 3: Depends on Level 2 and Level 0
private static readonly Lazy<MyService> __svc_MyService =
    new(() => new MyService(
        __svc_SqlRepo.Value,
        __svc_Settings.Value));
```

## Notes

- This is Phase 3 of Epic #391
- Significantly expands source-gen DI capabilities
- Still requires all dependencies to be explicitly registered
- Extension method registrations still not visible (addressed in Phase 4)
- Maintains AOT compatibility
- MS DI compatibility: constructor selection and optional parameters match MS DI behavior
