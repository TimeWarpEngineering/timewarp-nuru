# Recreate InAction.ExitCode with Nuru

## Description

Port the Cocona InAction.ExitCode sample to Nuru, demonstrating proper exit code handling for success, errors, and custom status codes.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application with exit code control
- Implement commands that return different exit codes
- Handle exceptions and their exit codes
- Create Overview.md comparing exit code strategies

## Checklist

### Implementation
- [ ] Create commands with various exit scenarios
- [ ] Implement custom exit code returns
- [ ] Handle exception-based exit codes
- [ ] Test exit code propagation

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Exit code return methods
  - [ ] Exception handling and codes
  - [ ] Success/failure conventions
  - [ ] Custom exit code patterns

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/InAction.ExitCode/`

Key features to compare:
- Return value as exit code
- CommandExitedException usage
- Error handling strategies
- Exit code constants