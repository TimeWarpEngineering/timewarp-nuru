# V2 Generator Phase 5: Generator Entry Point

## Description

Create the main incremental source generator that wires together locators, extractors, and emitters to produce the `RunAsync` interceptor.

## Parent

#265 Epic: V2 Source Generator Implementation

## Checklist

### Commit 5.1: Create NuruGenerator
- [ ] Create `generators/nuru-generator.cs`
- [ ] Implement `IIncrementalGenerator.Initialize`
- [ ] Wire up `RunAsyncLocator` as syntax provider
- [ ] Wire up attributed route detection
- [ ] Combine fluent and attributed routes
- [ ] Call `InterceptorEmitter` to produce output
- [ ] Register source output
- [ ] Verify build succeeds

## Notes

### Generator Structure
```csharp
namespace TimeWarp.Nuru.Generators;

[Generator]
public sealed class NuruGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Locate RunAsync call sites
        var runAsyncCalls = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: RunAsyncLocator.IsPotentialMatch,
                transform: RunAsyncLocator.Extract);
        
        // 2. Locate attributed routes
        var attributedRoutes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TimeWarp.Nuru.NuruRouteAttribute",
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: AttributedRouteExtractor.Extract);
        
        // 3. Combine and extract full AppModel
        var appModel = runAsyncCalls
            .Combine(attributedRoutes.Collect())
            .Select((data, ct) => AppExtractor.Extract(data, ct));
        
        // 4. Emit generated code
        context.RegisterSourceOutput(appModel, (ctx, model) =>
        {
            if (model is null) return;
            var source = InterceptorEmitter.Emit(model);
            ctx.AddSource("NuruGenerated.g.cs", source);
        });
    }
}
```

### Incremental Generator Best Practices
- Use `CreateSyntaxProvider` for efficient filtering
- Avoid allocations in predicate functions
- Make extracted data equatable for caching
- Handle null/empty cases gracefully

### UseNewGen Flag
Consider: Should this generator check the `UseNewGen` MSBuild property?
- If yes, only run when enabled
- If no, always run (simpler during development)
