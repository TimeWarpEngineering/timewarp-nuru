# MCP Server

AI-powered development assistance for TimeWarp.Nuru through the Model Context Protocol (MCP).

## What is the MCP Server?

The TimeWarp.Nuru MCP Server integrates with AI coding assistants (Claude Code, Roo Code, Continue) to provide real-time help while you build CLI applications:

- **Route validation**: Check patterns before you write code
- **Handler generation**: Auto-generate handler code from routes
- **Syntax help**: Get instant documentation and examples
- **Error guidance**: Understand error handling patterns

## Installation

### For Claude Code

Add the MCP server to Claude Code using the CLI:

```bash
claude mcp add --transport stdio --scope user timewarp-nuru -- dnx TimeWarp.Nuru.Mcp --prerelease --yes
```

This command:
- Installs the `TimeWarp.Nuru.Mcp` .NET global tool (including prerelease versions)
- Registers it with Claude Code as the `timewarp-nuru` MCP server
- Configures it for your user account (not just the current project)

After installation, restart Claude Code to load the MCP server.

#### Removing from Claude Code

To remove the MCP server from Claude Code:

```bash
claude mcp remove timewarp-nuru
```

This unregisters the server but does not uninstall the .NET global tool. To completely remove:

```bash
claude mcp remove timewarp-nuru
dotnet tool uninstall --global TimeWarp.Nuru.Mcp
```

### For Other AI Assistants (Manual Configuration)

#### Step 1: Install the Global Tool

```bash
dotnet tool install --global TimeWarp.Nuru.Mcp
```

#### Step 2: Configure Your AI Assistant

Add to your MCP client configuration (Roo Code, Continue, etc.):

```json
{
  "servers": {
    "timewarp-nuru": {
      "type": "stdio",
      "command": "timewarp-nuru-mcp"
    }
  }
}
```

#### Step 3: Restart Your IDE

The MCP server will automatically load and be available through your AI assistant.

## What You Can Do

### Validate Route Patterns

Ask your AI assistant to check if your routes are valid:

```
"Validate the route pattern 'deploy {env} {tag?}'"
"Is 'git commit --amend -m {message}' a valid route?"
"What's wrong with 'deploy <env>'?"
```

The MCP server analyzes your pattern and reports:
- ✅ Whether it's valid
- 📊 Detailed structure (segments, parameters, options)
- 🎯 Specificity score
- ⚠️ Any errors or warnings

### Generate Handler Code

Get instant handler code for your routes:

```
"Generate a handler for 'deploy {env} --dry-run'"
"Create a mediator handler for 'backup {source} {dest?}'"
"Show me handler code for 'docker run {*args}'"
```

The MCP server generates:
- Direct delegate handlers (fast approach)
- Mediator command and handler classes (structured approach)
- Properly typed parameters
- Ready-to-use code

### Get Syntax Help

Learn route pattern syntax interactively:

```
"Show me how to use optional parameters"
"Get syntax for catch-all parameters"
"How do I use typed parameters?"
"Show me all route pattern syntax"
```

### Access Examples

Fetch working examples from the repository:

```
"Show me the basic TimeWarp.Nuru example"
"Get the mediator pattern example"
"List all available examples"
"Show me the logging example"
```

## Available Tools

The MCP server provides these tools to your AI assistant:

| Tool | Purpose |
|------|---------|
| `validate_route` | Check if a route pattern is valid |
| `generate_handler` | Generate handler code from a pattern |
| `get_syntax` | Get route pattern syntax documentation |
| `get_pattern_examples` | Get examples for specific features |
| `get_example` | Fetch working code examples |
| `list_examples` | List all available examples |
| `get_error_handling_info` | Get error handling guidance |
| `cache_status` | Check example cache status |
| `clear_cache` | Clear cached examples |

## Real-World Usage

### Learning TimeWarp.Nuru

```
You: "Show me how to create a basic CLI app"
AI (using MCP): [Fetches and displays basic example]

You: "Now validate this pattern: 'greet {name} {greeting?}'"
AI (using MCP): "✅ Valid pattern! Here's the structure..."

You: "Generate a handler for that route"
AI (using MCP): [Generates handler code with proper types]
```

### Building a CLI App

```
You: "I'm building a deployment tool. Validate 'deploy {env} --version {tag?} --dry-run'"
AI (using MCP): "✅ Valid! Specificity: 15. This pattern has..."

You: "Generate a mediator handler for it"
AI (using MCP): [Generates command class and handler with DI]

You: "What if I want to make 'env' optional too?"
AI (using MCP): "⚠️ That would create ambiguity (NURU_S002). Consider using options instead..."
```

### Debugging Route Issues

```
You: "Why isn't my route 'copy <source> <dest>' working?"
AI (using MCP): "❌ Invalid syntax (NURU_P001). Use curly braces: 'copy {source} {dest}'"

You: "Fixed. Now validate 'copy {file?} {*args}'"
AI (using MCP): "❌ Can't mix optional with catch-all (NURU_S004). Use one or the other."
```

## Benefits

| Without MCP | With MCP |
|-------------|----------|
| Look up docs manually | Ask AI, get instant answers |
| Write handler code by hand | Generate correct code automatically |
| Trial and error with routes | Validate before writing code |
| Remember syntax rules | AI provides syntax with examples |

## Caching

Examples are cached for fast access:
- **Memory cache**: Instant access during session
- **Disk cache**: Persists between sessions (1-hour TTL)

Cache location:
- Linux/macOS: `~/.local/share/TimeWarp.Nuru.Mcp/cache/examples/`
- Windows: `%LOCALAPPDATA%\TimeWarp.Nuru.Mcp\cache\examples\`

## Supported AI Assistants

The MCP server works with any MCP-compatible AI assistant:

- ✅ Claude Code (VS Code extension)
- ✅ Roo Code
- ✅ Continue (VS Code extension)
- ✅ Any MCP-compatible client

## Troubleshooting

### Server not loading?

1. Check installation: `dotnet tool list --global | grep TimeWarp.Nuru.Mcp`
2. Verify configuration in MCP client settings
3. Restart your IDE completely

### Cache issues?

Ask your AI: "Clear the TimeWarp.Nuru MCP cache"

### Need latest examples?

Ask with force refresh: "Get the latest mediator example (force refresh)"

## Related Documentation

- **[Getting Started](../getting-started.md)** - Build your first app
- **[Routing](../features/routing.md)** - Route pattern syntax
- **[Analyzer](../features/analyzer.md)** - Compile-time validation
- **[Examples](../../../samples/)** - Working code samples
