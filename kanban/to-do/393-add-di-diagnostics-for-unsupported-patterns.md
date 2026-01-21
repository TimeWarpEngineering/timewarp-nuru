# Add DI Diagnostics for Unsupported Patterns

## Parent

Epic #391: Full DI Support - Source-Gen and Runtime Options

## Description

Add compile-time diagnostics that clearly report when source-gen DI cannot handle a registration pattern. Guide users toward either fixing the pattern or opting into runtime DI.

**Key principle:** When `UseMicrosoftDependencyInjection = false`, validate registrations and report actionable errors. When `true`, skip validation (runtime DI handles everything).

## Requirements

### New Diagnostics

| Code | Severity | When | Message |
|------|----------|------|---------|
| NURU050 | Error | Handler requires unregistered service | "Handler requires service '{0}' but it is not registered in ConfigureServices" |
| NURU051 | Error | Service has constructor dependencies | "Service '{0}' has constructor dependencies. Use .UseMicrosoftDependencyInjection() or register dependencies" |
| NURU052 | Warning | Extension method call detected | "Cannot analyze registrations inside '{0}()'. Registrations may not be visible to source-gen DI" |
| NURU053 | Error | Factory delegate registration | "Service '{0}' uses factory delegate. Use .UseMicrosoftDependencyInjection() for factory support" |
| NURU054 | Error | Internal type not accessible | "Cannot instantiate internal type '{0}'. Use .UseMicrosoftDependencyInjection() or expose public type" |

### Implementation

- [ ] Create `ServiceValidator` class in analyzers
- [ ] Add diagnostic descriptors to `Descriptors.cs`
- [ ] Validate handler service requirements against registered services
- [ ] Detect constructor dependencies on registered services
- [ ] Detect extension method calls (methods not named AddTransient/AddScoped/AddSingleton)
- [ ] Skip all validation when `UseMicrosoftDependencyInjection = true`

### Validation Logic

```csharp
internal static class ServiceValidator
{
    public static ImmutableArray<Diagnostic> Validate(
        AppModel app,
        ImmutableArray<ServiceDefinition> services,
        ImmutableArray<HandlerDefinition> handlers)
    {
        // Runtime DI handles everything - no validation needed
        if (app.UseMicrosoftDependencyInjection)
            return [];

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        // Check each handler's service requirements
        foreach (var handler in handlers)
        {
            foreach (var param in handler.ServiceParameters)
            {
                ValidateServiceParameter(param, services, diagnostics);
            }
        }

        // Check registered services for constructor dependencies
        foreach (var service in services)
        {
            ValidateServiceConstructor(service, services, diagnostics);
        }

        return diagnostics.ToImmutable();
    }
}
```

### User Experience

**Before (current - silent failure or cryptic error):**
```csharp
services.AddSingleton<IRepo, SqlRepo>();  // SqlRepo needs IDbConnection
// Compiles, but generated code fails: CS7036 missing required arguments
```

**After (clear guidance):**
```csharp
services.AddSingleton<IRepo, SqlRepo>();
// error NURU051: Service 'SqlRepo' has constructor dependencies (IDbConnection, ILogger<SqlRepo>).
//                Options:
//                  1. Register dependencies: services.AddSingleton<IDbConnection, ...>()
//                  2. Use runtime DI: .UseMicrosoftDependencyInjection()
```

## Notes

- This is Phase 2 of Epic #391
- Diagnostics should be actionable - tell users HOW to fix
- Include "Use .UseMicrosoftDependencyInjection()" as escape hatch in all messages
- Phase 3 will reduce NURU051 errors by supporting constructor dependencies
- Phase 4 will reduce NURU052 warnings by analyzing extension methods
