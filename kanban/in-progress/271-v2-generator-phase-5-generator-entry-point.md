# V2 Generator Phase 5: Generator Entry Point

## Description

Create the main incremental source generator that wires together locators, extractors, and emitters to produce the `RunAsync` interceptor.

## Parent

#265 Epic: V2 Source Generator Implementation

## Key References

**IMPORTANT: Read these before starting:**

1. **Architecture Document:**
   `.agent/workspace/2024-12-25T14-00-00_v2-source-generator-architecture.md`
   - Full pipeline design (Locate → Extract → Emit)
   - Three DSL support (Fluent, Mini-Language, Attributed)
   - Generated code structure
   - Incremental generator patterns

2. **Locators (Phase 2):**
   `source/timewarp-nuru-analyzers/generators/locators/`
   - `run-async-locator.cs` - Entry point, provides `IsPotentialMatch` and `Extract`
   - `create-builder-locator.cs` - Finds `NuruApp.CreateBuilder()` calls
   - `nuru-route-attribute-locator.cs` - Finds `[NuruRoute]` decorated classes
   - 25 total locator files

3. **Extractors (Phase 3):**
   `source/timewarp-nuru-analyzers/generators/extractors/`
   - `app-extractor.cs` - Main orchestrator, returns `AppModel`
   - `fluent-chain-extractor.cs` - Extracts routes from builder chain
   - `attributed-route-extractor.cs` - Extracts from `[NuruRoute]` classes
   - 7 total extractor files

4. **Emitters (Phase 4):**
   `source/timewarp-nuru-analyzers/generators/emitters/`
   - `interceptor-emitter.cs` - Main entry, produces complete interceptor source
   - 7 total emitter files

5. **Models (Phase 1):**
   `source/timewarp-nuru-analyzers/generators/models/`
   - `app-model.cs` - Top-level IR with Routes, Behaviors, Services
   - `intercept-site-model.cs` - FilePath, Line, Column for `[InterceptsLocation]`

6. **Reference Generator:**
   `source/timewarp-nuru-analyzers/reference-only/nuru-interceptor-generator.cs`
   - Existing interceptor generator pattern
   - Shows `IIncrementalGenerator` implementation

## Technical Notes

### Namespace Conflict
Due to `TimeWarp.Nuru.SyntaxNode` shadowing `Microsoft.CodeAnalysis.SyntaxNode`, use:
```csharp
using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;
```

### Coding Standards
Follow `documentation/developer/standards/csharp-coding.md`:
- PascalCase for private fields (no underscore prefix)
- 2-space indentation
- Allman bracket style
- No `var` keyword

### Incremental Generator Best Practices
- Use `CreateSyntaxProvider` for efficient filtering
- Avoid allocations in predicate functions
- Make extracted data equatable for caching
- Handle null/empty cases gracefully
- Use `ForAttributeWithMetadataName` for attribute-based discovery

## Checklist

### Commit 5.1: Create NuruGenerator
- [ ] Create `generators/nuru-generator.cs`
- [ ] Implement `IIncrementalGenerator.Initialize`
- [ ] Wire up `RunAsyncLocator` as syntax provider
- [ ] Wire up attributed route detection via `ForAttributeWithMetadataName`
- [ ] Combine fluent and attributed routes
- [ ] Call `InterceptorEmitter` to produce output
- [ ] Register source output with `ctx.AddSource()`
- [ ] Verify build succeeds

## Detailed Design

### Generator Pipeline

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          NURU GENERATOR PIPELINE                             │
└─────────────────────────────────────────────────────────────────────────────┘

  SyntaxProvider                 Combine                    Emit
  ─────────────                  ───────                    ────

  ┌─────────────────┐
  │ RunAsyncLocator │
  │ IsPotentialMatch│       ┌──────────────────┐
  │     Extract     │──────▶│                  │
  └─────────────────┘       │   Combine all    │       ┌──────────────────┐
                            │   inputs into    │       │                  │
  ┌─────────────────┐       │   AppExtractor   │──────▶│ InterceptorEmit  │
  │ ForAttribute    │──────▶│                  │       │ .Emit(model)     │
  │ NuruRouteAttr   │       │   Returns        │       │                  │
  └─────────────────┘       │   AppModel       │       │ → .g.cs file     │
                            └──────────────────┘       └──────────────────┘
```

### Generator Structure

```csharp
namespace TimeWarp.Nuru.Generators;

[Generator]
public sealed class NuruGenerator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // 1. Locate RunAsync call sites (entry points)
    IncrementalValuesProvider<InterceptSiteModel?> runAsyncCalls = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: RunAsyncLocator.IsPotentialMatch,
        transform: RunAsyncLocator.Extract)
      .Where(static site => site is not null);

    // 2. Locate attributed routes ([NuruRoute] classes)
    IncrementalValuesProvider<RouteDefinition?> attributedRoutes = context.SyntaxProvider
      .ForAttributeWithMetadataName(
        "TimeWarp.Nuru.NuruRouteAttribute",
        predicate: static (node, _) => node is ClassDeclarationSyntax,
        transform: AttributedRouteExtractor.Extract)
      .Where(static route => route is not null);

    // 3. Combine and extract full AppModel
    IncrementalValueProvider<AppModel?> appModel = runAsyncCalls
      .Collect()
      .Combine(attributedRoutes.Collect())
      .Select(static (data, ct) => AppExtractor.ExtractFromCombined(data, ct));

    // 4. Emit generated code
    context.RegisterSourceOutput(appModel, static (ctx, model) =>
    {
      if (model is null) return;
      string source = InterceptorEmitter.Emit(model);
      ctx.AddSource("NuruGenerated.g.cs", source);
    });
  }
}
```

### Key Integration Points

**RunAsyncLocator → InterceptSiteModel:**
```csharp
// RunAsyncLocator.Extract returns InterceptSiteModel with:
// - FilePath: Full path to source file
// - Line: 1-based line number
// - Column: 1-based column of method name
// Used for [InterceptsLocation] attribute
```

**AppExtractor Coordination:**
```csharp
// AppExtractor.Extract needs to:
// 1. Find the builder chain from RunAsync call site
// 2. Use FluentChainExtractor for fluent routes
// 3. Merge in attributed routes from ForAttributeWithMetadataName
// 4. Return complete AppModel
```

**InterceptorEmitter Output:**
```csharp
// Produces:
// - File-scoped class with [InterceptsLocation]
// - Route matching using C# list patterns
// - Handler invocation code
// - Built-in flags (--help, --version, --capabilities)
```

### UseNewGen Flag Consideration

The generator can optionally check the `UseNewGen` MSBuild property:
- **Option A:** Always run (simpler during development)
- **Option B:** Check via `context.AnalyzerConfigOptionsProvider`

```csharp
// Option B implementation:
IncrementalValueProvider<bool> useNewGen = context.AnalyzerConfigOptionsProvider
  .Select(static (provider, _) =>
  {
    provider.GlobalOptions.TryGetValue("build_property.UseNewGen", out string? value);
    return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
  });

// Then combine with appModel and only emit if enabled
```

## Output

After Phase 5, the generator produces:
```
NuruGenerated.g.cs
├── // <auto-generated/>
├── #nullable enable
├── namespace TimeWarp.Nuru.Generated;
├── file static class GeneratedInterceptor
│   ├── [InterceptsLocation(...)]
│   ├── public static async Task<int> RunAsync_Intercepted(...)
│   │   ├── Built-in flags (--help, --version, --capabilities)
│   │   ├── Route matching (in specificity order)
│   │   └── No match fallback
│   ├── private static void PrintHelp(...)
│   ├── private static void PrintVersion(...)
│   └── private static void PrintCapabilities(...)
```

## Testing Strategy

After the generator is created, verify with:
1. Build verification - solution compiles without errors
2. Manual inspection - run on sample project, check generated file
3. Unit tests (Phase 6) - test generator output for various inputs

## Notes

### Error Handling
The generator should handle edge cases gracefully:
- No `RunAsync` call found → emit nothing
- No routes defined → emit minimal interceptor with just built-in flags
- Invalid syntax → skip and emit diagnostic

### Diagnostic Reporting
Consider emitting diagnostics for:
- Multiple `RunAsync` calls (unsupported)
- Conflicting route patterns
- Invalid handler signatures

### Performance Considerations
- Use `static` lambdas to avoid allocations
- Filter early with `Where` to reduce work
- Make model types equatable for incremental caching
