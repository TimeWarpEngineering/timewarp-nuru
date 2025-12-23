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

### Key Insight

The Fluent Route Builder (#2) **doesn't need pattern parsing** - it's already structured! 
We extract directly from the Roslyn syntax of the `WithLiteral()`, `WithParameter()`, `WithOption()` calls.

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

### Segment Extraction - DONE
- [x] From `Syntax` (pattern parsing) - `SegmentDefinitionConverter.FromSyntax()`
- [x] From `CompiledRoute` (runtime) - `SegmentDefinitionConverter.FromCompiledRoute()`
- [x] Tests for FromSyntax (8 patterns, all passing)
- [x] Tests for FromCompiledRoute (7 patterns, documents gap)

### Segment Extraction - TODO
- [ ] From Fluent Route Builder (`WithLiteral()`, `WithParameter()`, `WithOption()`) - Roslyn analysis

### Handler Definition - DONE
- [x] `HandlerDefinitionBuilder` for constructing handler info
- [x] Support delegate, mediator, method handler kinds
- [x] Support parameters, options, flags, cancellation tokens, services
- [x] Support void, Task, Task<T>, sync return types
- [x] Tests for handler builder (7 tests)

### Handler Extraction (Roslyn) - TODO
- [ ] From delegate in `.WithHandler(delegate)` - extract parameter types, return type, async
- [ ] From `IRequest<T>` type (Attributed and Mediator) - extract properties, response type

### Route Definition Builder - DONE
- [x] Create `RouteDefinitionBuilder` with fluent API
- [x] `.WithPattern()`, `.WithSegments()`, `.WithMessageType()`
- [x] `.WithDescription()`, `.WithHandler()`, `.WithPipeline()`
- [x] `.WithAliases()`, `.WithGroupPrefix()`, `.WithSpecificity()`, `.WithOrder()`
- [x] `.Build()` produces immutable `RouteDefinition`

### Integration - DONE
- [x] Integration tests combining segments + handler + builder
- [x] Tests for sync handler, options, async with cancellation token

### Metadata Extraction (Fluent API) - TODO
- [ ] Message type from `.AsQuery()`, `.AsCommand()`, `.AsIdempotentCommand()`
- [ ] Description from `.WithDescription("...")`
- [ ] Aliases from `.WithAlias("...")`

### Metadata Extraction (Attributed) - TODO
- [ ] Message type from `[Query]`, `[Command]`, `[IdempotentCommand]` attributes
- [ ] Description from `[Description("...")]` attribute
- [ ] Pattern from `[Route("...")]` attribute

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

### Handler Info Needed for Code Emission
Without handler info, we can't emit working code. The handler tells us:
- Parameter names and types (for binding and type conversion)
- Return type (for invoker signature)
- Whether async (Task vs sync)
- Whether needs CancellationToken

### What's Ready for Step-4 (Code Emission)
We now have complete `RouteDefinition` objects containing:
- Parsed segments (from pattern)
- Handler definition (parameters, return type, async)
- Metadata (message type, description)

This is sufficient to start emitting code in step-4.

### Agent Context
- Agent name: Amina
- Working on Epic #239: Compile-time endpoint generation
- Step-1 and Step-2 complete (manual design-time and runtime construction)
