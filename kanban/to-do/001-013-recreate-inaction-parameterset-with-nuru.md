# Recreate InAction.ParameterSet with Nuru

## Description

Port the Cocona InAction.ParameterSet sample to Nuru, demonstrating parameter set functionality for mutually exclusive option groups.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application with parameter sets
- Implement mutually exclusive option groups
- Handle parameter set validation
- Create Overview.md comparing parameter set approaches
- Implementation location: `samples/cocona-comparison/InAction/parameter-set`

## Checklist

### Implementation
- [ ] Define parameter set groups
- [ ] Create commands with exclusive options
- [ ] Implement parameter set validation
- [ ] Test mutual exclusion rules

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Parameter set declaration
  - [ ] Mutual exclusion patterns
  - [ ] Validation approaches
  - [ ] Error messages

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/InAction.ParameterSet/`

Key features to compare:
- ICommandParameterSet interface
- Parameter set attributes
- Validation logic
- Help text for parameter sets