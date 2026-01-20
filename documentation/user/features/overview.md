# Features

Detailed documentation of TimeWarp.Nuru's features and capabilities.

## Available Features

### [Routing](routing.md)
Web-style routing for CLI applications:
- Route pattern syntax (literals, parameters, options, catch-all)
- Type-safe parameter binding
- Supported types (int, double, DateTime, Guid, etc.)
- Optional parameters
- Complex scenarios and examples

### [Roslyn Analyzer](analyzer.md)
Compile-time validation of route patterns:
- Automatic installation (included with TimeWarp.Nuru package)
- Parse errors (NURU_P###)
- Semantic errors (NURU_S###)
- IDE integration
- Error suppression (when needed)

### [Logging](logging.md)
High-performance logging system:
- Microsoft.Extensions.Logging integration
- Console logging configuration
- Log level filtering
- Third-party provider integration (Serilog, NLog, Application Insights)
- Zero overhead when disabled

### [Auto-Help](auto-help.md)
Automatic help generation:
- Command-level help
- Parameter descriptions
- Option documentation
- Auto-generated usage examples

### [Built-in Routes](built-in-routes.md)
Routes auto-registered by `NuruApp.CreateBuilder()`:
- `--version, -v` - Display version with commit info
- `--check-updates` - Check GitHub for newer versions
- `--interactive, -i` - Enter REPL mode
- Configuration via [`NuruAppOptions`](../reference/nuru-app-options.md)

### [Configuration](configuration.md)
Microsoft.Extensions.Configuration and DependencyInjection integration:
- Automatic loading from JSON files and environment variables
- Strongly-typed options with `IOptions<T>`
- Command-line configuration overrides (`--Section:Key=Value`)
- Startup validation with `IValidateOptions<T>`
- Service registration via `ConfigureServices`

### [Output Handling](output-handling.md)
Console stream management:
- stdout vs stderr separation
- Automatic JSON serialization
- Structured data output
- Piping and scripting support
- Best practices

### [Shell Completion](shell-completion.md)
Automatic tab completion for CLI applications:
- One-line enablement
- Cross-platform support (bash, zsh, PowerShell, fish)
- Command, option, and enum completion
- Static (zero runtime overhead)
- Type-aware parameter hints

### [Terminal Abstractions](terminal-abstractions.md)
Testable I/O and colored output:
- IConsole/ITerminal interfaces for testable CLI apps
- TestTerminal for unit testing output capture
- Fluent ANSI color extensions (`"text".Red().Bold()`)
- Lightweight Spectre.Console alternative
- Full CSS named colors support

### [Terminal Widgets](widgets.md)
Built-in widgets for formatted terminal output:
- **Rule**: Horizontal dividers with optional centered text
- **Panel**: Bordered boxes with headers and content
- **Table**: Columnar data with alignment and borders
- Fluent builder pattern for all widgets
- Full AOT and testability support

## Feature Highlights

| Feature | Benefit | Learn More |
|---------|---------|------------|
| 🎯 Web-Style Routing | Familiar syntax from web dev | [Routing](routing.md) |
| 🛡️ Compile-Time Validation | Catch errors before runtime | [Analyzer](analyzer.md) |
| ⚡ Zero-Overhead Logging | Optional, high-performance | [Logging](logging.md) |
| 📖 Auto-Help | No manual documentation | [Auto-Help](auto-help.md) |
| 🔧 Built-in Routes | Version, updates, REPL out-of-box | [Built-in Routes](built-in-routes.md) |
| ⚙️ Configuration | IOptions<T> and DI integration | [Configuration](configuration.md) |
| ⌨️ Shell Completion | Tab completion in all shells | [Shell Completion](shell-completion.md) |
| 🎨 Colored Output | Testable ANSI colors | [Terminal Abstractions](terminal-abstractions.md) |
| 📊 Terminal Widgets | Tables, panels, rules | [Widgets](widgets.md) |
| 🔒 Type Safety | Strong typing throughout | [Routing](routing.md#type-safety) |
| 🚀 Native AOT | Fast startup, small binaries | [Deployment](../guides/deployment.md) |

## Related Documentation

- **[Getting Started](../getting-started.md)** - Quick start guide
- **[Use Cases](../use-cases.md)** - Real-world patterns
- **[Tools](../tools/)** - Supporting tools (MCP Server)
- **[Reference](../reference/)** - Technical specs
