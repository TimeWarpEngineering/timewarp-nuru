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

### Source 1: Delegate + Pattern String

Extract from: `Map("pattern").WithHandler(delegate).WithDescription("...").AsQuery()`

| What | Extract From | Status |
|------|--------------|--------|
| Pattern string | `Map("...")` first argument | [ ] TODO |
| Segments | Parse pattern → `FromSyntax()` | [x] DONE (converter ready) |
| Handler params | Delegate parameter list | [ ] TODO |
| Handler return | Delegate return type | [ ] TODO |
| Handler async | Check if returns Task/Task<T> | [ ] TODO |
| Message type | `.AsQuery()`, `.AsCommand()`, `.AsIdempotentCommand()` | [ ] TODO |
| Description | `.WithDescription("...")` argument | [ ] TODO |
| Aliases | `.WithAlias("...")` arguments | [ ] TODO |

---

### Source 2: Delegate + Fluent Route Builder

Extract from: `Map(r => r.WithLiteral(...).WithParameter(...)).WithHandler(delegate)`

| What | Extract From | Status |
|------|--------------|--------|
| Segments | `WithLiteral()`, `WithParameter()`, `WithOption()` calls | [ ] TODO |
| Handler params | Delegate parameter list | [ ] TODO (same as Source 1) |
| Handler return | Delegate return type | [ ] TODO (same as Source 1) |
| Handler async | Check if returns Task/Task<T> | [ ] TODO (same as Source 1) |
| Message type | `.AsQuery()`, `.AsCommand()`, `.AsIdempotentCommand()` | [ ] TODO (same as Source 1) |
| Description | `.WithDescription("...")` argument | [ ] TODO (same as Source 1) |

---

### Source 3: Attributed Routes

Extract from: `[Route("pattern")] [Query] class Foo : IRequest<T> { properties }`

| What | Extract From | Status |
|------|--------------|--------|
| Pattern string | `[Route("...")]` attribute argument | [ ] TODO |
| Segments | Parse pattern → `FromSyntax()` | [x] DONE (converter ready) |
| Handler params | Public properties on class | [ ] TODO |
| Handler return | `IRequest<T>` type argument `T` | [ ] TODO |
| Handler async | Always async (mediator) | [x] DONE (implied) |
| Message type | `[Query]`, `[Command]`, `[IdempotentCommand]` attributes | [ ] TODO |
| Description | `[Description("...")]` attribute | [ ] TODO |

---

### Source 4: Mediator + Pattern String

Extract from: `Map<TRequest>("pattern").WithDescription("...").AsQuery()`

| What | Extract From | Status |
|------|--------------|--------|
| Pattern string | `Map<T>("...")` second argument | [ ] TODO |
| Segments | Parse pattern → `FromSyntax()` | [x] DONE (converter ready) |
| Handler params | Public properties on `T` | [ ] TODO (same as Source 3) |
| Handler return | `IRequest<TResponse>` type argument | [ ] TODO (same as Source 3) |
| Handler async | Always async (mediator) | [x] DONE (implied) |
| Message type | `.AsQuery()`, `.AsCommand()`, `.AsIdempotentCommand()` | [ ] TODO (same as Source 1) |
| Description | `.WithDescription("...")` argument | [ ] TODO (same as Source 1) |

---

### Cleanup
- [x] Remove old `CompiledRouteToRouteDefinition`
- [x] Remove old test files
- [ ] Decide which segment approach to keep (Syntax vs CompiledRoute)

## File Structure

```
sandbox/sourcegen/
├── converters/
│   └── SegmentDefinitionConverter.cs   # FromSyntax(), FromCompiledRoute()
├── builders/
│   ├── RouteDefinitionBuilder.cs       # Assembles RouteDefinition
│   └── HandlerDefinitionBuilder.cs     # Builds HandlerDefinition
├── models/
│   └── design-time-model.cs            # RouteDefinition, SegmentDefinition, etc.
├── tests/
│   ├── segment-from-syntax-tests.cs           
│   ├── segment-from-compiled-route-tests.cs   
│   ├── handler-definition-builder-tests.cs
│   └── route-definition-integration-tests.cs
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
| **Total** | **25** | ✅ |

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

### What's Ready for Step-4 (Code Emission)

We have the builders ready. Once we extract from any source, we can emit code.
For MVP, we could start with Source 1 (simplest) and expand from there.

### Agent Context
- Agent name: Amina
- Working on Epic #239: Compile-time endpoint generation
- Step-1 and Step-2 complete (manual design-time and runtime construction)
