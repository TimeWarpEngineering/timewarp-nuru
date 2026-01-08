# AOT-compatible JSON serialization for user-defined return types

## Summary

Create `TimeWarp.Nuru.Build` project with an MSBuild task that generates a `JsonSerializerContext` 
with `[JsonSerializable]` attributes for user-defined return types **before** the main compilation,
so System.Text.Json's source generator can process it.

## Problem

Currently, when a handler returns a user-defined type (like `StatsResponse`), the generated code 
attempts JSON serialization using `NuruJsonSerializerContext` but falls back to `ToString()` 
because user-defined types aren't in the context.

This is due to a fundamental .NET limitation: **source generators cannot see each other's output**. 
They run in parallel, not sequentially.

```csharp
// User's code
[NuruRoute("stats")]
public sealed class StatsCommand : ICommand<StatsResponse> { ... }

// Generated code tries:
JsonSerializer.Serialize(result, NuruJsonSerializerContext.Default.Options);
// But StatsResponse isn't in NuruJsonSerializerContext, so falls back to:
result.ToString();  // Outputs "StatsResponse" instead of JSON
```

## Solution: MSBuild Pre-Compile Task

MSBuild tasks run **before** compilation and can output files that **are** visible to source generators.

### Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Build Timeline                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  1. Restore                                                              │
│  2. BeforeCompile                                                        │
│  3. ★ GenerateNuruJsonContext (NEW MSBuild Task)                        │
│     ├─ Creates Roslyn compilation from source files                     │
│     ├─ Uses AttributedRouteExtractor to find [NuruRoute] classes        │
│     ├─ Uses HandlerExtractor to get return types (T from ICommand<T>)   │
│     ├─ Generates NuruUserTypesJsonContext.g.cs                          │
│     └─ Writes to $(IntermediateOutputPath)                              │
│  4. CoreCompile                                                          │
│     ├─ System.Text.Json source generator sees [JsonSerializable]        │
│     └─ NuruGenerator runs (separate, parallel)                          │
│  5. AfterCompile                                                         │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Why This Works

- MSBuild tasks CAN reference any dependencies (unlike source generators)
- `TimeWarp.Nuru.Build` can reference `TimeWarp.Nuru.Analyzers` to reuse extraction logic
- Generated file is included in compilation BEFORE source generators run
- System.Text.Json source generator sees the `[JsonSerializable]` attributes

## Design Decisions

| Question | Decision |
|----------|----------|
| Namespace for generated context | `TimeWarp.Nuru.Generated` (consistent with other generated code) |
| No types needing JSON | Skip generation entirely |
| Extraction failure | Log build **warning**, continue build (fallback to `ToString()` still works) |

## Files to Create

```
source/timewarp-nuru-build/
├── timewarp-nuru-build.csproj
├── GenerateNuruJsonContextTask.cs
└── build/
    └── TimeWarp.Nuru.Build.targets
```

## Files to Modify

| File | Change |
|------|--------|
| `handler-invoker-emitter.cs` | Try `NuruUserTypesJsonContext` first, then existing fallback chain |
| `timewarp-nuru.csproj` | Reference new build project |

## Checklist

- [ ] Create `source/timewarp-nuru-build/` project structure
- [ ] Create `timewarp-nuru-build.csproj` with references to analyzer and MSBuild APIs
- [ ] Implement `GenerateNuruJsonContextTask.cs`:
  - [ ] Parse source files with Roslyn
  - [ ] Call `AttributedRouteExtractor` to find routes
  - [ ] Extract return types from handlers
  - [ ] Filter to JSON-serializable types (skip Unit, primitives)
  - [ ] Generate context file to `$(IntermediateOutputPath)`
  - [ ] Log warning on extraction failure
- [ ] Create `build/TimeWarp.Nuru.Build.targets`:
  - [ ] Wire up task to run `BeforeTargets="CoreCompile"`
  - [ ] Include generated file in `@(Compile)` items
- [ ] Update `handler-invoker-emitter.cs`:
  - [ ] Try `NuruUserTypesJsonContext` first
  - [ ] Fall back to `NuruJsonSerializerContext`
  - [ ] Fall back to `ToString()`
- [ ] Test with `samples/02-calculator/03-calc-mixed.cs` (StatsResponse)
- [ ] Test reference resolution (`@(ReferencePath)` vs other item groups)
- [ ] Verify AOT build works
- [ ] Package integration with main TimeWarp.Nuru package

## Expected Outcome

```bash
# Before (current)
dotnet run samples/02-calculator/03-calc-mixed.cs -- stats 1 2 3 4 5
StatsResponse

# After
dotnet run samples/02-calculator/03-calc-mixed.cs -- stats 1 2 3 4 5
{"sum":15,"average":3,"min":1,"max":5,"count":5}
```

## Risk: Reference Resolution

The MSBuild task needs to resolve type symbols, which requires assembly references. We'll need to 
test if `@(ReferencePath)` provides what we need, or if we need additional MSBuild items like:
- `@(Reference)`
- `@(ProjectReference)` resolved paths
- Framework reference assemblies

**Fallback option**: If reference resolution is problematic, use **syntax-only extraction** 
(find `ICommand<T>` pattern without full semantic analysis).

## Notes

### Background
- Task #306 implemented smart output strategy with Unit suppression
- JSON serialization works for types in `NuruJsonSerializerContext` (primitives, common collections)
- User-defined types fall back to `ToString()` which loses structure

### Related Files
- `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs` - EmitOutputForStrategy()
- `source/timewarp-nuru-analyzers/generators/extractors/attributed-route-extractor.cs` - Route discovery
- `source/timewarp-nuru-analyzers/generators/extractors/handler-extractor.cs` - Return type extraction
- `source/timewarp-nuru-core/serialization/nuru-json-serializer-context.cs` - Built-in context

### Generated File Example

```csharp
// <auto-generated/>
// Generated by TimeWarp.Nuru.Build for AOT-compatible JSON serialization
#nullable enable

namespace TimeWarp.Nuru.Generated;

[global::System.Text.Json.Serialization.JsonSerializable(typeof(global::StatsResponse))]
[global::System.Text.Json.Serialization.JsonSerializable(typeof(global::ComparisonResult))]
[global::System.Text.Json.Serialization.JsonSourceGenerationOptions(
  PropertyNamingPolicy = global::System.Text.Json.Serialization.JsonKnownNamingPolicy.CamelCase)]
internal partial class NuruUserTypesJsonContext 
  : global::System.Text.Json.Serialization.JsonSerializerContext;
```
