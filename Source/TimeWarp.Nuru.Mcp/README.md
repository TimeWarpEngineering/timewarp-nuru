# TimeWarp.Nuru MCP Server

An MCP (Model Context Protocol) server that provides tools for working with TimeWarp.Nuru, a route-based CLI framework for .NET.

## Features

### Available Tools

#### 1. `get_example`
Fetches TimeWarp.Nuru code examples from the GitHub repository with intelligent caching.

**Parameters:**
- `name`: Example name (basic, async, console-logging, serilog, mediator, delegates) or 'list' to see all
- `forceRefresh`: Force refresh from GitHub, bypassing cache (default: false)

**Sample Prompts:**
```
"Show me the basic TimeWarp.Nuru example"
"Get the serilog logging example from TimeWarp.Nuru"
"List all available TimeWarp.Nuru examples"
"Refresh and show me the mediator pattern example"
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
Generates handler code from a route pattern.

**Parameters:**
- `pattern`: The route pattern to generate a handler for
- `useMediator`: Whether to generate mediator pattern code (default: false)

**Sample Prompts:**
```
"Generate a handler for the route 'deploy {env} --dry-run'"
"Create a mediator handler for 'backup {source} {dest?}'"
"Generate code for 'test {project} --verbose --filter {pattern}'"
"Show me the handler signature for 'docker {*args}'"
```

#### 9. `get_random_number`
Generates a random number between specified bounds (demo tool).

**Parameters:**
- `min`: Minimum value (inclusive, default: 0)
- `max`: Maximum value (exclusive, default: 100)

## Developing Locally

To test this MCP server from source code without building a package:

```json
{
  "servers": {
    "timewarp-nuru": {
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
"Get the async command example and explain how it works"
"What's the difference between the delegates and mediator examples?"
"Show me the syntax for optional parameters and give me examples"
```

### Building CLI Applications
```
"I want to build a git-like CLI. Show me the basic example and validate my route 'repo init {name} --bare'"
"Help me create routes for a deployment tool. Start with validating 'deploy {env} --version {tag}'"
"Generate a handler for 'backup {source} {dest?} --compress --verbose'"
"Create mediator pattern code for 'migrate {database} --rollback {version?}'"
```

### Route Pattern Development
```
"Show me examples of typed parameters and generate a handler for 'wait {seconds:int}'"
"Get the syntax for catch-all parameters and create code for 'docker {*args}'"
"Generate both direct and mediator handlers for 'test {project} --verbose'"
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

- [ ] Dynamic example discovery from GitHub
- [ ] Route pattern syntax documentation tool  
- [ ] Pseudo source generators for common CLI patterns
- [ ] Route conflict detection
- [ ] Parameter type validation

## Contributing

See the main [TimeWarp.Nuru repository](https://github.com/TimeWarpEngineering/timewarp-nuru) for contribution guidelines.