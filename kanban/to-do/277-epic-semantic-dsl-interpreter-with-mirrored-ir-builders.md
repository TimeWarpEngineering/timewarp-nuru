# Epic: Semantic DSL Interpreter with Mirrored IR Builders

## Description

Replace syntax-based chain walking with a semantic interpreter that "steps through" the DSL code, calling corresponding IR builder methods. This naturally handles nested groups, temp variables, and fragmented code styles.

## Problem

The current `FluentChainExtractor` walks the Roslyn syntax tree to extract route information. This approach:

1. Breaks with nested `WithGroupPrefix()` calls - prefix accumulation doesn't work correctly
2. Is fragile to code refactoring (temp variables, fragmented styles)
3. Requires complex state tracking that doesn't match how the DSL actually works

## Solution

Create IR builders that mirror the DSL structure, then use Roslyn's semantic model to "interpret" the DSL code, calling corresponding IR builder methods. The IR builders naturally handle nested groups because they use the same CRTP pattern as the DSL.

## Key Design Decisions

1. **Handler Capture**: `IrRouteBuilder.WithHandler()` takes `HandlerDefinition` (already extracted by `HandlerExtractor`)
2. **Intercept Sites**: Include in interpreter flow - walk from `CreateBuilder()` through `Build()` to `RunAsync()`
3. **CRTP Pattern**: Use in IR builders for consistency with DSL
4. **Entry Point**: Start at `CreateBuilder()` call, walk forward
5. **Error Handling**: Fail fast on unrecognized DSL methods

## Child Tasks

- [ ] #278 Phase 1: POC - Minimal Fluent Case
- [ ] #279 Phase 2: Add Group Support (CRTP)
- [ ] #280 Phase 3: Handle Fragmented Styles
- [ ] #281 Phase 4: Additional DSL Methods
- [ ] #282 Phase 5: Integration & Cleanup

## Blocks

- #272 V2 Generator Phase 6: Testing (nested group tests)
- #276 Implement WithGroupPrefix (makes this approach unnecessary for that specific fix)

## Notes

### Why Semantic vs Syntax

The consumer could write equivalent code in many syntactic forms:

```csharp
// Style 1: Pure fluent
NuruApp.CreateBuilder([])
  .Map("ping").WithHandler(() => "pong").Done()
  .Build();

// Style 2: Mixed
var builder = NuruApp.CreateBuilder([]);
builder.Map("ping").WithHandler(() => "pong").Done();
builder.Build();

// Style 3: Fully fragmented
var builder = NuruApp.CreateBuilder([]);
var endpoint = builder.Map("ping");
endpoint.WithHandler(() => "pong");
endpoint.Done();
builder.Build();
```

All three are semantically equivalent. The interpreter handles all of these because it uses the semantic model to understand types and track variable state.

### Why Not Execute at Compile Time?

We researched whether we could compile and execute the DSL at generator time. Answer: technically possible but impractical due to:
- Missing runtime dependencies in generator context
- Security/sandboxing restrictions
- Performance implications

The interpreter approach gives us the benefits of mirroring the DSL execution without actually executing it.
