# Recreate GettingStarted.MinimalApp with Nuru

## Description

Port the Cocona GettingStarted.MinimalApp sample to Nuru, demonstrating the simplest possible CLI application with basic argument and option handling.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a minimal Nuru CLI application that matches the functionality of Cocona's MinimalApp sample
- Implement basic command with argument and option handling
- Create Overview.md comparing Cocona vs Nuru approaches
- Implementation location: `Samples/CoconaComparison/GettingStarted/minimal-app`

## Checklist

### Implementation
- [x] Create single-file executable at `Samples/CoconaComparison/GettingStarted/minimal-app` (delegate version)
- [x] Create single-file executable at `Samples/CoconaComparison/GettingStarted/minimal-app-di` (class-based with DI)
- [x] Port the Hello command with toUpperCase option and name argument
- [x] Ensure proper argument and option parsing
- [x] Test both implementations work as expected

### Documentation
- [x] Create Overview.md with side-by-side comparison
- [x] Document differences in:
  - [x] Project setup and initialization
  - [x] Main method structure
  - [x] Attribute/annotation usage
  - [x] Argument and option declaration

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/GettingStarted.MinimalApp/`

Key features to compare:
- CoconaApp.Run<T>() vs Nuru equivalent
- [Argument] attribute vs Nuru approach
- Boolean option handling
- Console output methods