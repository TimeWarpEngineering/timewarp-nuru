# Recreate InAction.CommandOptionOverload with Nuru

## Description

Port the Cocona InAction.CommandOptionOverload sample to Nuru, demonstrating command overloading capabilities with different parameter sets.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application with command overloading
- Implement multiple command signatures with same name
- Handle different parameter combinations
- Create Overview.md comparing overload approaches
- Implementation location: `Samples/CoconaComparison/InAction/command-option-overload`

## Checklist

### Implementation
- [ ] Create commands with multiple overloads
- [ ] Implement parameter set differentiation
- [ ] Test all overload variations
- [ ] Ensure proper overload resolution

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Command overload declaration
  - [ ] Parameter set handling
  - [ ] Overload resolution rules
  - [ ] Help text for overloaded commands

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/InAction.CommandOptionOverload/`

Key features to compare:
- [CommandOverload] attribute usage
- Parameter matching logic
- Overload disambiguation
- Help system with overloads