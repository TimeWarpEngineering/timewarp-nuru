## TimeWarp.Nuru Specific Instructions

### Architecture

TimeWarp.Nuru is a CLI framework using source generators for AOT-compatible route handling.

Key components:
- **timewarp-nuru-core**: Core runtime library
- **timewarp-nuru-analyzers**: Roslyn analyzers and source generators
- **timewarp-nuru**: Main package combining core + analyzers

### Source Generator Pattern

Routes are defined via fluent API and processed at compile time:
```csharp
NuruApp.CreateBuilder(args)
  .Map("greet {name}").WithHandler((string name) => $"Hello, {name}!")
  .AsCommand().Done()
  .Build();
```

The generator intercepts `Map()` calls and emits optimized matching code.

### Testing Pattern

Tests use the Jaribu test framework with `TestTerminal` for output verification:
```csharp
using TestTerminal terminal = new();
NuruCoreApp app = NuruApp.CreateBuilder([])
  .UseTerminal(terminal)
  .Map("test").WithHandler(() => "output")
  .AsCommand().Done()
  .Build();

await app.RunAsync(["test"]);
terminal.OutputContains("output").ShouldBeTrue();
```

### MCP Server

The `timewarp-nuru` MCP server provides route validation and pattern examples.
See `source/timewarp-nuru-mcp-server/` for implementation.
