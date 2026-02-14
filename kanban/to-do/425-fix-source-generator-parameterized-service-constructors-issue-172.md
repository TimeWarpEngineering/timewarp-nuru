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

## Results

[To be filled after completion]
