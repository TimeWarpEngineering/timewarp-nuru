# Migrate unified-middleware sample to TimeWarp.Nuru native patterns

## Summary

The `samples/_unified-middleware/unified-middleware.cs` sample demonstrates unified pipeline middleware but has type conflicts between external `Mediator` package and TimeWarp.Nuru's native types.

## Current Issues

1. **Type ambiguity errors:**
   - `Mediator.Unit` vs `TimeWarp.Nuru.Unit`
   - `Mediator.IMessage` vs `TimeWarp.Nuru.IMessage`

2. **Generator error:**
   - `.Map<EchoCommand>()` without `.WithHandler()` throws `InvalidOperationException`
   - The generator expects explicit handler configuration

3. **Return type mismatch:**
   - `IRequestHandler` expects `ValueTask<Unit>` but types don't match

## Approach Options

### Option A: Use TimeWarp.Nuru Native Commands
Replace Mediator commands with TimeWarp.Nuru's nested Handler pattern:
```csharp
public sealed class EchoCommand : ICommand
{
  public string Message { get; set; } = string.Empty;
  
  public sealed class Handler : IHandler<EchoCommand>
  {
    public ValueTask Handle(EchoCommand request, CancellationToken ct) { ... }
  }
}
```

### Option B: Keep Mediator with Explicit Disambiguation
Add `using Unit = Mediator.Unit;` etc. to disambiguate types. Keep external Mediator for users who want that integration.

### Option C: Archive as Reference
Move to `samples/_archived/` as a reference for Mediator integration patterns, document the conflicts.

## Checklist

- [ ] Decide on approach (A, B, or C)
- [ ] If A: Convert EchoCommand and SlowCommand to TimeWarp.Nuru patterns
- [ ] If A: Convert LoggingBehavior and PerformanceBehavior to TimeWarp.Nuru behaviors
- [ ] Update `.Map<T>()` calls to work with generator
- [ ] Test all commands work: add, multiply, greet, echo, slow
- [ ] Rename from `_unified-middleware/` to `NN-unified-middleware/` when working

## Related

- #312 - Sample migration (parent task)
- #322 - Auto-detect ILogger<T> injection

## File

`samples/_unified-middleware/unified-middleware.cs`
