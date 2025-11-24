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

- [ ] Create failing test in Tests/TimeWarp.Nuru.Repl.Tests demonstrating the bug
- [ ] Update GetParameterCompletions signature to accept partialWord parameter
- [ ] Filter enum completions by partial word prefix (case-insensitive)
- [ ] Update call sites in GetCompletionsAfterCommand and GetCompletionsForRoute
- [ ] Verify test passes after fix
- [ ] Verify manual testing works: `deploy p<tab>` completes to `deploy prod`

## Notes

**Files to modify:**
- Tests/TimeWarp.Nuru.Repl.Tests/ (new test file)
- Source/TimeWarp.Nuru.Completion/Completion/CompletionProvider.cs
