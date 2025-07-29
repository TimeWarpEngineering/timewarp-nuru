# Recreate InAction.Validation with Nuru

## Description

Port the Cocona InAction.Validation sample to Nuru, demonstrating parameter validation using data annotations and custom validators.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application with validation
- Implement data annotation validators
- Add custom validation logic
- Create Overview.md comparing validation approaches

## Checklist

### Implementation
- [ ] Add data annotation attributes
- [ ] Create custom validators
- [ ] Implement validation error handling
- [ ] Test validation scenarios

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Validation attribute usage
  - [ ] Custom validator patterns
  - [ ] Error message formatting
  - [ ] Validation pipeline

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/InAction.Validation/`

Key features to compare:
- DataAnnotations support
- ICoconaParameterValidator interface
- Validation error messages
- Multiple validation rules