# Logging System

TimeWarp.Nuru uses Microsoft.Extensions.Logging for a high-performance, industry-standard logging system that integrates with any .NET logging provider.

## Quick Start

By default, logging is disabled (zero overhead). To enable console logging:

```csharp
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
    .UseConsoleLogging()  // Enable console logging
    .Map("test")
      .WithHandler(() => Console.WriteLine("Test"))
      .AsCommand()
      .Done()
    .Build();

return await app.RunAsync(args);
```

## Installation

The logging extensions (`UseConsoleLogging`, `UseDebugLogging`, `ConfigureLogging`) are part of the main `TimeWarp.Nuru` package (namespace `TimeWarp.Nuru`). No separate logging package is needed:

```bash
dotnet add package TimeWarp.Nuru
```

Or in a script file:
```csharp
#!/usr/bin/env dotnet run
#:package TimeWarp.Nuru
```

## Log Levels

The framework supports standard Microsoft.Extensions.Logging levels:

| Level | Value | Description |
|-------|-------|-------------|
| `Trace` | 0 | Most detailed information, shows internal operations |
| `Debug` | 1 | Detailed debugging information |
| `Information` | 2 | General informational messages |
| `Warning` | 3 | Warning messages for potential issues |
| `Error` | 4 | Error messages for failures |
| `Critical` | 5 | Critical failures requiring immediate attention |
| `None` | 6 | No logging output |

## Configuration Methods

### Basic Console Logging

```csharp
// Default: Information level and above
.UseConsoleLogging()

// Custom minimum level
.UseConsoleLogging(LogLevel.Debug)

// Trace level for debugging
.UseDebugLogging()  // Equivalent to UseConsoleLogging(LogLevel.Trace)
```

### Custom Configuration

```csharp
.UseConsoleLogging(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Debug)
        .AddFilter("TimeWarp.Nuru.Parser", LogLevel.Debug)
        .AddFilter("TimeWarp.Nuru.Lexer", LogLevel.Trace)
        .AddFilter("TimeWarp.Nuru.Compiler", LogLevel.Warning);
})
```

### Integration with Other Providers

#### Serilog

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

NuruApp app = NuruApp.CreateBuilder()
    .UseLogging(new SerilogLoggerFactory(Log.Logger))
    .Map("test")
      .WithHandler(() => Console.WriteLine("Test"))
      .AsCommand()
      .Done()
    .Build();
```

#### NLog

```csharp
using NLog.Extensions.Logging;

ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddNLog();
});

NuruApp app = NuruApp.CreateBuilder()
    .UseLogging(loggerFactory)
    .Map("test")
      .WithHandler(() => Console.WriteLine("Test"))
      .AsCommand()
      .Done()
    .Build();
```

#### Application Insights

```csharp
using Microsoft.Extensions.Logging.ApplicationInsights;

ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddApplicationInsights("InstrumentationKey");
});

NuruApp app = NuruApp.CreateBuilder()
    .UseLogging(loggerFactory)
    .Map("test")
      .WithHandler(() => Console.WriteLine("Test"))
      .AsCommand()
      .Done()
    .Build();
```

## Logged Components

The framework logs key operations across several components:

| Component | Logger category | Messages that are logged |
|-----------|-----------------|--------------------------|
| Parser | `TimeWarp.Nuru.Parser` | `ParsingPattern` (1100), `DumpingTokens` (1101), `DumpingAst` (1102) |
| Lexer | `TimeWarp.Nuru.Lexer` | `StartingLexicalAnalysis` (1050), `CompletedLexicalAnalysis` (1051), `DumpingTokens` (1101) |
| Compiler | `TimeWarp.Nuru.Compiler` | `SettingBooleanOptionParameter` (1103) |

There is no `TimeWarp.Nuru.CommandResolver` or `TimeWarp.Nuru.Parsing` category. Route matching does not write these log events. `ParsingLoggerMessages` still defines registration and matcher messages (events 1000–1001 and 1200–1355), and nothing calls them.

## Example Output

### Debug and trace

These lines are the messages the parser, lexer, and compiler actually write. The parser and lexer lines are one pattern (`add`, `{`, `x`, `:`, `double`, `}`, `{`, `y`, `:`, `double`, `}` is 11 tokens). The compiler line is a different pattern that has a boolean option. Token and AST dumps are omitted. Their text is whatever `DumpTokens` and `DumpAst` return.

```
09:15:23 dbug: TimeWarp.Nuru.Parser[1100]
      Parsing pattern: 'add {x:double} {y:double}'
09:15:23 trce: TimeWarp.Nuru.Lexer[1050]
      Starting lexical analysis of: 'add {x:double} {y:double}'
09:15:23 trce: TimeWarp.Nuru.Lexer[1051]
      Lexical analysis complete. Generated 11 tokens
09:15:23 dbug: TimeWarp.Nuru.Compiler[1103]
      Setting boolean option parameter name to: 'verbose'
```

## Performance Characteristics

### Zero-Allocation Logging

The framework uses `LoggerMessage.Define` for all internal logging, providing:
- **Zero allocations** when logging is disabled
- **Minimal allocations** when logging is enabled (only for dynamic values)
- **Compiled delegates** for maximum performance

### Zero Overhead When Disabled

When no logger is configured:
- Uses `NullLoggerFactory.Instance`
- All logging calls are no-ops
- No performance impact whatsoever

## Filtering by Component

### Using Microsoft.Extensions.Logging Filters

```csharp
.UseConsoleLogging(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Information)
        .AddFilter("TimeWarp.Nuru.Parser", LogLevel.Debug)
        .AddFilter("TimeWarp.Nuru.Lexer", LogLevel.Trace)
        .AddFilter("TimeWarp.Nuru.Compiler", LogLevel.None);
})
```

### Using appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "TimeWarp.Nuru": "Debug",
      "TimeWarp.Nuru.Parser": "Debug",
      "TimeWarp.Nuru.Lexer": "Trace",
      "TimeWarp.Nuru.Compiler": "Warning"
    }
  }
}
```

Load configuration:
```csharp
IConfigurationRoot configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConfiguration(configuration.GetSection("Logging"));
    builder.AddConsole();
});

NuruApp app = NuruApp.CreateBuilder()
    .UseLogging(loggerFactory)
    .Build();
```

## Troubleshooting Common Scenarios

### "Why isn't my route matching?"

Route matching does not log. To see how the pattern was tokenized and parsed:

```csharp
.UseConsoleLogging(builder =>
{
    builder
        .AddFilter("TimeWarp.Nuru.Parser", LogLevel.Debug)
        .AddFilter("TimeWarp.Nuru.Lexer", LogLevel.Trace);
})
```

### "How are my routes being parsed?"

Filter the parser. `ParsingPattern` is Debug (event 1100). Lexer detail is Trace.

```csharp
.UseConsoleLogging(builder =>
{
    builder.AddFilter("TimeWarp.Nuru.Parser", LogLevel.Debug);
})
```

### "What routes are registered?"

Nothing logs route registration. The registration messages in `ParsingLoggerMessages` (events 1000 and 1001) have no callers. `ParsingPattern` logs each pattern as the parser reads it, at Debug:

```csharp
.UseConsoleLogging(LogLevel.Debug)
```

### "I want to see everything!"

Enable trace for all components:

```csharp
.UseDebugLogging()  // or .UseConsoleLogging(LogLevel.Trace)
```

## Custom Logger Implementation

You can create custom loggers by implementing `ILoggerProvider`:

```csharp
public class CustomLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new CustomLogger(categoryName);
    }

    public void Dispose() { }
}

public class CustomLogger : ILogger
{
    private readonly string categoryName;

    public CustomLogger(string categoryName)
    {
        this.categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId,
        TState state, Exception exception,
        Func<TState, Exception, string> formatter)
    {
        // Custom logging logic here
        Console.WriteLine($"[{categoryName}] {formatter(state, exception)}");
    }
}

// Use it:
ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddProvider(new CustomLoggerProvider());
});

NuruApp app = NuruApp.CreateBuilder()
    .UseLogging(loggerFactory)
    .Build();
```

## Migration from Old System

`NURU_LOG_*` and `NURU_DEBUG` are not read. Use the logging API. There is no matcher category to replace `NURU_LOG_MATCHER`.

| Old variable (not read) | Replacement |
|-----|-----|
| `NURU_LOG_LEVEL=Debug` | `.UseConsoleLogging(LogLevel.Debug)` |
| `NURU_LOG_MATCHER=Trace` | No matcher logger. Trace `TimeWarp.Nuru.Parser` and `TimeWarp.Nuru.Lexer` |
| `NURU_LOG_PARSER=Debug` | `.AddFilter("TimeWarp.Nuru.Parser", LogLevel.Debug)` |
| `NURU_DEBUG=true` | `.UseDebugLogging()` |

## Tips

1. **Production**: Use no logging (default) or `Warning` level for best performance
2. **Development**: Use `Information` or `Debug` level
3. **Troubleshooting**: Use `Trace` level temporarily for specific components
4. **Integration**: Use structured logging providers like Serilog for production monitoring
5. **Performance**: The default (no logging) has zero overhead - safe for high-performance scenarios
6. **Testing**: Use `InMemoryLoggerProvider` to capture logs in tests