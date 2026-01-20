# Review and update MCP server for V2 source generator architecture

## Description

The TimeWarp.Nuru MCP (Model Context Protocol) server provides AI assistants with tools to understand and generate Nuru code. With the V2 source generator architecture, the MCP server needs to be reviewed and updated to:

1. Reflect the new compile-time source generator approach
2. Update examples to use `NuruApp.CreateBuilder()` API
3. Remove any Mediator-related content
4. Add tools for new features (behaviors, custom converters, etc.)

## MCP Server Location

- **Source:** `source/timewarp-nuru-mcp/`
- **Tests:** `tests/timewarp-nuru-mcp-tests/`

## Current Tools

- `get-example-tool.cs` - Retrieves code examples
- `get-syntax-tool.cs` - Route pattern syntax documentation
- `validate-route-tool.cs` - Validates route patterns
- `generate-handler-tool.cs` - Generates handler code
- `error-handling-tool.cs` - Error handling documentation
- `get-version-info-tool.cs` - Version information
- `cache-management-tool.cs` - Cache management

## Checklist

### Review Existing Tools
- [ ] Review `get-example-tool.cs` - ensure examples use V2 API
- [ ] Review `get-syntax-tool.cs` - update syntax docs if needed
- [ ] Review `validate-route-tool.cs` - ensure validation reflects V2
- [ ] Review `generate-handler-tool.cs` - update generated code patterns
- [ ] Review `error-handling-tool.cs` - update error scenarios

### Update Examples
- [ ] Update all examples to use `NuruApp.CreateBuilder()` pattern
- [ ] Remove any Mediator-based examples
- [ ] Add examples for new V2 features

### Add New Tools (if needed)
- [ ] Consider tool for pipeline behavior patterns
- [ ] Consider tool for custom type converter patterns
- [ ] Consider tool for attributed routes patterns
- [ ] Consider tool for REPL configuration

### Update Tests
- [ ] Review and update `mcp-01-example-retrieval.cs`
- [ ] Review and update `mcp-02-syntax-documentation.cs`
- [ ] Review and update `mcp-03-route-validation.cs`
- [ ] Review and update `mcp-04-handler-generation.cs`
- [ ] Review and update `mcp-05-error-documentation.cs`
- [ ] Review and update `mcp-06-version-info.cs`

### Documentation
- [ ] Update MCP server README if exists
- [ ] Ensure tool descriptions reflect V2 architecture

## Notes

The MCP server is used by AI assistants (like Claude) to understand TimeWarp.Nuru patterns. Keeping it up-to-date ensures AI-generated code follows current best practices.

Key V2 changes to reflect:
- `NuruApp.CreateBuilder()` instead of `new NuruAppBuilder()`
- No Mediator dependency
- Source generator handles all routing at compile-time
- Pipeline behaviors via `INuruBehavior<T>`
- Custom type converters via `IRouteTypeConverter`
- Attributed routes via `[NuruRoute]`
