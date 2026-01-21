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

## Requirements

### Model Enhancement
- [ ] Extend `ServiceDefinition` to include constructor parameters:
  ```csharp
  record ServiceDefinition(
      string ServiceTypeName,
      string ImplementationTypeName,
      ServiceLifetime Lifetime,
      ImmutableArray<ConstructorParameter> ConstructorParameters  // NEW
  );

  record ConstructorParameter(
      string ParameterName,
      string TypeName,
      bool IsBuiltIn  // ILogger<T>, IConfiguration, IOptions<T>, etc.
  );
  ```

### Service Extractor Enhancement
- [ ] After extracting service registration, analyze implementation constructor
- [ ] Extract parameter types and names
- [ ] Identify built-in services (ILogger<T>, IConfiguration, IOptions<T>, ITerminal, NuruApp)

### Dependency Graph Builder (New)
- [ ] Create `DependencyGraphBuilder` class
- [ ] Build adjacency list from service → dependencies
- [ ] Implement topological sort (Kahn's algorithm)
- [ ] Detect and report circular dependencies (NURU055 diagnostic)

### Emitter Enhancement
- [ ] Modify `InterceptorEmitter.EmitServiceFields()`:
  - Sort services topologically before emission
  - Emit constructor arguments based on dependencies
- [ ] Modify `ServiceResolverEmitter`:
  - Handle transient services with dependencies
  - Generate correct constructor calls

### Built-in Service Resolution
- [ ] `ILogger<T>` → `__loggerFactory.CreateLogger<T>()`
- [ ] `IConfiguration` → `configuration`
- [ ] `IOptions<T>` → Bind from configuration (existing logic)
- [ ] `ITerminal` → `app.Terminal`
- [ ] `NuruApp` → `app`

### Testing
- [ ] Service with single dependency
- [ ] Service with multiple dependencies
- [ ] Multi-level dependency chain (A → B → C)
- [ ] Diamond dependency pattern (A → B, A → C, B → D, C → D)
- [ ] Circular dependency detection
- [ ] Mix of built-in and custom services
- [ ] Transient services with dependencies

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
