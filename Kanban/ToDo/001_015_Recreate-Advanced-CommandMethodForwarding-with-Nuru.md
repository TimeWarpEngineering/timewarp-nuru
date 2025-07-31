# Recreate Advanced.CommandMethodForwarding with Nuru

## Description

Port the Cocona Advanced.CommandMethodForwarding sample to Nuru, demonstrating command method forwarding for creating aliases and command routing.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application with command forwarding
- Implement command aliases using forwarding
- Handle parameter passing in forwarding
- Create Overview.md comparing forwarding approaches
- Implementation location: `Samples/CoconaComparison/Advanced/command-method-forwarding`

## Checklist

### Implementation
- [ ] Create base commands
- [ ] Implement forwarding attributes
- [ ] Set up command aliases
- [ ] Test forwarding behavior

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Command forwarding syntax
  - [ ] Alias creation patterns
  - [ ] Parameter forwarding
  - [ ] Method resolution

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/Advanced.CommandMethodForwarding/`

Key features to compare:
- [CommandMethodForwardedTo] attribute
- Alias command patterns
- Parameter mapping
- Forwarding chains