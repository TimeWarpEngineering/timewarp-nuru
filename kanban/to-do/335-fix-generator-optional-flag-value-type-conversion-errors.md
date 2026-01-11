# Fix generator optional flag value type conversion errors

## Summary

The source generator produces type conversion errors for certain optional flag value patterns. Some tests in `routing-05-option-matching.cs` fail with conversion errors when flags have optional values.

## Background

Discovered during task #332 when refactoring `routing-05-option-matching.cs` tests to use TestTerminal pattern. 28 out of 31 tests pass; 3 fail with type-related errors.

## Checklist

- [ ] Run `routing-05-option-matching.cs` and identify the 3 failing tests
- [ ] Analyze the generated code for failing patterns
- [ ] Fix generator type conversion for optional flag values
- [ ] Verify all 31 tests in `routing-05-option-matching.cs` pass
- [ ] Add/update unit tests for optional flag value generation

## Test File

`tests/timewarp-nuru-core-tests/routing/routing-05-option-matching.cs`

## Notes

- Related to V2 Generator epic (#265)
- May involve nullable type handling or default value generation
- The 28 passing tests confirm the basic option matching works; issue is specific to certain patterns
