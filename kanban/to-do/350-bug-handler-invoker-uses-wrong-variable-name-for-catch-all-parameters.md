# Bug: Handler invoker uses wrong variable name for catch-all parameters

## Description

When the source generator invokes a handler with catch-all parameters, it uses the wrong variable name. The route-matcher-emitter creates catch-all variables with the format `__cmd_119` (with route index suffix), but the handler-invoker-emitter passes just `cmd`.

## Reproduction

**File:** `tests/timewarp-nuru-core-tests/routing/routing-09-complex-integration.cs` line 22

**Pattern:**
```csharp
.Map("docker run -i -t --env {e}* -- {*cmd}")
  .WithHandler((bool i, bool t, string[] e, string[] cmd) => ...)
```

**Generated code (broken):**
```csharp
// route-matcher-emitter creates: __cmd_119
string[] __cmd_119 = routeArgs[__startIndex..].ToArray();

// handler-invoker-emitter passes: cmd (WRONG!)
string result = __handler_119(__cmd_119, t, e, cmd);  // cmd doesn't exist!
```

**Error:**
```
error CS0103: The name 'cmd' does not exist in the current context
```

## Expected Behavior

The handler invoker should use `__cmd_119` (the actual variable name created by route-matcher-emitter):
```csharp
string result = __handler_119(i, t, e, __cmd_119);
```

## Root Cause

In `handler-invoker-emitter.cs`, the `BuildArgumentListFromRoute` method needs to use the route-indexed variable name for catch-all parameters, matching what `route-matcher-emitter.cs` generates.

## Checklist

- [ ] Fix `BuildArgumentListFromRoute` in handler-invoker-emitter.cs to use `__{name}_{routeIndex}` for catch-all params
- [ ] Verify routing-09-complex-integration.cs compiles and passes

## Files to Investigate

- `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs` - `BuildArgumentListFromRoute` method
- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` - see how catch-all variables are named
