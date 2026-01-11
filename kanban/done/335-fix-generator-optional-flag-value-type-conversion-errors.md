# Fix generator optional flag value type conversion errors

## Summary

The source generator produces type conversion errors for certain optional flag value patterns. Some tests in `routing-05-option-matching.cs` fail with conversion errors when flags have optional values.

## Background

Discovered during task #332 when refactoring `routing-05-option-matching.cs` tests to use TestTerminal pattern. 28 out of 31 tests pass; 3 fail with type-related errors.

## Design Decision

After discussion, we determined that type conversion failures should emit **clear error messages** rather than silently skipping to the next route. Real CLIs (git, docker, kubectl, npm) don't use type-based route dispatch - they use explicit subcommands or flags for disambiguation.

A new analyzer task (#336) was created to detect ambiguous route patterns at compile time, preventing the need for runtime fallback behavior.

## Checklist

- [x] Run `routing-05-option-matching.cs` and identify the 3 failing tests
- [x] Analyze the generated code for failing patterns
- [x] Fix generator type conversion for optional flag values
- [x] Change from "skip route" to "emit error message" on type conversion failure
- [x] Verify all 31 tests in `routing-05-option-matching.cs` pass
- [x] Verify routing-02, routing-03, and routing-04 still pass
- [x] Update tests that expected exceptions to use new exit code + error message pattern
- [x] Create task #336 for analyzer to detect ambiguous route patterns

## Test File

`tests/timewarp-nuru-core-tests/routing/routing-05-option-matching.cs`

## Results

### Issues Fixed

**1. Type conversion throws FormatException instead of clear error**
- Changed from `int.Parse()` to `int.TryParse()` for all built-in types
- Type conversion failure now emits: `Error: Invalid value 'abc' for parameter 'ms'. Expected: int`
- Returns exit code 1 (not exception, not silent skip)
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
- `EmitSimpleMatch()`: Removed skip label (no longer needed for type conversion)
- `EmitTypeConversions()`: Changed to emit error message and return 1 on failure
- `EmitValueOptionParsing()`: Rewrote to properly handle all 4 flag/value optionality combinations
- `EmitOptionTypeConversion()`: Changed to emit error message and return 1 on failure

**`tests/timewarp-nuru-core-tests/routing/routing-02-parameter-binding.cs`**
- Updated `Should_not_bind_integer_parameter_delay_abc` to expect exit code 1 + error message
- Updated `Should_not_bind_type_mismatch_age_twenty` to expect exit code 1 + error message

### Test Results

- `routing-02-parameter-binding.cs`: 8/8 PASS
- `routing-03-optional-parameters.cs`: 9/9 PASS
- `routing-04-catch-all.cs`: 5/5 PASS
- `routing-05-option-matching.cs`: 31/31 PASS (was 28/31)

### Related Tasks

- Created task #336: Add analyzer for ambiguous route patterns with overlapping type constraints
