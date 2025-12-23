# Source generator for design-time model

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Build testable helper methods in `sandbox/sourcegen/` that:
1. Convert parsed pattern syntax into `SegmentDefinition` array
2. Assemble `RouteDefinition` from pieces via a builder
3. (Step-4 will handle Roslyn syntax extraction and runtime code emission)

All helper methods are public and unit-testable.

## Architecture

### Separation of Concerns

| Component | Responsibility |
|-----------|----------------|
| `SegmentDefinitionConverter` | Static class. Converts `Syntax` or `CompiledRoute` segments to `SegmentDefinition` array. Pure transformation. |
| `RouteDefinitionBuilder` | Fluent builder. Assembles `RouteDefinition` from pieces provided by different analysis phases. |
| Design-time models | `RouteDefinition`, `SegmentDefinition`, `HandlerDefinition`, etc. Immutable records. |

### Data Flow

```
Pattern string
  → PatternParser.Parse() → Syntax (internal) or CompiledRoute (public)
  → SegmentDefinitionConverter.FromSyntax() or .FromCompiledRoute()
  → ImmutableArray<SegmentDefinition>
  → RouteDefinitionBuilder.WithSegments(...)
  → .WithPattern(...), .WithHandler(...), etc.
  → .Build() → RouteDefinition
```

### Two Conversion Approaches (Evaluating Both)

**Approach A: FromSyntax (via InternalsVisibleTo)**
- Full fidelity - preserves all type constraints including option parameters
- Requires InternalsVisibleTo in parsing project

**Approach B: FromCompiledRoute (public API only)**
- Uses only public types
- **Gap**: Loses option parameter type constraints (`--value {v:int}` becomes untyped)
- Keeping for comparison

## Checklist

### Setup
- [x] Reorganize sandbox: existing experiments in `sandbox/experiments/`
- [x] Create `sandbox/sourcegen/` for new generator code
- [x] Add InternalsVisibleTo to `source/timewarp-nuru-parsing/timewarp-nuru-parsing.csproj`

### Segment Conversion
- [x] Create `SegmentDefinitionConverter.FromSyntax()` - converts from internal Syntax
- [x] Create `SegmentDefinitionConverter.FromCompiledRoute()` - converts from public CompiledRoute
- [x] Tests for FromSyntax with various patterns (8 patterns, all passing)
- [x] Tests for FromCompiledRoute with same patterns (7 patterns, documents gap)

### Route Definition Builder
- [x] Create `RouteDefinitionBuilder` with fluent API
- [x] `.WithPattern()`, `.WithSegments()`, `.WithMessageType()`
- [x] `.WithDescription()`, `.WithHandler()`, `.WithPipeline()`
- [x] `.WithAliases()`, `.WithGroupPrefix()`, `.WithSpecificity()`, `.WithOrder()`
- [x] `.Build()` produces immutable `RouteDefinition`

### Cleanup
- [x] Remove old `CompiledRouteToRouteDefinition` (replaced by SegmentDefinitionConverter)
- [x] Remove old test files (replaced by new test structure)
- [ ] Decide which approach to keep (Syntax vs CompiledRoute) - deferred, keeping both for now

## File Structure

```
sandbox/sourcegen/
├── converters/
│   └── SegmentDefinitionConverter.cs   # Static: FromSyntax(), FromCompiledRoute()
├── builders/
│   └── RouteDefinitionBuilder.cs       # Fluent builder for RouteDefinition
├── models/
│   └── design-time-model.cs            # RouteDefinition, SegmentDefinition, etc.
├── tests/
│   ├── segment-from-syntax-tests.cs           
│   └── segment-from-compiled-route-tests.cs   
├── program.cs                          # Test runner entry point
└── sourcegen.csproj
```

## Test Patterns

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

**Key Finding**: `FromCompiledRoute` loses option type constraints because `OptionMatcher` doesn't expose them. `FromSyntax` preserves full fidelity.

## Run Tests

```bash
dotnet run --project sandbox/sourcegen/sourcegen.csproj
```

## Notes

### Why Static Class for Converter
- No state - pure transformation
- Source generators run at compile time (no DI)
- Simpler testing - input → output
- Matches existing patterns (`PatternParser.Parse()` is static)

### Why Keep Both Approaches
- Evaluating trade-offs
- FromSyntax: Full fidelity but requires InternalsVisibleTo
- FromCompiledRoute: Uses public API but loses option type info
- Will decide after more evaluation in step-4

### Agent Context
- Agent name: Amina
- Working on Epic #239: Compile-time endpoint generation
- Step-1 and Step-2 complete (manual design-time and runtime construction)
