# Recreate InAction.CommandOptions with Nuru

## Description

Port the Cocona InAction.CommandOptions sample to Nuru, demonstrating various command option types, short/long names, and default values.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application showcasing all option types
- Implement short and long option names
- Handle default values and required options
- Create Overview.md comparing option handling
- Implementation location: `Samples/CoconaComparison/InAction/command-options`

## Checklist

### Implementation
- [ ] Create commands with various option types
- [ ] Implement short (-x) and long (--xxx) options
- [ ] Add default values and optional parameters
- [ ] Test option parsing and validation

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Option attribute syntax
  - [ ] Short/long name conventions
  - [ ] Default value handling
  - [ ] Required vs optional options

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/InAction.CommandOptions/`

Key features to compare:
- [Option] attribute parameters
- Option naming conventions
- Type conversion for options
- Boolean flag handling