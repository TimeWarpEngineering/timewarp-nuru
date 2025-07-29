# Recreate GettingStarted.MinimalApp with Nuru

## Description

Port the Cocona GettingStarted.MinimalApp sample to Nuru, demonstrating the simplest possible CLI application with basic argument and option handling.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a minimal Nuru CLI application that matches the functionality of Cocona's MinimalApp sample
- Implement basic command with argument and option handling
- Create Overview.md comparing Cocona vs Nuru approaches

## Checklist

### Implementation
- [ ] Create Nuru project structure
- [ ] Port the Hello command with toUpperCase option and name argument
- [ ] Ensure proper argument and option parsing
- [ ] Test the application works as expected

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Project setup and initialization
  - [ ] Main method structure
  - [ ] Attribute/annotation usage
  - [ ] Argument and option declaration

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/GettingStarted.MinimalApp/`

Key features to compare:
- CoconaApp.Run<T>() vs Nuru equivalent
- [Argument] attribute vs Nuru approach
- Boolean option handling
- Console output methods