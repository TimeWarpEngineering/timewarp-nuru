# Suppress Unit result output in generated code

## Summary

When a command handler returns `Unit`, the generated code prints "TimeWarp.Nuru.Unit" to the terminal. This should be suppressed since `Unit` represents "no result".

## Problem

Current generated code for command handlers:
```csharp
global::TimeWarp.Nuru.Unit result = await __handler.Handle(__command, cancellationToken);
app.Terminal.WriteLine(result.ToString());  // Prints "TimeWarp.Nuru.Unit"
```

## Expected Behavior

For `Unit` return type, don't print anything:
```csharp
await __handler.Handle(__command, cancellationToken);
// No output for Unit
```

Or conditionally skip:
```csharp
global::TimeWarp.Nuru.Unit result = await __handler.Handle(__command, cancellationToken);
// Skip WriteLine for Unit type
```

## Checklist

- [ ] Update `EmitCommandInvocation` in `handler-invoker-emitter.cs` to check if return type is `Unit`
- [ ] Skip `EmitResultOutput` when return type is `Unit` or `TimeWarp.Nuru.Unit`
- [ ] Verify with `02-calc-commands.cs` sample - should no longer print "TimeWarp.Nuru.Unit"

## Notes

Discovered during task #304 while testing attributed routes with calculator sample.

The fix should be in `EmitCommandInvocation()` - check if `handler.ReturnType.UnwrappedTypeName` is `Unit` and skip the result output.
