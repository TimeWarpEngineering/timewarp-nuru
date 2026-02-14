# Fix source generator parameterized service constructors - issue 172

## Description

Nuru's source generator fails to compile when a service has only a parameterized constructor (no parameterless constructor), even when the service is properly registered in `ConfigureServices`.

### Error
```
error CS7036: There is no argument given that corresponds to the required parameter 
'workspaceService' of 'KanbanService.KanbanService(IWorkspaceService)'
```

### Root Cause
The generated code in `NuruGenerated.g.cs` calls `new KanbanService()` without parameters instead of using `GetRequiredService<T>()` from the service provider.

### Expected Behavior
The source generator should:
1. Detect constructor parameters
2. Generate code that uses `GetRequiredService<T>()` from the service provider
3. Only fall back to `new()` for parameterless constructors

## Checklist

- [ ] Build a test case to reproduce the issue (service with only parameterized constructor)
- [ ] Verify the build fails with CS7036 error as expected
- [ ] Investigate source generator code that handles service instantiation
- [ ] Add logic to detect parameterized constructors
- [ ] Generate code using `GetRequiredService<T>()` for parameterized constructors
- [ ] Verify fix resolves the issue

## Notes

- Related to issue #172
- Discovered during refactoring of WorkspaceCommands (task 087 in timewarp-ganda)
- Goal is to eliminate static service classes in favor of instance-based DI services

## Implementation Plan

### Issue Summary
The source generator fails to compile when a service has only a parameterized constructor (no parameterless constructor), even when the service is properly registered in `ConfigureServices`. The error is `CS7036: There is no argument given that corresponds to the required parameter`.

### Root Cause
In `service-resolver-emitter.cs:91-107`, when generating code for a registered service, the code uses `new {ImplementationTypeName}()` without checking if the service has constructor dependencies.

The `ServiceDefinition` already has `HasConstructorDependencies` and `ConstructorDependencyTypes` that capture this information - but they're not being used.

### Proposed Fix (Option A - Auto-fallback to Runtime DI for Services with Constructor Dependencies)

**Approach**: Modify `ServiceResolverEmitter.EmitServiceResolution()` to check if a service has constructor dependencies, and if so, fall back to runtime DI for that specific service even when global `useRuntimeDI` is false.

**Implementation Steps**:
1. **Modify `ServiceResolverEmitter.EmitServiceResolution()`** (`service-resolver-emitter.cs:86-108`)
   - After finding the service (line 88), check `service.HasConstructorDependencies`
   - If true, emit `GetRequiredService<T>(GetServiceProvider{_suffix}(app))` instead of `new T()`
   - This requires checking if there's a parameterless constructor - if not, use runtime DI

2. **Create reproduction test** (`tests/timewarp-nuru-tests/generator/generator-20-parameterized-service-constructor.cs`)
   - Create a service with ONLY a parameterized constructor
   - Register it in `ConfigureServices`
   - Inject it into a route handler
   - Verify the build fails with CS7036 (current behavior)
   - After fix: Verify the code compiles and runs correctly

### Test Case Pattern
Use existing pattern from `generator-04-static-service-injection.cs`:
- Services defined at global scope for generator discovery
- Uses Jaribu test framework with `TestTerminal`
- Test file format: `#!usr/bin/dotnet --` header, then test code

### Test Structure
```csharp
public interface IMyService { string Do(); }

public class MyService : IMyService
{
  private readonly IFormatter _formatter;
  public MyService(IFormatter formatter) => _formatter = formatter;
  public string Do() => _formatter.Format("test");
}
```

### Alternative Approaches Considered
- **Option B**: Always emit error NURU051 when `HasConstructorDependencies` is true, requiring user to use `.UseMicrosoftDependencyInjection()`
- **Option C**: When ANY service has constructor dependencies, automatically enable full runtime DI

### Impact Assessment
- **Breaking changes**: None - this fixes broken behavior
- **Performance**: Minimal - adds one check per service instantiation
- **User experience**: Improves - services with parameterized constructors now work without requiring explicit `.UseMicrosoftDependencyInjection()`

## Results

[To be filled after completion]
