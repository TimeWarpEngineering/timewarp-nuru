# Fix catch-all parameter variable name mismatch in handler invocation

## Description

Pre-existing bug: When a route has a catch-all parameter, the generated code uses different variable names for declaration and invocation, causing a compilation error.

### Example

Route pattern: `{*everything}`

Generated code:
```csharp
string[] __everything_34 = routeArgs[0..];                       // Declaration uses __everything_34
void __handler_34(string[] everything) => WriteLine(...);
__handler_34(everything);                                        // Invocation uses "everything" - ERROR!
```

The variable is declared as `__everything_34` (with index suffix to avoid collision with 'args') but the handler is invoked with just `everything` (the original parameter name).

### Root Cause

In `handler-invoker-emitter.cs`, the `BuildArgumentListFromRoute` method uses `routeParam.CamelCaseName` (line 692) for the argument name, but `route-matcher-emitter.cs` `EmitParameterExtraction` uses `__{param.CamelCaseName}_{routeIndex}` (line 390) for the variable declaration.

### Affected Files

- `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs` - `BuildArgumentListFromRoute` method (line 672+)
- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` - `EmitParameterExtraction` method (line 382+)
- `tests/test-apps/timewarp-nuru-testapp-delegates` - Contains catch-all routes that trigger this bug

## Checklist

- [ ] Update `BuildArgumentListFromRoute` to use the same naming pattern as `EmitParameterExtraction`
- [ ] For catch-all parameters, use `__{name}_{routeIndex}` instead of just `{name}`
- [ ] Test with `timewarp-nuru-testapp-delegates` which has catch-all routes
- [ ] Verify solution build succeeds

## Notes

- This is a pre-existing bug, not introduced by task #319 changes
- Discovered while implementing per-app route isolation
- The fix should be straightforward - just align the naming conventions between the two emitter methods
- The routeIndex needs to be passed through or calculated consistently
