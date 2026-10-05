# Tools

Supporting tools and integrations for TimeWarp.Nuru development.

## Available Tools

### [Key bindings](key-bindings.md)
List built-in REPL key bindings from the command line:
- Same table as the REPL `key-bindings` command
- Filter by profile, chord, or function name
- Table or detailed view

### [MCP Server](mcp-server.md)
> Frozen and **not part of the 3.0 release**. Use the Nuru skill (`skills/tw-nuru/SKILL.md`) instead.

AI-assisted development with Model Context Protocol:
- Route pattern validation
- Handler code generation
- Syntax examples and documentation
- Error handling guidance
- Integration with Claude Code, Roo Code, Continue
- Real-time assistance

## Tool Highlights

| Tool | Purpose | Learn More |
|------|---------|------------|
| ⌨️ Key bindings | List built-in REPL shortcuts | [Key bindings](key-bindings.md) |
| 🤖 MCP Server | Frozen; not part of 3.0 | [MCP Server](mcp-server.md) |

## Installation

### MCP Server

Not part of the 3.0 release. Older prereleases install with:

```bash
# Install as global tool
dotnet tool install --global TimeWarp.Nuru.Mcp

# Configure in your IDE (example for Claude Code)
# Add to MCP configuration
```

See [MCP Server documentation](mcp-server.md) for complete setup instructions.

## Future Tools

Planned tool integrations:
- Visual route designer
- Testing utilities
- Performance profilers
- Migration assistants

## Related Documentation

- **[Features](../features/)** - Core framework features
- **[Getting Started](../getting-started.md)** - Basic setup
- **[Developer Documentation](../../developer/)** - For contributors
