# Fix generator optional flag value type conversion errors

## Summary

The source generator produces type conversion errors for certain optional flag value patterns. Some tests in `routing-05-option-matching.cs` fail with conversion errors when flags have optional values.

## Background

Discovered during task #332 when refactoring `routing-05-option-matching.cs` tests to use TestTerminal pattern. 28 out of 31 tests pass; 3 fail with type-related errors.

## Checklist

- [x] Run `routing-05-option-matching.cs` and identify the 3 failing tests
- [x] Analyze the generated code for failing patterns
- [x] Fix generator type conversion for optional flag values
- [x] Verify all 31 tests in `routing-05-option-matching.cs` pass
- [x] Verify routing-03 and routing-04 still pass

## Test File

`tests/timewarp-nuru-core-tests/routing/routing-05-option-matching.cs`

## Results

### Issues Fixed

**1. Type conversion throws FormatException instead of graceful failure**
- Changed from `int.Parse()` to `int.TryParse()` for all built-in types
- Route now skips on conversion failure instead of throwing exception
- Test: `Should_not_match_typed_option_server_port_abc` now passes

**2. Required flag + optional value not handling missing value**
- Pattern `--config {mode?}` with input `["build", "--config"]` wasn't matching
- Fixed option parsing to properly detect flag at end of args without value
- Test: `Should_match_required_flag_optional_value_build_config_no_value` now passes

**3. Optional flag + required value not rejecting missing value**
- Pattern `--config? {mode}` with input `["build", "--config"]` was incorrectly matching
- Fixed option parsing to require value when parameter is not optional
- Test: `Should_not_match_optional_flag_required_value_build_config_no_value` now passes

**4. Optional typed positional parameters not working (bonus fix)**
- Pattern `list {count:int?}` with input `["list"]` wasn't matching
- Fixed to declare nullable type and properly handle null case
- Test: `Should_match_optional_integer_list_null` now passes

### Files Modified

**`source/timewarp-nuru-analyzers/generators/emitters/type-conversion-map.cs`**
- Added `GetBuiltInTryConversion()` method returning TryParse expressions
- Marked old `GetBuiltInConversion()` as obsolete
- Updated `IsBuiltInType()` to use `GetClrTypeName()` instead

**`source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`**
- `EmitSimpleMatch()`: Added skip label for routes with typed parameters
- `EmitTypeConversions()`: Changed to use TryParse with route skip on failure
- `EmitValueOptionParsing()`: Rewrote to properly handle all 4 flag/value optionality combinations
- `EmitOptionTypeConversion()`: Changed to use TryParse with route skip on failure

### Test Results

- `routing-03-optional-parameters.cs`: 9/9 PASS
- `routing-04-catch-all.cs`: 5/5 PASS
- `routing-05-option-matching.cs`: 31/31 PASS (was 28/31)
