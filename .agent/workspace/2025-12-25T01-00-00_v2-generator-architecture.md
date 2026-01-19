# V2 Generator Architecture - Core Understanding

**Date:** 2024-12-25
**Context:** Task #243 (Emit Pre-sorted Endpoint Array) and Epic #239

## The Goal

Move all **deterministic** work to compile time. Only **unavoidable** work happens at runtime.

### Compile Time (Generated Code)
- Route pattern parsing
- CompiledRoute creation with segment matchers
- Endpoint creation
- Sorting by specificity
- **Invoker code** - the function that extracts typed parameters and calls the handler directly

### Runtime (Unavoidable)
- Matching args against routes (args are unknown at compile time)
- Executing the matched invoker

## The Flow

```
args[] → Matcher (runtime) → Endpoint → Invoker (generated code) → result
```

The matcher finds which endpoint matches the args. The invoker is **generated code** that directly calls the handler - no reflection, no registry lookup, no DI dispatch.

## Key Insight: The Generated Invoker

The V1 approach:
1. `Endpoint.Handler` is a `Delegate`
2. At runtime, `InvokerRegistry` looks up a typed invoker by signature
3. Invoker casts the delegate and calls it

The V2 approach:
1. Generator emits the invoker as **inline code**
2. No registry lookup - the invoker IS the code that calls the handler
3. Parameters are extracted and typed at compile time

### Example

**User writes:**
```csharp
.Map("add {x:int} {y:int}").WithHandler((int x, int y) => x + y)
```

**V2 Generator emits:**
```csharp
// The invoker extracts typed params and calls handler directly
Func<Dictionary<string, object>, object?> Invoker_0 = (parameters) =>
{
  int x = (int)parameters["x"];
  int y = (int)parameters["y"];
  return x + y;  // Direct call to handler logic, inlined
};

// Endpoint with pre-built route and invoker
Endpoint Endpoint_0 = new()
{
  RoutePattern = "add {x:int} {y:int}",
  CompiledRoute = new CompiledRoute { /* matchers */ },
  // Instead of Handler delegate, we have the generated invoker
};
```

## Reference Implementation

See `sandbox/experiments/manual-runtime-construction.cs` - this is the manual version of what the generator should produce.

Key sections:
- Lines 82-94: Building `CompiledRoute.FromDefinition()` with the invoker
- Lines 86-93: The invoker lambda that extracts typed params and calls handler
- Lines 107-113: `router.Match(args)` then `matchResult.Execute()`

## What the Existing Matcher Provides

The V1 matcher (`EndpointResolver`, `CompiledRoute.TryMatch()`) already:
- Matches args against route patterns
- Extracts parameter values into `Dictionary<string, string>`
- Returns the matched `Endpoint`

This can be **reused** for V2. The only change is what happens after matching - instead of going through `DelegateExecutor` and `InvokerRegistry`, we call the generated invoker directly.

## Execution Path Comparison

### V1 (Current)
```
args → EndpointResolver.Resolve() → Endpoint
     → DelegateExecutor.ExecuteAsync()
     → InvokerRegistry.TryGetSync(signatureKey)  // Reflection-based lookup
     → invoker(handler, args)                     // Cast delegate, invoke
```

### V2 (Target)
```
args → EndpointResolver.Resolve() → Endpoint
     → endpoint.GeneratedInvoker(extractedValues)  // Direct call to generated code
```

## Next Steps

1. Comment out all but 1 test in `routing-01-basic-matching.cs`
2. Modify V2 generator to emit `Invoker` functions (like the manual example)
3. Add a property to `Endpoint` (or V2 variant) to hold the generated invoker
4. Modify execution path to use generated invoker when available

## Related Files

- `sandbox/experiments/manual-runtime-construction.cs` - Reference implementation
- `sandbox/sourcegen/models/design-time-model.cs` - Design-time model types
- `sandbox/sourcegen/emitters/RuntimeCodeEmitter.cs` - Emitter (needs updating)
- `source/timewarp-nuru-analyzers/analyzers/nuru-v2-generator.cs` - V2 generator
- `kanban/in-progress/239-epic-compile-time-endpoint-generation-zero-cost-build.md` - Epic
