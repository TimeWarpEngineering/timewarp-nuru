# Source generator for design-time model

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Build testable helper methods in `sandbox/sourcegen/` that:
1. Convert parsed pattern syntax into `SegmentDefinition` array
2. Assemble `RouteDefinition` from pieces via a builder
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
| `HandlerDefinitionExtractor` | Extracts handler info from delegates or `IRequest<T>` types |
| `MetadataExtractor` | Extracts message type, description from fluent chain or attributes |
| `RouteDefinitionBuilder` | Assembles `RouteDefinition` from pieces |

## Checklist

### Setup
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

### Handler Extraction - TODO
- [ ] From delegate in `.WithHandler(delegate)` - extract parameter types, return type, async
- [ ] From `IRequest<T>` type (Attributed and Mediator) - extract properties, response type

### Metadata Extraction (Fluent API) - TODO
- [ ] Message type from `.AsQuery()`, `.AsCommand()`, `.AsIdempotentCommand()`
- [ ] Description from `.WithDescription("...")`
- [ ] Aliases from `.WithAlias("...")`

### Metadata Extraction (Attributed) - TODO
- [ ] Message type from `[Query]`, `[Command]`, `[IdempotentCommand]` attributes
- [ ] Description from `[Description("...")]` attribute
- [ ] Pattern from `[Route("...")]` attribute

### Route Definition Builder - DONE
- [x] Create `RouteDefinitionBuilder` with fluent API
- [x] `.WithPattern()`, `.WithSegments()`, `.WithMessageType()`
- [x] `.WithDescription()`, `.WithHandler()`, `.WithPipeline()`
- [x] `.WithAliases()`, `.WithGroupPrefix()`, `.WithSpecificity()`, `.WithOrder()`
- [x] `.Build()` produces immutable `RouteDefinition`

### Cleanup
- [x] Remove old `CompiledRouteToRouteDefinition`
- [x] Remove old test files
- [ ] Decide which segment approach to keep (Syntax vs CompiledRoute)

## File Structure

```
sandbox/sourcegen/
├── converters/
│   └── SegmentDefinitionConverter.cs   # FromSyntax(), FromCompiledRoute(), FromFluentBuilder()
├── extractors/
│   ├── HandlerDefinitionExtractor.cs   # From delegate, from IRequest<T>
│   └── MetadataExtractor.cs            # Message type, description, aliases
├── builders/
│   └── RouteDefinitionBuilder.cs       # Fluent builder for RouteDefinition
├── models/
│   └── design-time-model.cs            # RouteDefinition, SegmentDefinition, etc.
├── tests/
│   ├── segment-from-syntax-tests.cs           
│   └── segment-from-compiled-route-tests.cs   
├── program.cs
└── sourcegen.csproj
```

## Test Patterns (Segment Conversion)

| Pattern | FromSyntax | FromCompiledRoute | Notes |
|---------|------------|-------------------|-------|
| `"add {x:int} {y:int}"` | ✅ | ✅ | Literal + typed parameters |
| `"greet {name}"` | ✅ | ✅ | Untyped parameter |
| `"copy {source} {dest?}"` | ✅ | ✅ | Optional parameter |
| `"echo {*args}"` | ✅ | ✅ | Catch-all parameter |
| `"status --verbose?"` | ✅ | ✅ | Optional boolean flag |
| `"config --value {v:int}"` | ✅ | ⚠️ GAP | Option with typed parameter |
| `"run --force,-f"` | ✅ | ✅ | Short and long form option |
| `"log --level {l:int?}"` | ✅ | N/A | Optional typed parameter |

**Key Finding**: `FromCompiledRoute` loses option type constraints. `FromSyntax` preserves full fidelity.

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

### Why Keep Both Segment Approaches
- Evaluating trade-offs
- FromSyntax: Full fidelity but requires InternalsVisibleTo
- FromCompiledRoute: Uses public API but loses option type info
- Will decide after more evaluation

### Agent Context
- Agent name: Amina
- Working on Epic #239: Compile-time endpoint generation
- Step-1 and Step-2 complete (manual design-time and runtime construction)
