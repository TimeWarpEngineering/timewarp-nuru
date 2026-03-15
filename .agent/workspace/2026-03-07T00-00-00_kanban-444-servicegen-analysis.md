# Kanban Task 444: TimeWarp.ServiceGen Analysis

## Executive Summary

Task 444 proposes extracting Nuru's compile-time service resolution (`ServiceResolverEmitter`) into a standalone, reusable source-generated DI container called `TimeWarp.ServiceGen`. This is a foundational task that enables both `TimeWarp.Mediator` (task 443) and a cleaner `TimeWarp.Nuru` architecture. The current implementation in Nuru already provides the core functionality—this task is about **generalization and extraction**, not building from scratch.

## Scope

This analysis covers:
- Current `ServiceResolverEmitter` implementation and capabilities
- `ServiceDefinition` model and `ServiceExtractor` logic
- Existing diagnostics (NURU050-054) for validation
- Runtime DI bridge (`UseMicrosoftDependencyInjection()`)
- Related tasks (393, 394, 443) and their dependencies
- Gap analysis between current Nuru code and ServiceGen requirements

## Methodology

- Read all source files in `/source/timewarp-nuru-analyzers/generators/`
- Analyzed `ServiceResolverEmitter.cs`, `ServiceExtractor.cs`, `ServiceDefinition.cs`
- Reviewed `InterceptorEmitter.cs` for service field generation
- Examined test files for current DI behavior
- Reviewed related kanban tasks (393, 394, 443)
- Read existing analysis reports on DI approaches

---

## Findings

### 1. Current Architecture Overview

Nuru's source-generated DI is implemented across several files:

```
source/timewarp-nuru-analyzers/
├── generators/
│   ├── emitters/
│   │   ├── service-resolver-emitter.cs      # Core: emits service resolution code
│   │   ├── handler-invoker-emitter.cs       # Uses ServiceResolverEmitter for handlers
│   │   └── interceptor-emitter.cs           # Emits Lazy<T> fields for services
│   ├── extractors/
│   │   └── service-extractor.cs             # Extracts registrations from ConfigureServices
│   └── models/
│       └── service-definition.cs            # ServiceDefinition record
├── validation/
│   └── service-validator.cs                  # NURU050-054 diagnostics
└── diagnostics/
    └── diagnostic-descriptors.service.cs     # Diagnostic definitions
```

### 2. ServiceResolverEmitter Capabilities

**File:** `service-resolver-emitter.cs` (374 lines)

The current implementation already provides most of what ServiceGen needs:

| Feature | Status | Implementation Location |
|---------|--------|------------------------|
| Constructor parameter analysis | ✅ Implemented | `ResolveConstructorArguments()` (lines 314-320) |
| `new T(dep1, dep2...)` emission | ✅ Implemented | Lines 98-104 |
| `Lazy<T>` for Singleton/Scoped | ✅ Implemented | Lines 91-96 |
| Built-in service recognition | ✅ Implemented | `IsConfigurationType()`, `IsTerminalType()`, `IsNuruAppType()` |
| `IOptions<T>` binding | ✅ Implemented | `EmitOptionsBinding()` (lines 232-260) |
| `ILogger<T>` resolution | ✅ Implemented | Lines 340-347 |
| Transitive dependency resolution | ✅ Implemented | `ResolveDepExpression()` (lines 326-373) |
| Runtime DI fallback | ✅ Implemented | Lines 79-84 |

**Key Code Pattern:**
```csharp
// Singleton/Scoped: use Lazy<T> field
if (service.Lifetime is ServiceLifetime.Singleton or ServiceLifetime.Scoped)
{
  string fieldName = InterceptorEmitter.GetServiceFieldName(service.ImplementationTypeName);
  sb.AppendLine($"{indent}{typeName} {varName} = {fieldName}.Value;");
}
// Transient with constructor deps: inline new T(resolvedDeps...)
else if (service.HasConstructorDependencies)
{
  string args = ResolveConstructorArguments(service, services);
  sb.AppendLine($"{indent}{typeName} {varName} = new {service.ImplementationTypeName}({args});");
}
```

### 3. ServiceDefinition Model

**File:** `service-definition.cs` (122 lines)

The model is well-designed and already includes:

```csharp
public sealed record ServiceDefinition(
  string ServiceTypeName,
  string ImplementationTypeName,
  ServiceLifetime Lifetime,
  ImmutableArray<string> ConstructorDependencyTypes = default,
  bool IsFactoryRegistration = false,
  bool IsInternalType = false,
  Location? RegistrationLocation = null)
```

**What's Already There:**
- Service/Implementation type names (fully qualified)
- Lifetime (Singleton, Scoped, Transient)
- Constructor dependency types
- Factory registration detection
- Internal type detection
- Source location for diagnostics

### 4. ServiceExtractor Capabilities

**File:** `service-extractor.cs` (884 lines)

The extractor handles multiple registration patterns:

| Pattern | Support | Code Location |
|---------|---------|---------------|
| `AddSingleton<T>()` | ✅ Full | Lines 322-343 |
| `AddSingleton<TInterface, TImpl>()` | ✅ Full | Lines 322-343 |
| `AddSingleton(typeof(IFoo), typeof(Foo))` | ✅ Full | Lines 346-356 |
| Lambda body: `s => { s.AddSingleton<...>(); }` | ✅ Full | Lines 48-74 |
| Method group: `ConfigureServices(MyMethod)` | ✅ Full | Lines 80-120 |
| Factory delegates: `AddSingleton(sp => new Foo())` | ⚠️ Detected only | Lines 220-221, 256-269 |
| Extension methods: `AddLogging()`, `AddHttpClient()` | ⚠️ Whitelisted | Lines 68-69 |

**Limitation:** Factory delegates are detected but not analyzed—the generator emits NURU053 error requiring `UseMicrosoftDependencyInjection()`.

### 5. InterceptorEmitter Service Fields

**File:** `interceptor-emitter.cs` (lines 324-359)

Generates static `Lazy<T>` fields for Singleton/Scoped services:

```csharp
private static void EmitServiceFields(StringBuilder sb, IEnumerable<ServiceDefinition> services)
{
  foreach (ServiceDefinition service in cachedServices)
  {
    string fieldName = GetServiceFieldName(service.ImplementationTypeName);
    if (service.HasConstructorDependencies)
    {
      string args = ServiceResolverEmitter.ResolveConstructorArguments(service, allServices);
      sb.AppendLine($"private static readonly Lazy<{service.ImplementationTypeName}> {fieldName} = new(() => new {service.ImplementationTypeName}({args}));");
    }
    else
    {
      sb.AppendLine($"private static readonly Lazy<{service.ImplementationTypeName}> {fieldName} = new(() => new {service.ImplementationTypeName}());");
    }
  }
}
```

### 6. Runtime DI Bridge

**File:** `interceptor-emitter.cs` (lines 366-454)

Nuru already has a complete Microsoft DI bridge:

```csharp
private static void EmitRuntimeDIInfrastructure(StringBuilder sb, ImmutableArray<AppModel> apps)
{
  // Emits per-app:
  // - __ConfigureServices_N() method with user's lambda body
  // - GetServiceProvider_N() method that builds ServiceCollection
  // - Registers Nuru built-ins (ITerminal, NuruApp)
  // - Calls user's ConfigureServices delegate
}
```

**Usage in generated code:**
```csharp
// When useRuntimeDI = true
global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
  .GetRequiredService<TService>(GetServiceProvider_N(app));
```

### 7. Existing Diagnostics

**File:** `diagnostic-descriptors.service.cs` (71 lines)

| Code | Severity | Purpose |
|------|----------|---------|
| NURU050 | Error | Handler requires unregistered service |
| NURU051 | Error | Service has constructor dependencies (missing deps) |
| NURU052 | Warning | Extension method registration not analyzable |
| NURU053 | Error | Factory delegate registration not supported |
| NURU054 | Error | Internal type not accessible |

**Validation Skip:** All diagnostics are skipped when `UseMicrosoftDependencyInjection = true`.

### 8. Test Coverage

**File:** `generator-20-parameterized-service-constructor.cs` (323 lines)

Tests verify:
- Service with `IConfiguration` dependency
- Service with registered service dependency
- Mixed parameterless + parameterized services
- Transitive dependencies (A → B → C)
- Singleton with constructor deps in endpoints

**File:** `generator-15-runtime-di.cs` (508 lines)

Tests verify:
- Runtime DI with constructor dependencies
- Singleton/Transient lifetime behavior
- Method group references
- Transitive `ILogger<T>` dependencies

---

## Gap Analysis: Current Nuru vs. ServiceGen Requirements

### What ServiceGen Needs (from Task 444)

| Requirement | Current Status | Gap |
|-------------|----------------|-----|
| `AddSingleton<T>()` | ✅ Implemented | None |
| `AddSingleton<TInterface, TImpl>()` | ✅ Implemented | None |
| `AddTransient<T>()` | ✅ Implemented | None |
| `AddScoped<T>()` | ✅ Implemented | None |
| Factory overloads: `AddSingleton<T>(Func<T>)` | ❌ Not supported | Need factory emission |
| Instance overloads: `AddSingleton<T>(T instance)` | ❌ Not supported | Need instance storage |
| Circular dependency detection | ❌ Not implemented | Need topological sort |
| Missing registration detection | ✅ NURU050/051 | None |
| Scoped lifetime (scope context) | ⚠️ Partial | Need scope boundary API |
| Open generics support | ❌ Not supported | Complex for source gen |
| Keyed services | ❌ Not supported | Aligns with .NET 8 |
| Standalone package | ❌ Tied to Nuru | Need extraction |

### Design Decisions Needed (from Task 444)

1. **Registration Style:** Fluent API vs. Attribute-based vs. Both
   - Current: Fluent API only (`builder.Services.AddSingleton<T>()`)
   - Recommendation: Keep fluent API, add attributes for library authors

2. **Scope Semantics:** What defines a scope boundary?
   - Current: Scoped treated same as Singleton (static `Lazy<T>`)
   - Recommendation: CLI command invocation as scope boundary

3. **Open Generics:** `AddSingleton(typeof(IRepository<>), typeof(Repository<>))`
   - Current: Not supported
   - Complexity: High—requires generic type instantiation at compile time

4. **Keyed Services:** Align with .NET 8 `IKeyedServiceProvider`
   - Current: Not supported
   - Value: High for multi-tenant scenarios

---

## Code Extraction Plan

### Phase 1: Create TimeWarp.ServiceGen Package

**New Package Structure:**
```
source/timewarp-servicegen/
├── timewarp-servicegen.csproj
├── global-usings.cs
├── ServiceCollectionExtensions.cs      # AddServiceGen() extension
├── abstractions/
│   ├── IServiceCollection.cs           # Minimal abstraction
│   └── ServiceLifetime.cs              # Enum
├── models/
│   ├── ServiceDefinition.cs            # Extracted from Nuru
│   └── ServiceExtractionResult.cs       # Extracted from Nuru
├── extractors/
│   └── ServiceExtractor.cs             # Extracted from Nuru
├── emitters/
│   └── ServiceResolverEmitter.cs       # Extracted from Nuru
├── validation/
│   └── ServiceValidator.cs             # Extracted from Nuru
└── diagnostics/
    └── DiagnosticDescriptors.cs        # SG050-054 (renamed from NURU)
```

### Phase 2: Update Nuru to Use ServiceGen

**Changes in Nuru:**
1. Add `TimeWarp.ServiceGen` as package dependency
2. Remove extracted files from `timewarp-nuru-analyzers`
3. Update `InterceptorEmitter` to use `ServiceResolverEmitter` from ServiceGen
4. Update `HandlerInvokerEmitter` similarly
5. Keep Nuru-specific built-in services (`ITerminal`, `NuruApp`) as extensions

### Phase 3: Add Missing Features

**Priority Order:**
1. Factory delegate support (high value, moderate complexity)
2. Instance registration support (simple)
3. Circular dependency detection (important for correctness)
4. Proper scoped lifetime (requires scope API design)
5. Open generics (complex, defer if needed)
6. Keyed services (defer to align with .NET 8 adoption)

---

## Key Files to Extract

### Core Files (Direct Extraction)

| File | Lines | Extraction Complexity |
|------|-------|----------------------|
| `service-definition.cs` | 122 | Low - standalone record |
| `service-extractor.cs` | 884 | Medium - remove Nuru-specifics |
| `service-resolver-emitter.cs` | 374 | Medium - remove Nuru built-ins |
| `service-validator.cs` | 387 | Low - rename diagnostics |
| `diagnostic-descriptors.service.cs` | 71 | Low - rename prefix |

### Supporting Files (Partial Extraction)

| File | What to Extract |
|------|-----------------|
| `interceptor-emitter.cs` | `EmitServiceFields()`, `GetServiceFieldName()` |
| `handler-invoker-emitter.cs` | `ResolveServiceForCommand()` pattern |

---

## Dependencies and Integration Points

### Task 443 (TimeWarp.Mediator) Depends on This

From task 443:
```
TimeWarp.Mediator (depends on ServiceGen)
├── ISender<TScope>, IPublisher<TScope>, INotification
├── Handler/message abstractions
├── Source generator: emits typed dispatch, uses ServiceGen for handler DI
```

**ServiceGen must provide:**
- Handler constructor dependency resolution
- `Lazy<T>` fields for singleton handlers
- Runtime DI bridge for complex scenarios

### Related Tasks

| Task | Relationship |
|------|--------------|
| #393 | DI diagnostics - already implemented, extract to ServiceGen |
| #394 | Constructor dependency resolution - already implemented, extract to ServiceGen |
| #443 | Mediator extraction - depends on ServiceGen |

---

## Recommendations

### High Priority

1. **Extract ServiceGen as standalone package**
   - Start with direct extraction of existing code
   - Rename diagnostics from NURU0xx to SG0xx
   - Remove Nuru-specific built-in services (make them injectable)

2. **Add factory delegate support**
   - Current: NURU053 error
   - Target: Emit factory invocation code
   - Pattern: `services.AddSingleton<T>(() => new T(...))`

3. **Add instance registration support**
   - Pattern: `services.AddSingleton<T>(instance)`
   - Store in static field

### Medium Priority

4. **Implement circular dependency detection**
   - Add topological sort to service emission
   - Emit SG055 error for circular deps

5. **Design scope boundary API**
   - CLI command invocation as scope
   - `using var scope = app.CreateScope()` pattern

### Low Priority (Defer)

6. **Open generics support**
   - Complex for source generation
   - Defer until concrete use case

7. **Keyed services**
   - Align with .NET 8+ adoption
   - Defer until framework support is widespread

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Breaking existing Nuru tests | Medium | High | Extract incrementally, run CI tests after each change |
| ServiceGen API doesn't match MS DI exactly | Medium | Medium | Document differences, provide migration guide |
| Factory delegate emission complexity | Medium | Medium | Start with simple cases, expand incrementally |
| Scope semantics unclear | High | Medium | Define clear scope boundary before implementing |

---

## References

- Task 444: `/kanban/to-do/444-timewarp-servicegen-source-generated-aot-friendly-di-container.md`
- Task 443: `/kanban/to-do/443-extract-mediator-abstractions-to-timewarp-mediator-with-source-generated-dispatch.md`
- Task 393: `/kanban/to-do/393-add-di-diagnostics-for-unsupported-patterns.md`
- Task 394: `/kanban/to-do/394-implement-constructor-dependency-resolution.md`
- ServiceResolverEmitter: `/source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs`
- ServiceExtractor: `/source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs`
- ServiceDefinition: `/source/timewarp-nuru-analyzers/generators/models/service-definition.cs`
- ServiceValidator: `/source/timewarp-nuru-analyzers/validation/service-validator.cs`
- Test: `/tests/timewarp-nuru-tests/generator/generator-20-parameterized-service-constructor.cs`
- Test: `/tests/timewarp-nuru-tests/generator/generator-15-runtime-di.cs`
