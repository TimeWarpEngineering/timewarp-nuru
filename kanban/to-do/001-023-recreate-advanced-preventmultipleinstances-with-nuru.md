# Recreate Advanced.PreventMultipleInstances with Nuru

## Description

Port the Cocona Advanced.PreventMultipleInstances sample to Nuru, demonstrating single instance enforcement using mutex or similar mechanisms.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application with single instance check
- Implement mutex-based instance prevention
- Handle already-running scenarios gracefully
- Create Overview.md comparing singleton approaches
- Implementation location: `samples/cocona-comparison/Advanced/prevent-multiple-instances`

## Checklist

### Implementation
- [ ] Create instance prevention attribute
- [ ] Implement mutex or similar mechanism
- [ ] Add graceful handling for duplicates
- [ ] Test multiple instance scenarios

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Singleton enforcement patterns
  - [ ] Mutex implementation details
  - [ ] Error handling strategies
  - [ ] Cross-platform considerations

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/Advanced.PreventMultipleInstances/`

Key features to compare:
- PreventMultipleInstancesAttribute
- Mutex usage patterns
- Instance detection logic
- User notification methods