# Recreate Advanced.JsonValueConverter with Nuru

## Description

Port the Cocona Advanced.JsonValueConverter sample to Nuru, demonstrating custom JSON value conversion for complex parameter types.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application with JSON converters
- Implement custom type conversion from JSON
- Handle complex object parameters
- Create Overview.md comparing conversion approaches
- Implementation location: `Samples/CoconaComparison/Advanced/json-value-converter`

## Checklist

### Implementation
- [ ] Create custom value converters
- [ ] Support JSON string parameters
- [ ] Deserialize to complex types
- [ ] Test JSON parsing scenarios

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Value converter interfaces
  - [ ] JSON parsing strategies
  - [ ] Type registration patterns
  - [ ] Error handling for JSON

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/Advanced.JsonValueConverter/`

Key features to compare:
- ICoconaValueConverter interface
- JSON deserialization
- Custom type support
- Converter registration