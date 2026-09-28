# Review large test files for refactoring opportunities

## Description

Multiple test files exceed 500 lines. While test files can be larger than production code due to test data, files over 600+ lines should be reviewed for potential splitting by test category or feature area.

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

Regenerated 2026-09-28 (triage 477) from `wc -l` over `tests/**/*.cs`; the 2025 list named six files that no
longer exist. Review each; split only where the guidelines below apply.

- [ ] `tests/timewarp-nuru-tests/generator/generator-26-constructor-dependency-resolution.cs` (729 lines)
- [ ] `tests/timewarp-nuru-tests/repl/repl-31-multiline-buffer.cs` (726 lines)
- [ ] `tests/timewarp-nuru-tests/routing/routing-05-option-matching.cs` (717 lines)
- [ ] `tests/timewarp-nuru-tests/repl/repl-23-key-binding-profiles.cs` (659 lines)
- [ ] `tests/timewarp-nuru-tests/repl/repl-18-psreadline-keybindings.cs` (557 lines)
- [ ] `tests/timewarp-nuru-tests/repl/repl-28-text-selection.cs` (525 lines)
- [ ] `tests/timewarp-nuru-tests/generator/generator-01-intercept.cs` (523 lines)
- [ ] `tests/timewarp-nuru-tests/repl/repl-33-yank-arguments.cs` (520 lines)
- [ ] `tests/timewarp-nuru-tests/lexer/lexer-15-advanced-features.cs` (515 lines)
- [ ] `tests/timewarp-nuru-tests/generator/generator-15-runtime-di.cs` (508 lines)
- [ ] `tests/timewarp-nuru-tests/repl/repl-29-word-operations.cs` (505 lines)

## Notes

### Test File Considerations

Test files are often larger because they contain:
- Test data/fixtures inline
- Multiple test methods covering edge cases
- Setup/teardown code
- Comprehensive coverage of a feature

### Splitting Strategies

1. **By test category** - Split unit/integration/edge case tests
2. **By feature subset** - Group related functionality tests
3. **Extract test data** - Move large test data to separate files
4. **Extract helpers** - Move test utilities to shared files

### Lower Priority

Test files are lower priority than production code because:
- They don't affect runtime performance
- They're less frequently read by users
- Larger test files don't increase cognitive load in the same way
- Test organization is more flexible

### Guidelines for Decision

Split a test file if:
- It covers multiple unrelated features
- Finding specific tests is difficult
- Test data could be externalized
- Shared test utilities could be extracted
