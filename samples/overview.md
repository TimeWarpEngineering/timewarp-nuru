# TimeWarp.Nuru Samples

This directory contains example applications demonstrating various features and patterns of TimeWarp.Nuru.

## Important Files

### `examples.json`

**⚠️ DO NOT RENAME OR MOVE THIS FILE ⚠️**

The `examples.json` file is a manifest used by the TimeWarp.Nuru MCP (Model Context Protocol) server for dynamic example discovery. This enables AI assistants like Claude to automatically fetch and present relevant code examples.

**Purpose:**
- Provides metadata about available examples
- Enables dynamic discovery without code changes
- Allows MCP server to fetch examples from GitHub
- Maintains a single source of truth for example documentation

**Adding New Examples:**
When adding a new example to this repository:
1. Create your example file in the appropriate directory
2. Add an entry to `examples.json` with:
   - Unique `id` (lowercase, hyphenated)
   - Descriptive `name` and `description`
   - Relative `path` from the Samples directory
   - Relevant `tags` for categorization
   - `difficulty` level (beginner, intermediate, advanced)

## Available Examples

### Getting Started
- **basic** - Basic TimeWarp.Nuru application with various route patterns
- **console-logging** - Simple console logging integration

### Advanced Features
- **async** - Async command handlers with Task-based routes
- **serilog** - Structured logging with Serilog

### Architecture Patterns
- **delegates** - Direct delegate routing for maximum performance
- **mediator** - Mediator pattern with dependency injection

## Running Examples

Most examples are single-file applications that can be run directly:

```bash
# Make executable (Linux/macOS)
chmod +x example.cs

# Run directly
./example.cs

# Or run with dotnet
dotnet run example.cs
```

## Example Structure

Each example typically includes:
- Route definitions demonstrating specific patterns
- Comments explaining key concepts
- Real-world use cases
- Performance considerations where relevant

## Integration with MCP Server

The TimeWarp.Nuru MCP server uses the `examples.json` manifest to:
1. List available examples dynamically
2. Fetch specific examples on demand
3. Cache examples for offline access
4. Provide example metadata to AI assistants

For more information about the MCP server, see [Source/TimeWarp.Nuru.Mcp/README.md](../Source/TimeWarp.Nuru.Mcp/README.md).