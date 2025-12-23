# Source generator for design-time model

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Build testable helper methods in `sandbox/sourcegen/` that:
1. Convert parsed pattern syntax into `SegmentDefinition` array
2. Assemble `RouteDefinition` from pieces via builders
3. Extract route metadata from various source types

All helper methods are public and unit-testable.

## Route Definition Sources

There are **four** ways routes can be defined:

| # | Source | Pattern From | Handler From | Metadata From |
|---|--------|--------------|--------------|---------------|
| 1 | Delegate + Pattern String | `Map("...")` argument | `.WithHandler(delegate)` | `.AsQuery()`, `.WithDescription()` |
| 2 | Delegate + Fluent Builder | `Map(r => r.WithLiteral()...)` | `.WithHandler(delegate)` | `.AsQuery()`, `.WithDescription()` |
| 3 | Attributed | `[Route("...")]` attribute | `IRequest<T>` properties | `[Query]`, `[Description]` attributes |
| 4 | Mediator + Pattern String | `Map<T>("...")` argument | `IRequest<T>` properties | `.AsQuery()`, `.WithDescription()` |

### 1. Delegate-based with Pattern String
```csharp
builder.Map("add {x:int} {y:int}")
  .WithHandler((int x, int y) => x + y)
  .WithDescription("Add two numbers")
  .AsQuery()
  .Done()
```

### 2. Delegate-based with Fluent Route Builder
```csharp
builder.Map(r => r
    .WithLiteral("deploy")
    .WithParameter("env")
    .WithOption("force", "f")
    .Done())
  .WithHandler((string env, bool force) => ...)
  .AsCommand()
  .Done()
```

### 3. Attributed Routes
```csharp
[Route("add {x:int} {y:int}")]
[Description("Add two numbers")]
[Query]
public class AddCommand : IRequest<int>
{
  public int X { get; set; }
  public int Y { get; set; }
}
```

### 4. Mediator-based with Pattern String
```csharp
builder.Map<AddCommand>("add {x:int} {y:int}")
  .WithDescription("Add two numbers")
  .AsQuery()
  .Done()
```

## Architecture

### Data Flow by Source

```
Pattern String (Map("..."))     ──► PatternParser ──► Syntax ──┐
                                                               │
Fluent Builder (Map(r => ...))  ──► Analyze builder calls ─────┤
                                                               ├──► SegmentDefinition[] ──┐
Attributed ([Route("...")])     ──► PatternParser ──► Syntax ──┤                          │
                                                               │                          ├──► RouteDefinitionBuilder ──► RouteDefinition
Mediator (Map<T>("..."))        ──► PatternParser ──► Syntax ──┘                          │
                                                                                          │
Handler/Metadata extraction ──────────────────────────────────────────────────────────────┘
```

### Separation of Concerns

| Component | Responsibility |
|-----------|----------------|
| `SegmentDefinitionConverter` | Converts `Syntax`, `CompiledRoute`, or fluent builder calls to `SegmentDefinition[]` |
| `HandlerDefinitionBuilder` | Builds handler info (parameters, return type, async) |
| `RouteDefinitionBuilder` | Assembles `RouteDefinition` from pieces |

## Checklist

### Setup - DONE
- [x] Reorganize sandbox: existing experiments in `sandbox/experiments/`
- [x] Create `sandbox/sourcegen/` for new generator code
- [x] Add InternalsVisibleTo to `source/timewarp-nuru-parsing/timewarp-nuru-parsing.csproj`

### Shared Infrastructure - DONE
- [x] `SegmentDefinitionConverter.FromSyntax()` - pattern string → segments
- [x] `SegmentDefinitionConverter.FromCompiledRoute()` - (keeping for comparison)
- [x] `HandlerDefinitionBuilder` - construct handler info
- [x] `RouteDefinitionBuilder` - assemble RouteDefinition from pieces
- [x] Tests for segment conversion (15 tests)
- [x] Tests for handler builder (7 tests)
- [x] Integration tests (3 tests)

---

### Source 1: Delegate + Pattern String - COMPLETE

Extract from: `Map("pattern").WithHandler(delegate).WithDescription("...").AsQuery()`

| What | Extract From | Status |
|------|--------------|--------|
| Pattern string | `Map("...")` first argument | [x] DONE |
| Segments | Parse pattern → `FromSyntax()` | [x] DONE |
| Handler params | Delegate parameter list | [x] DONE |
| Handler return | Delegate return type | [x] DONE (syntax-only) |
| Handler async | Check if returns Task/Task<T> | [x] DONE |
| Message type | `.AsQuery()`, `.AsCommand()`, `.AsIdempotentCommand()` | [x] DONE |
| Description | `.WithDescription("...")` argument | [x] DONE |
| Aliases | `.WithAlias("...")` arguments | [x] DONE |

**Files:**
- `extractors/FluentChainExtractor.cs` - walks fluent chain
- `extractors/DelegateAnalyzer.cs` - analyzes lambda parameters
- `tests/fluent-chain-extractor-tests.cs` - 6 tests, all pass

---

### Source 2: Delegate + Fluent Route Builder - COMPLETE

Extract from: `Map(r => r.WithLiteral(...).WithParameter(...)).WithHandler(delegate)`

| What | Extract From | Status |
|------|--------------|--------|
| Segments | `WithLiteral()`, `WithParameter()`, `WithOption()` calls | [x] DONE |
| Handler params | Delegate parameter list | [x] DONE (same as Source 1) |
| Handler return | Delegate return type | [x] DONE (same as Source 1) |
| Handler async | Check if returns Task/Task<T> | [x] DONE (same as Source 1) |
| Message type | `.AsQuery()`, `.AsCommand()`, `.AsIdempotentCommand()` | [x] DONE (same as Source 1) |
| Description | `.WithDescription("...")` argument | [x] DONE (same as Source 1) |

**Files:**
- `extractors/FluentRouteBuilderExtractor.cs` - extracts segments from builder lambda
- `tests/fluent-route-builder-extractor-tests.cs` - 8 tests, all pass

---

### Source 3: Attributed Routes - COMPLETE

Extract from: `[Route("pattern")] [Query] class Foo : IRequest<T> { properties }`

| What | Extract From | Status |
|------|--------------|--------|
| Pattern string | `[Route("...")]` attribute argument | [x] DONE |
| Segments | Parse pattern → `FromSyntax()` | [x] DONE |
| Handler params | Public properties on class | [x] DONE |
| Handler return | `IRequest<T>` type argument `T` | [x] DONE |
| Handler async | Always async (mediator) | [x] DONE (implied) |
| Message type | `[Query]`, `[Command]`, `[IdempotentCommand]` attributes | [x] DONE |
| Description | `[Description("...")]` attribute | [x] DONE |

**Files:**
- `extractors/AttributedRouteExtractor.cs` - extracts from class attributes and properties
- `tests/attributed-route-extractor-tests.cs` - 8 tests, all pass

---

### Source 4: Mediator + Pattern String - COMPLETE

Extract from: `Map<TRequest>("pattern").WithDescription("...").AsQuery()`

| What | Extract From | Status |
|------|--------------|--------|
| Pattern string | `Map<T>("...")` first argument | [x] DONE |
| Segments | Parse pattern → `FromSyntax()` | [x] DONE |
| Handler params | Public properties on `T` | [x] DONE (same as Source 3) |
| Handler return | `IRequest<TResponse>` type argument | [x] DONE (same as Source 3) |
| Handler async | Always async (mediator) | [x] DONE (implied) |
| Message type | `.AsQuery()`, `.AsCommand()`, `.AsIdempotentCommand()` | [x] DONE |
| Description | `.WithDescription("...")` argument | [x] DONE |

**Files:**
- `extractors/MediatorRouteExtractor.cs` - extracts from Map<T> chains
- `tests/mediator-route-extractor-tests.cs` - 7 tests, all pass

---

### Cleanup
- [x] Remove old `CompiledRouteToRouteDefinition`
- [x] Remove old test files
- [x] Keep both segment approaches (Syntax preferred, CompiledRoute for comparison)

## File Structure

```
sandbox/sourcegen/
├── converters/
│   └── SegmentDefinitionConverter.cs   # FromSyntax(), FromCompiledRoute()
├── extractors/
│   ├── FluentChainExtractor.cs         # Source 1: Map("...").WithHandler(delegate)
│   ├── FluentRouteBuilderExtractor.cs  # Source 2: Map(r => r.WithLiteral()...)
│   ├── AttributedRouteExtractor.cs     # Source 3: [Route] attributed classes
│   ├── MediatorRouteExtractor.cs       # Source 4: Map<T>("...")
│   └── DelegateAnalyzer.cs             # Analyzes lambda parameters/returns
├── builders/
│   ├── RouteDefinitionBuilder.cs       # Assembles RouteDefinition
│   └── HandlerDefinitionBuilder.cs     # Builds HandlerDefinition
├── models/
│   └── design-time-model.cs            # RouteDefinition, SegmentDefinition, etc.
├── tests/
│   ├── segment-from-syntax-tests.cs           
│   ├── segment-from-compiled-route-tests.cs   
│   ├── handler-definition-builder-tests.cs
│   ├── route-definition-integration-tests.cs
│   ├── fluent-chain-extractor-tests.cs        # Source 1 tests
│   ├── fluent-route-builder-extractor-tests.cs # Source 2 tests
│   ├── attributed-route-extractor-tests.cs    # Source 3 tests
│   └── mediator-route-extractor-tests.cs      # Source 4 tests
├── program.cs
└── sourcegen.csproj
```

## Test Summary

| Test Suite | Tests | Status |
|------------|-------|--------|
| FromSyntax segments | 8 | ✅ All pass |
| FromCompiledRoute segments | 7 | ✅ All pass (gap documented) |
| HandlerDefinitionBuilder | 7 | ✅ All pass |
| Integration (complete RouteDefinition) | 3 | ✅ All pass |
| FluentChainExtractor (Source 1) | 6 | ✅ All pass |
| FluentRouteBuilderExtractor (Source 2) | 8 | ✅ All pass |
| AttributedRouteExtractor (Source 3) | 8 | ✅ All pass |
| MediatorRouteExtractor (Source 4) | 7 | ✅ All pass |
| **Total** | **54** | ✅ |

## Run Tests

```bash
dotnet run --project sandbox/sourcegen/sourcegen.csproj
```

## Notes

### Shared Extraction Logic

Several pieces are shared across sources:
- **Delegate analysis**: Sources 1 & 2 both need to extract from `.WithHandler(delegate)`
- **IRequest<T> analysis**: Sources 3 & 4 both need to extract from request type properties
- **Fluent metadata**: Sources 1, 2 & 4 use `.AsQuery()`, `.WithDescription()`, etc.
- **Attribute metadata**: Source 3 uses `[Query]`, `[Description]`, etc.

### Step-3 Complete - Ready for Step-4 (Code Emission)

All four route definition sources are fully implemented and tested:
- **Source 1**: Delegate + Pattern String (6 tests)
- **Source 2**: Delegate + Fluent Route Builder (8 tests)
- **Source 3**: Attributed Routes (8 tests)
- **Source 4**: Mediator + Pattern String (7 tests)

Step-4 can now emit runtime code from any `RouteDefinition`, regardless of how it was defined.

### Agent Context
- Agent name: Amina
- Working on Epic #239: Compile-time endpoint generation
- Step-1 and Step-2 complete (manual design-time and runtime construction)
- Step-3 complete (all four sources extracting to RouteDefinition)
