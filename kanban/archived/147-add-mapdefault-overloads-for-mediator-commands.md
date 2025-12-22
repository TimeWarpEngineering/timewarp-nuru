# Add MapDefault overloads for Mediator commands

## Description

`MapDefault` currently only supports delegate handlers. Add overloads to support Mediator commands for consistency with `Map` method signatures.

**Current state:**
- `Map(string pattern, Delegate handler, ...)` has `MapDefault(Delegate handler, ...)`
- `Map<TCommand>(string pattern, ...)` has **no** `MapDefault<TCommand>(...)`
- `Map<TCommand, TResponse>(string pattern, ...)` has **no** `MapDefault<TCommand, TResponse>(...)`

**Desired state:**
Add two new overloads:
- `MapDefault<TCommand>(string? description = null)`
- `MapDefault<TCommand, TResponse>(string? description = null)`

## Checklist

- [ ] Add `MapDefault<TCommand>()` to `NuruCoreAppBuilder` in `nuru-core-app-builder.routes.cs`
- [ ] Add `MapDefault<TCommand, TResponse>()` to `NuruCoreAppBuilder`
- [ ] Add covariant overrides in `NuruAppBuilder` (`nuru-app-builder.overrides.cs`)
- [ ] Update `NuruInvokerGenerator` if needed (verify it handles `MapDefault<T>` calls)
- [ ] Add tests in `routing-22-async-task-int-return.cs` or new test file
- [ ] Update documentation in `documentation/user/features/routing.md`

## Notes

Implementation should follow existing `Map<TCommand>` pattern:

```csharp
public virtual NuruCoreAppBuilder MapDefault<TCommand>(string? description = null)
  where TCommand : IRequest, new()
{
  return MapMediator(typeof(TCommand), string.Empty, description);
}

public virtual NuruCoreAppBuilder MapDefault<TCommand, TResponse>(string? description = null)
  where TCommand : IRequest<TResponse>, new()
{
  return MapMediator(typeof(TCommand), string.Empty, description);
}
```

The key difference from delegate `MapDefault` is passing `string.Empty` as the pattern to `MapMediator`, just like the delegate version passes `string.Empty` to `MapInternal`.

## Archived

**Reason:** Obsolete - `MapDefault` is being removed entirely to simplify source generation. Use `Map("")` instead for default routes. This applies to both delegate and Mediator command handlers.
