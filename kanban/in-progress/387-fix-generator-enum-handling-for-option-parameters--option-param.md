# Fix generator enum handling for option parameters (--option {param})

## Summary

The source generator fails to emit type conversion code for enum parameters when they are option values (e.g., `--mode {mode}` where `mode` is an enum type). This causes compile errors in generated code.

Task #372 fixed enum handling for positional parameters but did not cover option parameters.

## Problem

```csharp
.Map("deploy {env} --mode {mode}")
  .WithHandler((string env, DeploymentMode mode) => 0)
```

Generated code:
```csharp
int __handler_0(string env, global::DeploymentMode mode) => 0;
int result = __handler_0(env, mode);  // ERROR: 'mode' is never declared
```

The generator emits the handler signature correctly but doesn't generate code to:
1. Extract the option value from args
2. Convert the string to the enum type

## Related Files

- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` - needs to handle enum option params
- `tests/timewarp-nuru-tests/repl/repl-16-enum-completion.cs` - has skipped tests referencing this task
- `tests/timewarp-nuru-tests/completion/dynamic/completion-21-integration-enabledynamic.cs` - `Should_auto_register_enum_sources` test commented out, restore after fix

## Checklist

- [ ] Investigate how option parameters are extracted in route-matcher-emitter.cs
- [ ] Add enum type conversion for option parameters (similar to #372 fix for positional params)
- [ ] Unskip tests in repl-16-enum-completion.cs
- [ ] Restore `Should_auto_register_enum_sources` test in completion-21-integration-enabledynamic.cs
- [ ] Add explicit test for `--option {enumParam}` pattern
- [ ] Run full CI to verify no regressions

## Notes

Error discovered while working on #340 (shell completion migration). The completion tests use `--mode {mode}` with a `DeploymentMode` enum.

Previous fix (#372) only handled positional enum parameters, not option parameters.

---

## Implementation Plan

### Problem
The source generator fails to emit type conversion code for enum parameters when they are option values (e.g., `--mode {mode}` where `mode` is an enum type).

### Root Cause
In `EmitOptionTypeConversion()` (route-matcher-emitter.cs), after checking for custom converters, the code falls back to string without checking for enum types.

### Changes Required

#### 1. Modify `EmitOptionTypeConversion()` Method
- Add `RouteDefinition route` parameter to method signature
- Add enum type check after custom converter check (around line 1251)
- Match by option name (LongForm or ShortForm) and `IsEnumType` flag
- Call new `EmitOptionEnumTypeConversion()` method for enum types

#### 2. Create `EmitOptionEnumTypeConversion()` Method
- Similar to `EmitEnumTypeConversion()` but adapted for options
- Handle optional vs required option parameters
- Use `EnumTypeConverter<T>` pattern with option-specific error messages

#### 3. Update Call Sites
- Update call to `EmitOptionTypeConversion()` in `EmitValueOptionParsingWithIndexTracking()` to pass `route` parameter

#### 4. Test Updates
- Unskip tests in `repl-16-enum-completion.cs`
- Add new test for `--option {enumParam}` pattern
- Verify no regressions with CI tests

### Files Modified
- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`

### Reference
- Commit da3378d6 ("Fix enum parameter conversion for simple routes (#372)") shows the pattern for positional enums
