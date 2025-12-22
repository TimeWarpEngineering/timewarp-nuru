# Zero-cost Build() implementation

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Implement the final `Build()` method that simply wires up pre-generated structures. No parsing, no sorting, no registration - just return references to generated static data.

## Requirements

- `Build()` does minimal work (near no-op)
- Returns app instance with pre-generated data
- Only runtime work: configuration loading if needed
- Startup time should be sub-millisecond for route infrastructure

## Checklist

- [ ] Create new `GeneratedNuruApp` class (or modify existing)
- [ ] Wire `GeneratedEndpoints.All` for route matching
- [ ] Wire `GeneratedHelpText` for help display
- [ ] Wire `GeneratedCompletionScripts` for completions
- [ ] Wire `GeneratedServices` for service access
- [ ] Implement minimal `Build()` that assembles references
- [ ] Verify cold start improvement
- [ ] Profile to confirm no hidden allocations

## Notes

### Before (Runtime)

```csharp
public NuruCoreApp Build()
{
    // Merge attributed routes from registry
    foreach (var route in NuruRouteRegistry.RegisteredRoutes)
        _endpoints.Add(CreateEndpoint(route));
    
    // Sort by specificity
    _endpoints.Sort();
    
    // Build help text
    var helpText = HelpGenerator.Generate(_endpoints);
    
    // Build DI container
    var serviceProvider = _services.BuildServiceProvider();
    
    // Create app
    return new NuruCoreApp(_endpoints, helpText, serviceProvider, ...);
}
```

### After (Generated)

```csharp
public NuruCoreApp Build()
{
    // Just wire up pre-generated data
    return new NuruCoreApp(
        endpoints: GeneratedEndpoints.All,      // Pre-sorted array
        helpText: GeneratedHelpText.RootHelp,   // Pre-formatted string
        services: GeneratedServices.Instance,    // Static service holder
        // ...
    );
}
```

### Measurement

```csharp
var sw = Stopwatch.StartNew();
var app = builder.Build();
Console.WriteLine($"Build: {sw.ElapsedTicks} ticks");  // Should be < 1000
```
