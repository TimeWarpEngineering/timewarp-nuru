# V2 Generator Runtime Types Analysis

## Executive Summary

The V2 generator implementation (Task #262) copied sandbox code that emits **new runtime types** instead of using the **existing runtime types**. This causes CS0436 type conflicts when projects reference the library. The sandbox approach was a deviation from the documented plan in Task #243, which explicitly shows using existing types like `LiteralMatcher`, `ParameterMatcher`, and `CompiledRoute`.

## Scope

Analysis of why the V2 generator build fails with type conflicts, and whether to use existing runtime types or generate new ones.

## Methodology

1. Traced the build error (CS0436 type conflicts for `ISegmentMatcher`, `CompiledRoute`, etc.)
2. Compared sandbox-generated types vs existing runtime types
3. Searched all kanban tasks and markdown documentation for architectural decisions
4. Found explicit evidence in Task #243 showing the intended approach

## Findings

### The Build Error

When building tests with `UseNewGen=true`, CS0436 errors occur:

```
error CS0436: The type 'ISegmentMatcher' in 'GeneratedRoutes.g.cs' conflicts 
with the imported type 'ISegmentMatcher' in 'TimeWarp.Nuru, Version=3.0.0.0'
```

**Root Cause**: The V2 generator runs on both:
1. `timewarp-nuru` library → generates types in `TimeWarp.Nuru.V2.Generated`
2. Test project (references library) → generates same types in same namespace

Two definitions of the same types = conflict.

### Sandbox Types vs Existing Runtime Types

The sandbox created entirely different types:

| Sandbox (Generated)           | Existing Runtime                          |
| ----------------------------- | ----------------------------------------- |
| `ISegmentMatcher` (interface) | `RouteMatcher` (abstract class)           |
| `LiteralMatcher : ISegmentMatcher` | `LiteralMatcher : RouteMatcher`           |
| `IntParameterMatcher`         | Does not exist (uses `ParameterMatcher`)  |
| `StringParameterMatcher`      | Does not exist (uses `ParameterMatcher`)  |
| `ParameterExtractor`          | Does not exist                            |
| `TypeConverter`               | Uses `TypeConverterProvider`              |
| `Router`                      | Uses `EndpointResolver`                   |
| `MatchResult`                 | Does not exist                            |
| `MatchAttempt`                | Does not exist                            |

The `CompiledRoute` types also have different shapes:

**Existing Runtime CompiledRoute:**
```csharp
public class CompiledRoute
{
    public required IReadOnlyList<RouteMatcher> Segments { get; set; }
    public MessageType MessageType { get; set; }
    public string? CatchAllParameterName { get; set; }
    public int Specificity { get; set; }
    // Cached properties: PositionalMatchers, OptionMatchers, RepeatedOptions
}
```

**Sandbox CompiledRoute:**
```csharp
internal sealed class CompiledRoute
{
    public string Pattern { get; }
    public ISegmentMatcher[] SegmentMatchers { get; }
    public ParameterExtractor[] ParameterExtractors { get; }
    public Func<Dictionary<string, object>, object?> Invoker { get; }
}
```

### How This Happened

1. **Task #242-step-2** (Manual runtime construction) chose "Option B: Build custom types" with this note:
   > "Used **Option B**: Built minimal custom runtime structures inline rather than using existing Nuru types. This clearly shows what needs to be generated and **keeps the experiment self-contained.**"

2. This was a **sandbox decision** for isolation during experimentation, not a final architectural decision.

3. **Task #242-step-4** (Source generator emits runtime) built on step-2 and created `RuntimeCodeEmitter` that emits these sandbox types.

4. **Task #262** (V2 Generator Core Endpoint Generation) then copied this sandbox code into the real analyzer project without reconsidering whether the sandbox approach was appropriate.

5. No one documented whether "use existing types" or "generate new types" was the intended final approach.

### Evidence of Intended Approach

**Task #243** (Emit pre-sorted Endpoint array) explicitly documents the expected generated output:

```csharp
internal static readonly Endpoint[] All =
[
    new Endpoint
    {
        CompiledRoute = new CompiledRoute
        {
            Segments =
            [
                new LiteralMatcher("add"),
                new ParameterMatcher("a", isCatchAll: false, "int"),
                new ParameterMatcher("b", isCatchAll: false, "int"),
            ],
            Specificity = 140,
            MessageType = MessageType.Query
        },
        // ...
    },
];
```

This shows:
- `new LiteralMatcher("add")` - **existing** type
- `new ParameterMatcher(...)` - **existing** type  
- `new CompiledRoute { Segments = [...] }` - **existing** type
- Only a container class in `TimeWarp.Nuru.Generated` namespace

The intent was always to use existing runtime types, not create new ones.

### Additional Evidence

The epic (#239) states:
- "Source generator emits **pre-built instances**" - instances of existing types
- "Two-layer data model - Rich design-time model for generator, **minimal runtime model**" - existing runtime is already minimal

Task #249 (Delete runtime infrastructure) lists what gets deleted but notably does NOT include:
- `CompiledRoute`
- `LiteralMatcher` / `ParameterMatcher` / `RouteMatcher`
- The matcher infrastructure

This confirms these types are meant to remain and be used by V2.

## Recommendations

### Immediate Fix for #262

1. **Rewrite `runtime-code-emitter.cs`** to use existing runtime types:
   - Remove `EmitRuntimeTypes()` method entirely (~250 lines)
   - Set `IncludeRuntimeTypes = false` as default
   - Emit code that instantiates `TimeWarp.Nuru.LiteralMatcher`, `TimeWarp.Nuru.ParameterMatcher`, `TimeWarp.Nuru.CompiledRoute`
   - Add `using TimeWarp.Nuru;` to generated output

2. **Update Task #262** with a "Decisions" section documenting this finding

3. **Identify gaps** - The existing `CompiledRoute` doesn't have an `Invoker` property. Need to determine how V2 connects routes to handlers.

### Process Improvement

1. **Document decisions when made** - The sandbox should have explicitly noted "using custom types FOR SANDBOX ONLY - production should use existing types"

2. **Review before copying** - When moving sandbox code to production, review whether sandbox shortcuts are appropriate

3. **Cross-reference tasks** - Task #262 should have referenced Task #243 which shows the expected output format

## References

| Document | Key Finding |
|----------|-------------|
| `kanban/to-do/243-emit-pre-sorted-endpoint-array.md` | Shows expected output using existing types |
| `kanban/done/242-step-2-manual-runtime-construction.md` | Documents "Option B" sandbox decision |
| `kanban/done/242-step-4-source-generator-emits-runtime.md` | Built emitter using sandbox types |
| `kanban/in-progress/239-epic-compile-time-endpoint-generation-zero-cost-build.md` | Epic vision, lists what gets deleted (not matchers) |
| `kanban/to-do/249-delete-runtime-infrastructure-after-migration.md` | Lists deletions, confirms matchers stay |
| `source/timewarp-nuru-parsing/parsing/runtime/` | Existing runtime types |
| `source/timewarp-nuru-analyzers/analyzers/emitters/runtime-code-emitter.cs` | Current (broken) emitter |
