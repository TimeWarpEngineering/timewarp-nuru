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

- [x] Create `source/timewarp-nuru-build/` project structure
- [x] Create `timewarp-nuru-build.csproj` with references to analyzer and MSBuild APIs
- [x] Implement `GenerateNuruJsonContextTask.cs`:
  - [x] Parse source files with Roslyn
  - [x] Call `AttributedRouteExtractor` to find routes
  - [x] Extract return types from handlers
  - [x] Filter to JSON-serializable types (skip Unit, primitives)
  - [x] Generate context file to `$(IntermediateOutputPath)`
  - [x] Log warning on extraction failure
- [x] Create `build/TimeWarp.Nuru.Build.targets`:
  - [x] Wire up task to run `BeforeTargets="CoreCompile"`
  - [x] Include generated file in `@(Compile)` items
- [x] Update `handler-invoker-emitter.cs`:
  - [x] Try `NuruUserTypesJsonContext` first
  - [x] Fall back to `NuruJsonSerializerContext`
  - [x] Fall back to `ToString()`
- [x] Test with `samples/02-calculator/03-calc-mixed.cs` (StatsResponse)
- [x] Test reference resolution (`@(ReferencePath)` vs other item groups)
- [x] Verify AOT build works
- [ ] Package integration with main TimeWarp.Nuru package (deferred - works for local development)

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

## Results

**Status:** Working for local development. Package integration deferred.

### What Works

1. **MSBuild Task** (`GenerateNuruJsonContextTask.cs`):
   - **Uses the same IR infrastructure as the source generator**
   - `DslInterpreter` for delegate routes (`.Map(...).WithHandler(...)`)
   - `AttributedRouteExtractor` for `[NuruRoute]` attributed classes
   - Extracts return types from `RouteDefinition.Handler.ReturnType`
   - Filters out primitives and Unit types
   - Generates `NuruUserTypesJsonContext.g.cs` to intermediate output

2. **Targets Integration**:
   - `Directory.Build.targets` at repo root imports the MSBuild targets
   - Runs `BeforeTargets="CoreCompile"` so System.Text.Json source generator sees it

3. **Handler Emitter**:
   - Updated to try `NuruUserTypesJsonContext.Default.Options` first
   - Falls back to built-in `NuruJsonSerializerContext.Default.Options`
   - Final fallback to `ToString()`

### Test Results

```bash
# JSON output for attributed routes (StatsResponse from [NuruRoute])
$ dotnet run samples/02-calculator/03-calc-mixed.cs -- stats 1 2 3 4 5
{"sum":15,"average":3,"min":1,"max":5,"count":5}

# JSON output for delegate routes (ComparisonResult from .Map().WithHandler())
$ dotnet run samples/02-calculator/03-calc-mixed.cs -- compare 10 5
{"x":10,"y":5,"isEqual":false,"difference":5,"ratio":2}

# AOT build succeeds without warnings
$ dotnet publish samples/05-aot-example -c Release
# Creates ~10MB native binary
```

### Key Improvements

1. **Initial fix**: `Directory.Build.targets` was in `source/` but samples are in `samples/` (siblings, not nested). Moving the targets to repo root fixed the issue.

2. **Reuse of IR infrastructure**: Initially the MSBuild task did its own primitive parsing looking only for `[NuruRoute]` classes. Refactored to use `DslInterpreter` and `AttributedRouteExtractor` - the same components the source generator uses. This ensures:
   - Single source of truth for route extraction
   - Both delegate AND attributed routes are handled
   - Consistent extraction logic

### Remaining Work (Package Integration)

When packaging for NuGet:
1. Enable `<IsPackable>true</IsPackable>` in `timewarp-nuru-build.csproj`
2. Include task DLL and dependencies in package's `build/` folder
3. Update main `TimeWarp.Nuru` package to depend on or include the build task
4. Test with NuGet package reference (not project reference)
