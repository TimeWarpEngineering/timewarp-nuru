# Fix generator repeated options passing single value instead of array

## Summary

The source generator incorrectly handles repeated options (e.g., `{e}*`). When an option is marked as repeatable with `*`, the handler should receive an array of all values, but the generator passes only a single value.

## Background

Discovered during task #332 when refactoring `routing-06-repeated-options.cs` tests to use TestTerminal pattern.

**Route pattern:** `docker run {e}*`
**Input:** `docker run -e FOO=bar -e BAZ=qux`
**Expected:** Handler receives `string[] e = ["FOO=bar", "BAZ=qux"]`
**Actual:** Handler receives single value instead of array

## Checklist

- [ ] Investigate how the generator processes `*` (repeatable) modifiers
- [ ] Fix generator to collect all repeated option values into an array
- [ ] Verify `routing-06-repeated-options.cs` tests pass
- [ ] Add/update unit tests for repeated options code generation

## Test File

`tests/timewarp-nuru-core-tests/routing/routing-06-repeated-options.cs`

## Notes

- Related to V2 Generator epic (#265)
- The DSL correctly parses the `*` modifier, but the generator doesn't handle it properly
- May need to look at how parameter collection works in the interceptor generation
