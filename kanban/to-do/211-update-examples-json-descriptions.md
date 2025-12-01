# Update examples.json Descriptions with Package Requirements

## Description

Update the `samples/examples.json` file to include clear package requirements in the descriptions. AI agents reading these descriptions need to understand which NuGet packages are required for each example type.

## Parent

MCP Builder Pattern Guidance Analysis - standardizing samples to prevent AI confusion

## Requirements

- Add package requirements to Mediator-based example descriptions
- Add "NO additional packages required" to delegate-only example descriptions
- Ensure descriptions are clear and actionable for AI agents

## Checklist

### Implementation
- [ ] Update "mediator" example description to include: "REQUIRES: Mediator.Abstractions + Mediator.SourceGenerator packages and services.AddMediator(options => {...})"
- [ ] Update "delegate" example description to include: "NO additional packages required. Maximum performance."
- [ ] Review all other example descriptions for clarity on package requirements
- [ ] Verify examples.json is valid JSON after changes
- [ ] Test MCP tools that read examples.json to ensure they work correctly

## Notes

**Target changes for examples.json:**

```json
{
  "id": "mediator",
  "description": "Mediator pattern with DI. REQUIRES: Mediator.Abstractions + Mediator.SourceGenerator packages and services.AddMediator(options => {...})",
  ...
},
{
  "id": "delegate",
  "description": "Pure delegate routing - NO additional packages required. Maximum performance.",
  ...
}
```

File to update:
- `samples/examples.json`

Reference analysis: `.agent/workspace/2025-12-01T21-15-00_mcp-builder-pattern-guidance-analysis.md`
