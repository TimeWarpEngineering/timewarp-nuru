# Recreate Advanced.OptionLikeCommand with Nuru

## Description

Port the Cocona Advanced.OptionLikeCommand sample to Nuru, demonstrating option-like commands that act like global options but execute command logic.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application with option-like commands
- Implement --version and --help as commands
- Handle special option patterns
- Create Overview.md comparing approaches

## Checklist

### Implementation
- [ ] Create option-like command handlers
- [ ] Implement --version command
- [ ] Add custom option commands
- [ ] Test option-command behavior

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Option-like command patterns
  - [ ] Special option handling
  - [ ] Command vs option semantics
  - [ ] Priority and ordering

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/Advanced.OptionLikeCommand/`

Key features to compare:
- [OptionLikeCommand] attribute
- Global option behavior
- Command execution priority
- Built-in option override