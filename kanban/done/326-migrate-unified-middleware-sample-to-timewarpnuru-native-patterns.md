# Migrate unified-middleware sample to TimeWarp.Nuru native patterns

## Status: COMPLETE ✅

## Summary

Migrated the unified-middleware sample from external `Mediator` package to TimeWarp.Nuru native patterns. The sample now demonstrates that delegate routes and attributed routes share the same `INuruBehavior` pipeline.

## Approach Taken: Option A

Converted to TimeWarp.Nuru native patterns:
- Removed external `Mediator.Abstractions` and `Mediator.SourceGenerator` packages
- Converted `IPipelineBehavior<TMessage, TResponse>` to `INuruBehavior`
- Converted Mediator commands to `[NuruRoute]` attributed commands with nested Handler
- Used `Console.WriteLine` instead of `ILogger<T>` (ILogger support is #322)

## Checklist

- [x] Decide on approach (A, B, or C) → **Option A**
- [x] Convert EchoCommand and SlowCommand to TimeWarp.Nuru patterns
- [x] Convert LoggingBehavior and PerformanceBehavior to INuruBehavior
- [x] Remove external Mediator package dependencies
- [x] Test all commands work: add, multiply, greet, echo, slow
- [x] Rename from `_unified-middleware/` to `11-unified-middleware/`

## Generator Bug Fixed

During migration, discovered that attributed routes with behaviors failed because:
- `BehaviorContext` was created with `Command = __command`
- But `__command` was created INSIDE the behavior pipeline (too late)

**Fix:**
- Updated `behavior-emitter.cs` to create command BEFORE context for attributed routes
- Added `commandAlreadyCreated` parameter to `handler-invoker-emitter.cs` to skip duplicate creation
- Updated `route-matcher-emitter.cs` to pass the flag when behaviors are present

## Test Results

| Command | Type | Result |
|---------|------|--------|
| `add 5 3` | Delegate | ✅ Pipeline wraps correctly |
| `multiply 4 7` | Delegate | ✅ Pipeline wraps correctly |
| `greet World` | Delegate | ✅ Pipeline wraps correctly |
| `echo "hello"` | Attributed | ✅ Pipeline wraps correctly |
| `slow 600` | Attributed | ✅ Shows SLOW! warning |

## Files Modified

- `samples/11-unified-middleware/unified-middleware.cs` - Complete rewrite
- `source/timewarp-nuru-analyzers/generators/emitters/behavior-emitter.cs` - Command creation for attributed routes
- `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs` - Skip duplicate command creation
- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` - Pass commandAlreadyCreated flag

## Related

- #312 - Sample migration (parent task)
- #322 - Auto-detect ILogger<T> injection (future enhancement)
- #306 - Suppress Unit result output (minor issue - Unit prints to console)

## File

`samples/11-unified-middleware/unified-middleware.cs`
