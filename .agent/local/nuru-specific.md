## TimeWarp.Nuru Specific Instructions

### Architecture

TimeWarp.Nuru is a CLI framework using source generators for AOT-compatible route handling.

Key components:
- **timewarp-nuru-core**: Core runtime library
- **timewarp-nuru-analyzers**: Roslyn analyzers and source generators
- **timewarp-nuru**: Main package combining core + analyzers

### Project Structure

```
/source/                        # Main library source code
/tests/
  test-apps/                    # Integration test applications
  timewarp-nuru-core-tests/     # Core unit tests
  ci-tests/                     # CI multi-mode test runner
/benchmarks/                    # Performance benchmarks
/samples/                       # Example implementations
/runfiles/                      # Build and utility runfiles
/kanban/                        # Task tracking
```

### Route Pattern Support

- Literal routes: `status`, `version`
- Parameters: `greet {name}`, `delay {ms:int}`
- Optional parameters: `deploy {env} {tag?}`
- Options: `build --config {mode}`, `git commit -m {message}`
- Catch-all: `docker {*args}`

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

### REPL Testing Limitations

- Claude cannot run interactive REPL tests due to non-interactive shell
- All REPL functionality tests must be run by human user
- Claude can verify compilation but cannot test interactive features (arrow keys, etc.)

### Code Style

- `dotnet format` runs automatically in build
- All warnings treated as errors
- **NO TRAILING WHITESPACE** - build fails with RCS1037

### Building AOT Executables

```bash
# Standard AOT build
dotnet publish -c Release -r linux-x64 -p:PublishAot=true
```

### MCP Server

The `timewarp-nuru` MCP server provides route validation and pattern examples.
See `source/timewarp-nuru-mcp-server/` for implementation.
