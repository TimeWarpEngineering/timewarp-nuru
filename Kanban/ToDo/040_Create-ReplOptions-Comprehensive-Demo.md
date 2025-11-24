# Create Comprehensive ReplOptions Demo

## Description

Create a new REPL demo file (repl-options-showcase.cs) that demonstrates ALL ReplOptions features not covered in other demos.

## Requirements

- Demonstrate PersistHistory with custom HistoryFilePath
- Demonstrate MaxHistorySize configuration
- Demonstrate ShowExitCode option
- Demonstrate custom PromptColor (not default green)
- Demonstrate ContinueOnError = false behavior
- Demonstrate custom HistoryIgnorePatterns
- Include clear comments explaining each option
- Use AddReplSupport() builder pattern

## Checklist

### Implementation
- [ ] Create repl-options-showcase.cs in Samples/ReplDemo/
- [ ] Configure PersistHistory = true with custom HistoryFilePath
- [ ] Set MaxHistorySize to demonstrate history limits
- [ ] Enable ShowExitCode option
- [ ] Use non-default PromptColor
- [ ] Set ContinueOnError = false
- [ ] Configure HistoryIgnorePatterns
- [ ] Add commands that demonstrate each feature
- [ ] Verify demo compiles and runs correctly

### Documentation
- [ ] Add inline comments explaining each ReplOptions property

## Notes

- This demo should be more educational than practical
- Consider adding commands that help demonstrate each feature:
  - A command that fails (to show ContinueOnError behavior)
  - A command that returns non-zero exit code (to show ShowExitCode)
  - Commands that should be ignored by history (to show HistoryIgnorePatterns)
- Reference ReplOptions class in TimeWarp.Nuru.Repl for all available properties
- Use repl-basic-demo.cs as the template for the AddReplSupport() pattern
