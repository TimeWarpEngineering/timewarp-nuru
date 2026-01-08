# AOT-compatible JSON serialization for user-defined return types

## Description

Currently, when a handler returns a user-defined type (like `StatsResponse`), the generated code attempts JSON serialization using `NuruJsonSerializerContext` but falls back to `ToString()` because user-defined types aren't in the context.

This is due to a fundamental .NET limitation: **source generators cannot see each other's output**. Our generator emits a partial `JsonSerializerContext` class, but the System.Text.Json source generator doesn't process it because it runs in parallel, not sequentially.

## Problem

```csharp
// User's code
[NuruRoute("stats")]
public sealed class StatsCommand : ICommand<StatsResponse> { ... }

// Generated code tries:
JsonSerializer.Serialize(result, NuruJsonSerializerContext.Default.Options);
// But StatsResponse isn't in NuruJsonSerializerContext, so falls back to:
result.ToString();  // Outputs "StatsResponse" instead of JSON
```

## Potential Solutions

### Option A: User-provided JsonSerializerContext
Allow users to register their own context:
```csharp
NuruApp.CreateBuilder(args)
  .WithJsonSerializerContext<MyJsonContext>()
  .Build();
```
Generator uses the provided context instead of `NuruJsonSerializerContext`.

### Option B: Attribute-based registration
Users mark their types:
```csharp
[NuruJsonSerializable]
public class StatsResponse { ... }
```
Generator collects these and emits `[JsonSerializable(typeof(StatsResponse))]` attributes on a user-visible partial class that the user completes.

### Option C: Emit partial class in user's namespace
Generate a partial `JsonSerializerContext` that users must complete:
```csharp
// Generated in user's namespace
[JsonSerializable(typeof(StatsResponse))]
public partial class AppJsonSerializerContext : JsonSerializerContext;

// User must add in their code:
// (empty file, just triggers System.Text.Json source generator)
```

### Option D: Documentation only
Document that users needing AOT-compatible JSON output must:
1. Create their own `JsonSerializerContext`
2. Override `ToString()` on their types
3. Use primitive return types

## Checklist

- [ ] Research if any workaround exists for source generator ordering
- [ ] Evaluate Option A (user-provided context)
- [ ] Evaluate Option B (attribute-based)
- [ ] Evaluate Option C (partial class in user namespace)
- [ ] Implement chosen solution
- [ ] Update samples to demonstrate pattern
- [ ] Document the limitation and workaround

## Notes

### Background
- Task #306 implemented smart output strategy with Unit suppression
- JSON serialization works for types in `NuruJsonSerializerContext` (primitives, common collections)
- User-defined types fall back to `ToString()` which loses structure

### Related Files
- `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs` - EmitOutputForStrategy()
- `source/timewarp-nuru-core/serialization/nuru-json-serializer-context.cs` - Built-in context

### Test Case
```bash
dotnet run samples/02-calculator/03-calc-mixed.cs -- stats 1 2 3 4 5
# Currently outputs: StatsResponse
# Should output: {"sum":15,"average":3,"min":1,"max":5,"count":5}
```
