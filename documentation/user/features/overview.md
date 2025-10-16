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

### [Output Handling](output-handling.md)
Console stream management:
- stdout vs stderr separation
- Automatic JSON serialization
- Structured data output
- Piping and scripting support
- Best practices

## Feature Highlights

| Feature | Benefit | Learn More |
|---------|---------|------------|
| 🎯 Web-Style Routing | Familiar syntax from web dev | [Routing](routing.md) |
| 🛡️ Compile-Time Validation | Catch errors before runtime | [Analyzer](analyzer.md) |
| ⚡ Zero-Overhead Logging | Optional, high-performance | [Logging](logging.md) |
| 📖 Auto-Help | No manual documentation | [Auto-Help](auto-help.md) |
| 🔒 Type Safety | Strong typing throughout | [Routing](routing.md#type-safety) |
| 🚀 Native AOT | Fast startup, small binaries | [Deployment](../guides/deployment.md) |

## Related Documentation

- **[Getting Started](../getting-started.md)** - Quick start guide
- **[Use Cases](../use-cases.md)** - Real-world patterns
- **[Tools](../tools/)** - Supporting tools (MCP Server)
- **[Reference](../reference/)** - Technical specs
