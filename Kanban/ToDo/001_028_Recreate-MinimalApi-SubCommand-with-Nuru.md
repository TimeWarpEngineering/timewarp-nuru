# Recreate MinimalApi.SubCommand with Nuru

## Description

Port the Cocona MinimalApi.SubCommand sample to Nuru, demonstrating sub-command implementation using minimal API patterns.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI with minimal API sub-commands
- Implement nested command structure minimally
- Show sub-command organization patterns
- Create Overview.md comparing approaches
- Implementation location: `Samples/CoconaComparison/MinimalApi/minimal-api-subcommand`

## Checklist

### Implementation
- [ ] Create sub-commands with minimal API
- [ ] Implement command nesting
- [ ] Add command groups
- [ ] Test sub-command routing

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Minimal sub-command syntax
  - [ ] Command grouping patterns
  - [ ] Nesting strategies
  - [ ] Route-like command paths

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/MinimalApi.SubCommand/`

Key features to compare:
- Minimal API sub-commands
- Command path building
- Group organization
- Nested lambda handlers