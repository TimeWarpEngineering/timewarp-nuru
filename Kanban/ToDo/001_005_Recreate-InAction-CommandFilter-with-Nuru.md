# Recreate InAction.CommandFilter with Nuru

## Description

Port the Cocona InAction.CommandFilter sample to Nuru, demonstrating command filter/middleware functionality for cross-cutting concerns like logging, validation, and authorization.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application with command filters/middleware
- Implement pre and post command execution logic
- Demonstrate filter ordering and chaining
- Create Overview.md comparing filter/middleware approaches

## Checklist

### Implementation
- [ ] Create custom command filter implementations
- [ ] Apply filters to commands
- [ ] Implement filter ordering
- [ ] Test filter execution pipeline

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Filter/middleware concepts
  - [ ] Filter attribute usage
  - [ ] Execution pipeline ordering
  - [ ] Context passing between filters

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/InAction.CommandFilter/`

Key features to compare:
- CommandFilterAttribute vs Nuru middleware
- Filter execution order
- Command context access
- Exception handling in filters