# Recreate GettingStarted.SubCommandApp with Nuru

## Description

Port the Cocona GettingStarted.SubCommandApp sample to Nuru, demonstrating sub-command functionality including nested commands and primary commands.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application with multiple sub-commands
- Implement nested command structure (sub-commands and sub-sub-commands)
- Support shell completion
- Create Overview.md comparing sub-command approaches

## Checklist

### Implementation
- [ ] Create main program with Hello and Bye commands
- [ ] Implement SubCommands class with Konnichiwa and Hello methods
- [ ] Implement SubSubCommands class with Foobar and Primary methods
- [ ] Enable shell completion support
- [ ] Test all command paths work correctly

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Sub-command declaration ([HasSubCommands] vs Nuru)
  - [ ] Command attributes and descriptions
  - [ ] Primary command patterns
  - [ ] Shell completion configuration
  - [ ] Enum parameter handling

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/GettingStarted.SubCommandApp/`

Key features to compare:
- [HasSubCommands] attribute vs Nuru approach
- [Command] attribute with descriptions
- [PrimaryCommand] vs Nuru default command
- Enum as command parameter
- Shell completion enablement