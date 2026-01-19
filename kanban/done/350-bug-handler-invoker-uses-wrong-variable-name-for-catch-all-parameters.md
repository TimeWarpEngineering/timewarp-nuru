# Bug: Handler invoker uses wrong variable name for catch-all parameters

## Description

When the source generator invokes a handler with catch-all parameters, it generates the wrong argument list. The problem is more complex than just catch-all naming.

## Reproduction

**File:** `tests/timewarp-nuru-core-tests/routing/routing-09-complex-integration.cs` line 22

**Pattern:**
```csharp
.Map("docker run -i -t --env {e}* -- {*cmd}")
  .WithHandler((bool i, bool t, string[] e, string[] cmd) => ...)
```

**Generated code (broken):**
```csharp
// Route matcher creates these variables:
string[] __cmd_119 = routeArgs[3..];
bool i = Array.Exists(routeArgs, a => a == "-i");
bool t = Array.Exists(routeArgs, a => a == "-t");
string? e = null;  // from --env option

// Handler definition:
string __handler_119(bool i, bool t, string[] e, string[] cmd) => $"...";

// Handler invocation - WRONG ORDER AND NAMES:
string result = __handler_119(__cmd_119, t, e, cmd);
//                            ^^^^^^^^^ should be: i
//                                         ^ should be: __cmd_119
```

**Errors:**
```
error CS0103: The name 'cmd' does not exist in the current context
```

## Analysis

### What route-matcher-emitter creates:
- `__cmd_119` (string[]) - catch-all parameter with route-indexed name
- `i` (bool) - short-only flag `-i`
- `t` (bool) - short-only flag `-t`
- `e` (string?) - option value from `--env`

### What BuildArgumentListFromRoute should produce:
Arguments: `i, t, e, __cmd_119`

### What it actually produces:
Arguments: `__cmd_119, t, e, cmd`

### Root Cause Investigation

The issue is in `BuildArgumentListFromRoute` in `handler-invoker-emitter.cs` (lines 676-747).

The method iterates through `handler.Parameters` and for each parameter checks `param.Source` (BindingSource):
- `Parameter` or `CatchAll`: matches by position to `routeParams`, uses `routeParam.IsCatchAll` to decide naming
- `Option` or `Flag`: looks up in `routeOptions` by ParameterName/LongForm/ShortForm

**Problem 1**: Short-only flags (`-i`, `-t`) weren't matched because the lookup didn't check `ShortForm`.
- **Partial fix applied**: Added `o.ShortForm?.Equals(...)` to the lookup (line 721)

**Problem 2**: When matching fails, it falls back to `param.ParameterName` which doesn't exist in generated scope.

**Problem 3**: The first argument is `__cmd_119` when it should be `i`. This suggests either:
- Handler param `i` has wrong `BindingSource` (maybe CatchAll instead of Flag?)
- Or the positional matching for CatchAll is happening too early

### Key Code Locations

1. **handler-invoker-emitter.cs:676-747** - `BuildArgumentListFromRoute` method
2. **handler-invoker-emitter.cs:718-731** - Option/Flag matching logic (partial fix applied)
3. **pattern-string-extractor.cs:232-247** - How flags are classified as `BindingSource.Flag`
4. **route-matcher-emitter.cs:406-410** - How catch-all variables are named

### Next Steps

1. Debug/trace what `BindingSource` values the handler parameters actually have
2. Check if `-i` and `-t` are correctly identified as `BindingSource.Flag`
3. Verify `cmd` is correctly identified as `BindingSource.CatchAll`
4. Fix the argument list building to:
   - Correctly match short-only flags by ShortForm (partial fix done)
   - Use correct variable names for all parameter types

## Partial Fix Applied

Added ShortForm to option matching in handler-invoker-emitter.cs:718-721:
```csharp
OptionDefinition? matchingOption = routeOptions.FirstOrDefault(o =>
  o.ParameterName?.Equals(param.ParameterName, StringComparison.OrdinalIgnoreCase) == true ||
  o.LongForm?.Equals(param.ParameterName, StringComparison.OrdinalIgnoreCase) == true ||
  o.ShortForm?.Equals(param.ParameterName, StringComparison.OrdinalIgnoreCase) == true ||  // ADDED
  ToCamelCase(o.LongForm ?? "").Equals(param.ParameterName, StringComparison.OrdinalIgnoreCase));
```

**This fix alone didn't resolve the issue** - the generated code still has wrong argument order.

## Checklist

- [x] Add ShortForm to option matching lookup (partial fix)
- [ ] Investigate why argument order is wrong (first arg is `__cmd_119` instead of `i`)
- [ ] Check BindingSource values for handler parameters
- [ ] Fix argument list building to produce correct order and names
- [ ] Clear caches and verify fix: `ganda runfile cache --clear`
- [ ] Verify routing-09-complex-integration.cs compiles and passes

## Files

- `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/extractors/pattern-string-extractor.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
