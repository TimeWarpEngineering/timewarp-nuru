# Update examples.json Descriptions with Package Requirements

## Description

Update example descriptions in examples.json to clearly indicate which examples require additional packages, helping developers choose appropriate starting points.

## Requirements

- Update example descriptions to indicate package requirements
- Add "REQUIRES: Mediator packages" to mediator examples
- Add "NO additional packages" to delegate examples
- Add tags like "requires-packages" or "no-dependencies"

## Checklist

### Implementation
- [ ] Audit examples.json to identify all examples
- [ ] Categorize examples by package requirements
- [ ] Update descriptions for mediator examples
- [ ] Update descriptions for delegate examples
- [ ] Add appropriate tags to all examples
- [ ] Validate JSON format after changes

### Testing
- [ ] Verify MCP list_examples tool shows updated descriptions
- [ ] Verify get_example tool returns updated metadata

## Notes

Tags: mcp, documentation

File: `samples/examples.json`

Example updates:
```json
{
  "name": "mediator-example",
  "description": "REQUIRES: Mediator packages. Demonstrates command routing with Mediator pattern.",
  "tags": ["mediator", "requires-packages", "di"]
}

{
  "name": "delegate-example", 
  "description": "NO additional packages. Simple delegate-based command routing.",
  "tags": ["delegate", "no-dependencies", "minimal"]
}
```
