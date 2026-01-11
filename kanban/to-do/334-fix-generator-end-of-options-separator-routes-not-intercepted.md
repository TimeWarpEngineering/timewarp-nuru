# Fix generator end-of-options separator routes not intercepted

## Summary

Routes containing the end-of-options separator (`--`) are not being intercepted by the generated code. The `RunAsync` call falls through to the non-intercepted path, causing "RunAsync was not intercepted" errors.

## Background

Discovered during task #332 when refactoring `routing-08-end-of-options.cs` tests to use TestTerminal pattern.

**Route pattern:** `git checkout -- {file}`
**Input:** `git checkout -- README.md`
**Expected:** Route matches and handler executes
**Actual:** "RunAsync was not intercepted" - generator doesn't recognize the route

## Checklist

- [ ] Investigate why `--` in route patterns prevents interception
- [ ] Check if DSL parser handles `--` correctly
- [ ] Fix generator to properly match routes with end-of-options separator
- [ ] Verify `routing-08-end-of-options.cs` tests pass
- [ ] Add/update unit tests for end-of-options handling

## Test File

`tests/timewarp-nuru-core-tests/routing/routing-08-end-of-options.cs`

## Notes

- The `--` separator is a POSIX convention meaning "end of options, everything after is a positional argument"
- Common usage: `git checkout -- file.txt` (the `--` prevents `file.txt` from being interpreted as a branch name)
- Related to V2 Generator epic (#265)
