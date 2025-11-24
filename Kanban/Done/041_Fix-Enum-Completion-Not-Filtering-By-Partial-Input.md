# Fix Enum Completion Not Filtering By Partial Input

## Description

When using tab completion in REPL mode, enum parameter completions show all values but don't filter when the user types a partial value.

**Steps to reproduce:**
1. Run `repl-basic-demo.cs`
2. Type `d<tab>` → correctly completes to `deploy `
3. Type `<tab>` → correctly shows enum values: `dev`, `staging`, `prod`
4. Type `p<tab>` → BUG: Should complete to `prod` but doesn't filter

**Root Cause:**
In `Source/TimeWarp.Nuru.Completion/Completion/CompletionProvider.cs`, the `GetParameterCompletions` method (line 380) doesn't receive the partial word being typed, so it returns all enum values without filtering by prefix.

## Requirements

- Create a test in Tests/TimeWarp.Nuru.Repl.Tests that duplicates the issue (test should fail initially)
- Fix the bug by passing partialWord to GetParameterCompletions and filtering enum values
- Test should pass after fix

## Checklist

- [x] Create failing test in Tests/TimeWarp.Nuru.Completion.Tests/Dynamic demonstrating the bug
- [x] Update GetParameterCompletions signature to accept partialWord parameter
- [x] Filter enum completions by partial word prefix (case-insensitive)
- [x] Update call sites in GetCompletionsForRoute to pass partial word
- [x] Fix parameter matching loop to not consume partial word being completed
- [x] Fix partial word retrieval to use argIndex instead of cursorPosition
- [x] Verify test passes after fix (5/5 tests pass)
- [ ] Verify manual testing works: `deploy p<tab>` completes to `deploy prod`

## Implementation Notes

**Test file created:**
- Tests/TimeWarp.Nuru.Completion.Tests/Dynamic/completion-26-enum-partial-filtering.cs

**Changes to CompletionProvider.cs:**
1. Added `partialWord` parameter to `GetParameterCompletions` method
2. Filter enum values by `enumName.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase)`
3. In ParameterMatcher handling loop: break early when at last arg before cursor
4. Fixed `partial` computation to use `args[argIndex]` instead of `args[cursorPosition]`
