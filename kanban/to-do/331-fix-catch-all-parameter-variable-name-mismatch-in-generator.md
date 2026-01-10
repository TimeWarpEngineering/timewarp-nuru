# Fix catch-all parameter variable name mismatch in generator

## Description

The source generator for catch-all parameters (`{*name}`) extracts the value to a uniquely-named variable but then calls the handler with the original parameter name, which is undefined.

## Bug Details

**Generated code (broken):**
```csharp
// Route: {*everything}
if (routeArgs.Length >= 0)
{
  string[] __everything_34 = routeArgs[0..];  // Extract to unique variable
  void __handler_34(string[] everything) => WriteLine($"Unknown command: {string.Join(" ", everything)}");
  __handler_34(everything);  // BUG: 'everything' is undefined, should be '__everything_34'
  return 0;
}
```

**Error:**
```
error CS0103: The name 'everything' does not exist in the current context
```

**Expected generated code:**
```csharp
// Route: {*everything}
if (routeArgs.Length >= 0)
{
  string[] __everything_34 = routeArgs[0..];
  void __handler_34(string[] everything) => WriteLine($"Unknown command: {string.Join(" ", everything)}");
  __handler_34(__everything_34);  // FIXED: Use the generated variable name
  return 0;
}
```

## Scope

This bug affects ALL catch-all parameters, not just the universal catch-all. Examples found in `timewarp-nuru-testapp-delegates`:
- `docker run {*args}` - calls `__handler_18(args)` instead of `__handler_18(__args_18)`
- `docker build {*args}` - same issue
- `docker ps {*args}` - same issue
- `kubectl {*args}` - same issue
- `npm {*args}` - same issue
- `{*everything}` - same issue

## Checklist

- [ ] Locate the emitter code that generates handler invocation for catch-all parameters
- [ ] Fix to use generated variable name (`__name_N`) instead of parameter name
- [ ] Add/update generator test for catch-all parameter handling
- [ ] Verify `timewarp-nuru-testapp-delegates` builds successfully
- [ ] Verify all existing generator tests pass

## Notes

- Bug discovered during Mediator dependency removal (task #330)
- The bug was previously hidden because the Mediator-based generated code path was being used
- File likely in: `source/timewarp-nuru-analyzers/generators/emitters/`
