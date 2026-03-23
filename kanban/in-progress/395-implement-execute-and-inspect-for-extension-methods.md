# Implement Execute and Inspect for Extension Methods

## Parent

Epic #391: Full DI Support - Source-Gen and Runtime Options

## Description

Execute the `ConfigureServices` registrations at compile time using a real `ServiceCollection` instance. This allows the generator to "see" what services are registered by extension methods in external assemblies, then generate static instantiation code for them.

**Key insight:** Instead of statically analyzing what extension methods register (impossible across assemblies), just RUN them and observe the results.

## Current Problem

```csharp
services.AddMyLibraryServices();  // Generator can't see inside this!
```

The generator only sees the method call, not what `AddMyLibraryServices()` actually registers.

## Solution: Execute and Inspect

```csharp
// In source generator:
var services = new ServiceCollection();

// Actually execute the extension method!
services.AddMyLibraryServices();

// Now inspect what was registered
foreach (ServiceDescriptor descriptor in services)
{
    // descriptor.ServiceType       → typeof(IFoo)
    // descriptor.ImplementationType → typeof(FooImpl)
    // descriptor.Lifetime          → Singleton
}

// Generate static code based on actual registrations
```

## Requirements

### Compile-Time Execution
- [ ] Create `ServiceCollectionExecutor` class
- [ ] Parse `ConfigureServices` lambda body
- [ ] For each method invocation:
  - Map Roslyn `IMethodSymbol` to runtime `MethodInfo` via reflection
  - Handle generic method instantiation (`MakeGenericMethod`)
  - Invoke method on real `ServiceCollection`
- [ ] Handle assembly loading for referenced assemblies

### Registration Inspection
- [ ] Enumerate resulting `ServiceDescriptor` collection
- [ ] For each descriptor:
  - Extract `ServiceType`, `ImplementationType`, `Lifetime`
  - Handle `ImplementationFactory` (flag as requiring runtime DI)
  - Analyze implementation constructor (reuse Phase 3 logic)

### Type Mapping
- [ ] Create `TypeMapper` to convert Roslyn `ITypeSymbol` ↔ `System.Type`
- [ ] Load assemblies from compilation references
- [ ] Cache loaded types for performance

### Code Generation
- [ ] Generate static instantiation code for all discovered services
- [ ] Combine with Phase 3 constructor resolution
- [ ] Report diagnostic if factory delegate detected (still requires runtime DI)
- [ ] Report diagnostic if internal type detected (still requires runtime DI)

### Error Handling
- [ ] Handle assembly load failures gracefully
- [ ] Handle method invocation failures (report diagnostic)
- [ ] Handle reflection exceptions

## Implementation Sketch

```csharp
internal static class ServiceCollectionExecutor
{
    public static ImmutableArray<ServiceDefinition> Execute(
        LambdaExpressionSyntax configureServicesLambda,
        SemanticModel semanticModel,
        Compilation compilation)
    {
        // Create real ServiceCollection
        var services = new ServiceCollection();

        // Execute each statement in the lambda body
        foreach (var statement in GetStatements(configureServicesLambda))
        {
            if (statement is InvocationExpressionSyntax invocation)
            {
                ExecuteInvocation(services, invocation, semanticModel, compilation);
            }
        }

        // Convert ServiceDescriptors to ServiceDefinitions
        return services.Select(ToServiceDefinition).ToImmutableArray();
    }

    private static void ExecuteInvocation(
        IServiceCollection services,
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        Compilation compilation)
    {
        var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (methodSymbol is null) return;

        // Get runtime MethodInfo
        var containingType = GetRuntimeType(methodSymbol.ContainingType, compilation);
        var methodInfo = GetMethodInfo(containingType, methodSymbol);

        // Handle generic methods
        if (methodSymbol.IsGenericMethod)
        {
            var typeArgs = methodSymbol.TypeArguments
                .Select(t => GetRuntimeType(t, compilation))
                .ToArray();
            methodInfo = methodInfo.MakeGenericMethod(typeArgs);
        }

        // Execute!
        var args = BuildArguments(invocation, services, semanticModel, compilation);
        methodInfo.Invoke(null, args);
    }

    private static Type GetRuntimeType(ITypeSymbol symbol, Compilation compilation)
    {
        var fullName = symbol.ToDisplayString();

        // Try loaded assemblies
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = asm.GetType(fullName);
            if (type != null) return type;
        }

        // Load from compilation reference
        var reference = compilation.References
            .OfType<PortableExecutableReference>()
            .FirstOrDefault(r => ContainsType(r, symbol));

        if (reference != null)
        {
            var assembly = Assembly.LoadFrom(reference.FilePath);
            return assembly.GetType(fullName);
        }

        throw new InvalidOperationException($"Cannot load type: {fullName}");
    }
}
```

## Limitations

Even with Execute and Inspect, some patterns still require runtime DI:

| Pattern | Can Generate Static? | Reason |
|---------|---------------------|--------|
| `AddSingleton<IFoo, Foo>()` | ✅ Yes | Type visible |
| `AddMyLibraryServices()` | ✅ Yes | Execute reveals registrations |
| `AddSingleton<IFoo>(sp => ...)` | ❌ No | Factory needs ServiceProvider |
| `AddSingleton<IInternal, InternalImpl>()` | ❌ No | Can't instantiate internal |

## Testing

- [ ] Simple extension method with AddSingleton
- [ ] Extension method registering multiple services
- [ ] Nested extension methods (A calls B calls C)
- [ ] Mix of inline and extension method registrations
- [ ] Extension method with generic type arguments
- [ ] Factory delegate detection and diagnostic
- [ ] Internal type detection and diagnostic
- [ ] Assembly loading edge cases

## Notes

- This is Phase 4 of Epic #391
- Most ambitious phase - executes code at compile time
- Registration is side-effect free (just adds to list)
- Does NOT instantiate services at compile time (only registration)
- Combined with Phase 3, covers most real-world DI patterns
- Factory delegates and internal types still require `UseMicrosoftDependencyInjection()`
