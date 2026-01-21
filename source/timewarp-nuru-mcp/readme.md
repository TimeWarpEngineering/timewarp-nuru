# TimeWarp.Nuru MCP Server

An MCP (Model Context Protocol) server that provides tools for working with TimeWarp.Nuru, a route-based CLI framework for .NET.

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

```bash
claude mcp remove timewarp-nuru
```

This unregisters the server but does not uninstall the .NET global tool. To completely remove:

```bash
claude mcp remove timewarp-nuru
dotnet tool uninstall --global TimeWarp.Nuru.Mcp
```

### For Other MCP Clients (Manual Configuration)

#### Install the .NET Global Tool

```bash
# Install from NuGet (when published)
dotnet tool install --global TimeWarp.Nuru.Mcp

# Or install from local package during development
dotnet tool install --global --add-source ./Source/TimeWarp.Nuru.Mcp/bin/Release/ TimeWarp.Nuru.Mcp --version 2.1.0-beta.20

# Uninstall
dotnet tool uninstall --global TimeWarp.Nuru.Mcp
```

#### Configure in Your MCP Client

After installing the tool, configure it in your MCP-compatible client (Roo Code, Continue, etc.):

Add the following MCP server configuration:

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

Most MCP clients will automatically detect and load the server after restarting.

## Features

### Available Tools

#### 1. `get_example`
Fetches TimeWarp.Nuru code examples from the GitHub repository with intelligent caching.

**Parameters:**
- `name`: Example name or 'list' to see all available examples
- `forceRefresh`: Force refresh from GitHub, bypassing cache (default: false)

**Available Examples:**
- `basic`, `createbuilder`, `delegate`, `mixed` - Calculator examples
- `hello-world` - Simplest possible app
- `async` - Async command handlers
- `console-logging`, `serilog` - Logging integrations
- `configuration`, `configuration-validation`, `command-line-overrides` - Configuration samples
- `repl-basic`, `repl-keybindings`, `repl-interactive`, `repl-options` - REPL demos
- `shell-completion`, `dynamic-completion` - Tab completion examples
- `syntax-examples`, `builtin-types`, `custom-type-converter` - Reference examples
- `test-output-capture`, `test-colored-output`, `test-terminal-injection` - Testing examples
- `pipeline-middleware`, `unified-middleware` - Pipeline behavior examples
- `aspire-telemetry`, `aspire-host-otel` - OpenTelemetry/Aspire integration

**Sample Prompts:**
```
"Show me the basic TimeWarp.Nuru example"
"Get the createbuilder example from TimeWarp.Nuru"
"Show me how REPL works in Nuru"
"Get the dynamic-completion example"
"Show me how to integrate with Aspire and OpenTelemetry"
"Get the aspire-host-otel example for IHostApplicationBuilder integration"
"List all available TimeWarp.Nuru examples"
```

#### 2. `list_examples`
Lists all available TimeWarp.Nuru examples with descriptions.

**Sample Prompts:**
```
"What TimeWarp.Nuru examples are available?"
"Show me all the example types"
```

#### 3. `validate_route`
Validates a TimeWarp.Nuru route pattern and provides detailed information about its structure.

**Parameters:**
- `pattern`: The route pattern to validate (e.g., 'deploy {env} --dry-run')

**Sample Prompts:**
```
"Validate the route pattern 'deploy {env} {tag?}'"
"Check if 'git commit --amend -m {message}' is a valid route"
"What's wrong with the pattern 'prompt <input>'"
"Show me the structure of 'docker {*args}'"
```

#### 4. `cache_status`
Shows the current cache status including cached examples and their expiration times.

**Sample Prompts:**
```
"Show me the cache status"
"What examples are cached?"
"Check the MCP cache"
```

#### 5. `clear_cache`
Clears all cached TimeWarp.Nuru examples.

**Sample Prompts:**
```
"Clear the MCP cache"
"Remove all cached examples"
```

#### 6. `get_syntax`
Provides TimeWarp.Nuru route pattern syntax documentation.

**Parameters:**
- `element`: Syntax element to get documentation for (default: 'all')
  - Options: literals, parameters, types, optional, catchall, options, descriptions, all

**Sample Prompts:**
```
"Show me the route pattern syntax for parameters"
"How do I use optional parameters in TimeWarp.Nuru?"
"Get the syntax documentation for catch-all parameters"
"Show me all route pattern syntax"
```

#### 7. `get_pattern_examples`
Provides examples of specific route pattern features.

**Parameters:**
- `feature`: Pattern feature to get examples for (default: 'basic')
  - Options: basic, typed, optional, catchall, options, complex

**Sample Prompts:**
```
"Show me examples of typed parameters in routes"
"Get examples of routes with options"
"Show me complex route pattern examples"
"How do I use catch-all parameters? Show examples"
```

#### 8. `generate_handler`
Generates handler code from a route pattern using the V2 fluent DSL with `.WithHandler()`.

**Parameters:**
- `pattern`: The route pattern to generate a handler for

**Generated Code Features:**
- Uses V2 fluent DSL pattern with `.Map().WithHandler().AsCommand().Done()`
- Shows `[NuruRoute]` attributed pattern as alternative
- Demonstrates pipeline behaviors with `.AddBehavior()` and `.Implements<T>()`
- Generates correct parameter types based on route constraints

**Sample Prompts:**
```
"Generate a handler for the route 'deploy {env} --dry-run'"
"Create handler code for 'backup {source} {dest?}'"
"Generate code for 'test {project} --verbose --filter {pattern}'"
"Show me the handler signature for 'docker {*args}'"
```

#### 9. `get_error_handling_info`
Provides information about TimeWarp.Nuru error handling with different focus areas.

**Parameters:**
- `area`: Specific area to get information about (default: 'overview')
  - Options: overview, architecture, philosophy
- `forceRefresh`: Force refresh from GitHub, bypassing cache (default: false)

**Sample Prompts:**
```
"Explain TimeWarp.Nuru's error handling approach"
"Show me the error handling architecture in TimeWarp.Nuru"
"What's the philosophy behind error handling in Nuru?"
"Get the latest error handling documentation"
```

#### 10. `get_error_scenarios`
Provides information about specific error scenarios in TimeWarp.Nuru.

**Parameters:**
- `scenario`: Specific error scenario to get information about (default: 'all')
  - Options: parsing, binding, conversion, execution, matching, all
- `forceRefresh`: Force refresh from GitHub, bypassing cache (default: false)

**Sample Prompts:**
```
"What are the common error scenarios in TimeWarp.Nuru?"
"How does TimeWarp.Nuru handle parsing errors?"
"Explain parameter binding errors in Nuru"
"Show me all error scenarios in TimeWarp.Nuru"
```

#### 11. `get_error_handling_best_practices`
Provides best practices for error handling in TimeWarp.Nuru applications.

**Parameters:**
- `forceRefresh`: Force refresh from GitHub, bypassing cache (default: false)

**Sample Prompts:**
```
"What are the best practices for error handling in TimeWarp.Nuru?"
"Show me error handling recommendations for Nuru apps"
"How should I handle errors in my TimeWarp.Nuru application?"
"Get the latest error handling best practices"
```

#### 12. `get_behavior_info`
Provides information about pipeline behaviors in TimeWarp.Nuru.

**Parameters:**
- `forceRefresh`: Force refresh from GitHub, bypassing cache (default: false)

**Sample Prompts:**
```
"How do pipeline behaviors work in TimeWarp.Nuru?"
"What is INuruBehavior?"
"Explain the behavior pipeline in Nuru"
```

#### 13. `get_behavior_example`
Provides a code example for implementing basic pipeline behaviors.

**Sample Prompts:**
```
"Show me how to create a logging behavior"
"Give me an example of a pipeline behavior"
"How do I implement INuruBehavior?"
```

#### 14. `get_filtered_behavior_example`
Provides a code example for implementing filtered behaviors that only apply to specific routes.

**Sample Prompts:**
```
"How do I create behaviors that only apply to certain routes?"
"Show me filtered behavior examples"
"How do marker interfaces work with behaviors?"
"Give me an example of IRequireAuth behavior"
```

#### 15. `get_type_converter_info`
Provides information about custom type converters in TimeWarp.Nuru.

**Parameters:**
- `forceRefresh`: Force refresh from GitHub, bypassing cache (default: false)

**Sample Prompts:**
```
"How do type converters work in TimeWarp.Nuru?"
"What is IRouteTypeConverter?"
"How do I convert custom types from command line arguments?"
```

#### 16. `get_type_converter_example`
Provides a code example for implementing custom type converters.

**Sample Prompts:**
```
"Show me how to create a custom type converter"
"Give me an IRouteTypeConverter example"
"How do I parse custom types from strings?"
```

#### 17. `get_endpoint_info`
Provides information about the `[NuruRoute]` attribute for auto-discovered routes.

**Parameters:**
- `forceRefresh`: Force refresh from GitHub, bypassing cache (default: false)

**Sample Prompts:**
```
"What is the NuruRoute attribute?"
"How do endpoints work in TimeWarp.Nuru?"
"Explain auto-discovered routes"
```

#### 18. `get_endpoint_example`
Provides a code example for endpoints with nested Handler class.

**Sample Prompts:**
```
"Show me how to use [NuruRoute]"
"Give me an endpoint example"
"How do I create routes with auto-discovery?"
"Show me the Handler class pattern"
```

## Developing from Source

To run the MCP server directly from source code during development:

```json
{
  "servers": {
    "timewarp-nuru-dev": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/TimeWarp.Nuru.Mcp/TimeWarp.Nuru.Mcp.csproj"
      ]
    }
  }
}
```

## Testing the MCP Server

```bash
# Run the test script
cd Tests/TimeWarp.Nuru.Mcp.Tests
./test-mcp-server.cs

# Test individual tools
./test-validate-route.cs
./test-get-syntax.cs
./test-generate-handler.cs
./test-dynamic-examples.cs
```

## Caching

Examples are cached in two layers:
1. **Memory cache**: Immediate access during the session
2. **Disk cache**: Persists between sessions with 1-hour TTL

Cache location:
- **Linux/macOS**: `~/.local/share/TimeWarp.Nuru.Mcp/cache/examples/`
- **Windows**: `%LOCALAPPDATA%\TimeWarp.Nuru.Mcp\cache\examples\`

## Use Cases

### Learning TimeWarp.Nuru
```
"Show me how to create a basic CLI app with TimeWarp.Nuru"
"Get the createbuilder example - I'm familiar with ASP.NET Core"
"Show me the fluent DSL pattern with .WithHandler()"
"Show me the syntax for optional parameters and give me examples"
"How do I add REPL support to my CLI app?"
```

### Building CLI Applications
```
"I want to build a git-like CLI. Show me the basic example and validate my route 'repo init {name} --bare'"
"Help me create routes for a deployment tool. Start with validating 'deploy {env} --version {tag}'"
"Generate a handler for 'backup {source} {dest?} --compress --verbose'"
"Create code for 'migrate {database} --rollback {version?}'"
```

### Adding Interactive Features
```
"Show me the repl-basic example"
"How do I customize REPL key bindings?"
"Get the shell-completion example for adding tab completion"
"Show me how to implement dynamic completion that queries my database"
```

### Route Pattern Development
```
"Show me examples of typed parameters and generate a handler for 'wait {seconds:int}'"
"Get the syntax for catch-all parameters and create code for 'docker {*args}'"
"Generate handlers for 'test {project} --verbose'"
```

### Pipeline Behaviors
```
"How do I add cross-cutting concerns to my CLI?"
"Show me the behavior example for logging"
"How do I create filtered behaviors for authentication?"
"Give me an example of behaviors that only apply to certain routes"
```

### Custom Type Converters
```
"How do I parse custom types from command line arguments?"
"Show me how to create an Environment type converter"
"Get the type converter example"
```

### Endpoints
```
"How do I use [NuruRoute] for auto-discovered commands?"
"Show me the endpoint pattern"
"What's the difference between fluent and endpoints?"
```

### Configuration
```
"Show me the configuration-validation example"
"How do I support --Section:Key=Value style overrides?"
"Get the command-line-overrides example"
```

### Observability & Telemetry
```
"Show me the aspire-host-otel example"
"How do I integrate Nuru with Aspire Dashboard for telemetry?"
"Get the aspire-telemetry example for basic OTLP export"
"How does IHostApplicationBuilder work with NuruAppBuilder?"
"Show me how to use TelemetryBehavior for command tracing"
```

### Testing
```
"Show me the test-output-capture example"
"How do I test colored output in Nuru apps?"
"Get the test-terminal-injection example for DI-based testing"
```

### Code Generation
```
"Validate this route and tell me its specificity: 'npm install {package} --save-dev'"
"What's wrong with my route pattern 'build <target>'"
"Show me the structure of 'kubectl get {resource} --watch --enhanced'"
```

## Building and Publishing

### Build the Package
```bash
dotnet pack -c Release
```

### Publish to NuGet.org
```bash
dotnet nuget push bin/Release/*.nupkg --api-key <your-api-key> --source https://api.nuget.org/v3/index.json
```

## Using from NuGet.org

Once published, configure in your IDE:

```json
{
  "servers": {
    "timewarp-nuru": {
      "type": "stdio",
      "command": "dnx",
      "args": ["mcp", "TimeWarp.Nuru.Mcp"]
    }
  }
}
```

## Future Enhancements

- [ ] Route conflict detection
- [ ] Parameter type validation
- [ ] Pseudo source generators for common CLI patterns (migration assistants, CRUD CLIs)
- [ ] Interactive route builder tool

## Contributing

See the main [TimeWarp.Nuru repository](https://github.com/TimeWarpEngineering/timewarp-nuru) for contribution guidelines.
