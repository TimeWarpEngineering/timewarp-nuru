# Recreate InAction.MultipleCommandTypes with Nuru

## Description

Port the Cocona InAction.MultipleCommandTypes sample to Nuru, demonstrating organizing commands across multiple classes and types.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application with multiple command classes
- Organize commands by feature or domain
- Register multiple command types
- Create Overview.md comparing organization patterns
- Implementation location: `samples/cocona-comparison/InAction/multiple-command-types`

## Checklist

### Implementation
- [ ] Create multiple command classes
- [ ] Register all command types with app
- [ ] Organize commands logically
- [ ] Test command discovery

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Command class organization
  - [ ] Multiple type registration
  - [ ] Command discovery patterns
  - [ ] Namespace conventions

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/InAction.MultipleCommandTypes/`

Key features to compare:
- Multiple command class registration
- Command organization strategies
- Type discovery mechanisms
- Command grouping patterns