# Update generate_handler MCP Tool with Package Requirements

## Description

Enhance the generate_handler MCP tool to include required package information when generating mediator-based code, helping developers avoid common setup errors.

## Requirements

- When `useMediator=true`, include required package information in generated code
- Show both .csproj PackageReference format and #:package directive format
- Include the common error message and solution

## Checklist

### Implementation
- [ ] Update generate-handler-tool.cs to detect mediator pattern usage
- [ ] Add PackageReference XML format to generated output
- [ ] Add #:package directive format for file-based apps
- [ ] Add common error message and solution comment
- [ ] Test output with useMediator=true
- [ ] Test output with useMediator=false (no package info needed)

### Testing
- [ ] Verify generated code compiles with proper packages
- [ ] Test MCP tool response format

## Notes

Tags: mcp, mediator

File: `source/timewarp-nuru-mcp/tools/generate-handler-tool.cs`

Example output addition:
```csharp
// REQUIRED PACKAGES (choose format based on your project type):
// 
// For .csproj projects:
//   <PackageReference Include="Mediator" Version="3.0.0" />
//   <PackageReference Include="Mediator.Abstractions" Version="3.0.0" />
//
// For file-based apps (.cs with shebang):
//   #:package Mediator@3.0.0
//   #:package Mediator.Abstractions@3.0.0
//
// COMMON ERROR: "Cannot resolve Mediator" 
// SOLUTION: Ensure services.AddMediator() is called in your builder configuration
```
