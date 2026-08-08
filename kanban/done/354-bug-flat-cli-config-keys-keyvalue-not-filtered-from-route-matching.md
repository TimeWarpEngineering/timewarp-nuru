# BUG: Flat CLI config keys (--Key=value) not filtered from route matching

## Description

Hierarchical CLI configuration keys with colon separator (`--Section:Key=value`) are correctly filtered from route matching before args are processed. However, flat keys without a colon (`--Key=value`) are NOT filtered, causing route matching to fail when these args are present.

## Root Cause

`source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:454`

The filter only checked for `:` (colon), not `=` (equals):
```csharp
// OLD: Only filtered --Section:Key=value (required colon)
arg.StartsWith("--") && arg.Contains(':')
```

## Fix

Updated `EmitConfigArgFiltering()` to filter all .NET CLI config formats:
- `--Key=value` (flat with equals)
- `--Section:Key=value` (hierarchical with colon)
- `/Key=value` (forward slash with equals)
- `/Section:Key=value` (forward slash with colon)

New logic checks for `=` OR `:` after the prefix:
```csharp
static bool IsConfigArg(string arg)
{
  if (arg.StartsWith("--"))
    return arg.IndexOf('=') > 2 || arg.IndexOf(':') > 2;
  if (arg.StartsWith("/") && char.IsLetter(arg[1]))
    return arg.IndexOf('=') > 1 || arg.IndexOf(':') > 1;
  return false;
}
```

## Checklist

- [x] Update route argument filtering to also filter flat CLI config keys
- [x] Verify `routing-12-colon-filtering.cs` tests still pass
- [x] Fix failing test `Should_override_flat_key_from_cli()`
- [x] Add tests for `/Key=value` and `/Section:Key=value` formats
- [x] Add test to verify space-separated options are NOT filtered

## Test Results

`tests/timewarp-nuru-core-tests/configuration/configuration-02-cli-overrides.cs`:
- 7 tests, all passing
- Tests flat keys, hierarchical keys, forward slash format, space-separated options

CI: 482 tests, all passing

## Files Modified

- `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` - Updated filter logic
- `tests/timewarp-nuru-core-tests/configuration/configuration-02-cli-overrides.cs` - Added 3 new tests
