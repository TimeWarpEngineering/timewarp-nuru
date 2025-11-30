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
- [x] Create repl-options-showcase.cs in samples/repl-demo/
- [x] Configure PersistHistory = true with custom HistoryFilePath
- [x] Set MaxHistorySize to demonstrate history limits
- [x] Enable ShowExitCode option
- [x] Use non-default PromptColor
- [x] Set ContinueOnError = false
- [x] Configure HistoryIgnorePatterns
- [x] Add commands that demonstrate each feature
- [x] Verify demo compiles and runs correctly

### Documentation
- [x] Add inline comments explaining each ReplOptions property

## Notes

- This demo should be more educational than practical
- Consider adding commands that help demonstrate each feature:
  - A command that fails (to show ContinueOnError behavior)
  - A command that returns non-zero exit code (to show ShowExitCode)
  - Commands that should be ignored by history (to show HistoryIgnorePatterns)
- Reference ReplOptions class in TimeWarp.Nuru.Repl for all available properties
- Use repl-basic-demo.cs as the template for the AddReplSupport() pattern
