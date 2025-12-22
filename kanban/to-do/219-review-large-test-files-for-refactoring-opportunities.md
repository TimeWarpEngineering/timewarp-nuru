# Review large test files for refactoring opportunities

## Description

Multiple test files exceed 500 lines. While test files can be larger than production code due to test data, files over 600+ lines should be reviewed for potential splitting by test category or feature area.

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

### High Priority (600+ lines)
- [ ] `attributed-route-generator-02-source.cs` (692 lines)
- [ ] `routing-05-option-matching.cs` (689 lines)
- [ ] `engine-03-candidate-generator.cs` (673 lines)
- [ ] `engine-01-input-tokenizer.cs` (672 lines)
- [ ] `delegate-command-generator-01-basic.cs` (624 lines)
- [ ] `program.cs` (testapp-mediator) (595 lines)

### Medium Priority (500-600 lines)
- [ ] `repl-31-multiline-buffer.cs` (572 lines)
- [ ] `repl-18-psreadline-keybindings.cs` (558 lines)
- [ ] `repl-23-key-binding-profiles.cs` (530 lines)
- [ ] `repl-28-text-selection.cs` (526 lines)
- [ ] `attributed-route-generator-03-matching.cs` (524 lines)
- [ ] `repl-33-yank-arguments.cs` (521 lines)
- [ ] `lexer-15-advanced-features.cs` (515 lines)
- [ ] `repl-29-word-operations.cs` (506 lines)

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
