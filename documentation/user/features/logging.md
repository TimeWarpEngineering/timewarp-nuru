# Logging System

TimeWarp.Nuru uses Microsoft.Extensions.Logging for a high-performance, industry-standard logging system that integrates with any .NET logging provider.

## Quick Start

By default, logging is disabled (zero overhead). To enable console logging:

```csharp
using TimeWarp.Nuru;
using TimeWarp.Nuru.Logging;

NuruApp app = new NuruAppBuilder()
    .UseConsoleLogging()  // Enable console logging
    .AddRoute("test", () => Console.WriteLine("Test"))
    .Build();

return await app.RunAsync(args);
```

## Installation

Add the logging package to enable console output:

```bash
dotnet add package TimeWarp.Nuru.Logging
```

Or in a script file:
```csharp
#!/usr/bin/env dotnet run
#:package TimeWarp.Nuru
#:package TimeWarp.Nuru.Logging
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
        .AddFilter("TimeWarp.Nuru.CommandResolver", LogLevel.Trace)
        .AddFilter("TimeWarp.Nuru.Parsing", LogLevel.Warning);
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

NuruApp app = new NuruAppBuilder()
    .UseLogging(new SerilogLoggerFactory(Log.Logger))
    .AddRoute("test", () => Console.WriteLine("Test"))
    .Build();
```

#### NLog

```csharp
using NLog.Extensions.Logging;

ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddNLog();
});

NuruApp app = new NuruAppBuilder()
    .UseLogging(loggerFactory)
    .AddRoute("test", () => Console.WriteLine("Test"))
    .Build();
```

#### Application Insights

```csharp
using Microsoft.Extensions.Logging.ApplicationInsights;

ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddApplicationInsights("InstrumentationKey");
});

NuruApp app = new NuruAppBuilder()
    .UseLogging(loggerFactory)
    .AddRoute("test", () => Console.WriteLine("Test"))
    .Build();
```

## Logged Components

The framework logs key operations across several components:

| Component | Namespace | Key Messages |
|-----------|-----------|--------------|
| Registration | TimeWarp.Nuru | Route registration during startup |
| Parsing | TimeWarp.Nuru.Parsing | Route pattern parsing and compilation |
| Command Resolution | TimeWarp.Nuru.CommandResolver | Route matching and argument extraction |
| Parameter Binding | TimeWarp.Nuru.Binding | Binding arguments to method parameters |
| Type Conversion | TimeWarp.Nuru.TypeConversion | Converting string arguments to typed parameters |

## Example Output

### Debug Level

```
09:15:23 info: TimeWarp.Nuru.NuruAppBuilder[1000]
      Starting route registration
09:15:23 dbug: TimeWarp.Nuru.NuruAppBuilder[1001]
      Registering route: 'add {x:double} {y:double}'
09:15:23 dbug: TimeWarp.Nuru.NuruAppBuilder[1001]
      Registering route: 'subtract {x:double} {y:double}'
09:15:23 info: TimeWarp.Nuru.CommandResolver.RouteBasedCommandResolver[1200]
      Resolving command: 'add 5 3'
09:15:23 dbug: TimeWarp.Nuru.CommandResolver.RouteBasedCommandResolver[1204]
      ✓ Matched route: 'add {x:double} {y:double}'
```

### Trace Level

```
09:15:23 trce: TimeWarp.Nuru.CommandResolver.RouteBasedCommandResolver[1202]
      [1/2] Checking route: 'add {x:double} {y:double}'
09:15:23 trce: TimeWarp.Nuru.CommandResolver.RouteBasedCommandResolver[1300]
      Matching 3 positional segments against 3 arguments
09:15:23 trce: TimeWarp.Nuru.CommandResolver.RouteBasedCommandResolver[1307]
      Attempting to match 'add' against add
09:15:23 trce: TimeWarp.Nuru.CommandResolver.RouteBasedCommandResolver[1310]
      Literal 'add' matched
09:15:23 trce: TimeWarp.Nuru.CommandResolver.RouteBasedCommandResolver[1307]
      Attempting to match '5' against {x:double}
09:15:23 trce: TimeWarp.Nuru.CommandResolver.RouteBasedCommandResolver[1309]
      Extracted parameter 'x' = '5'
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
        // Show all command resolution details
        .AddFilter("TimeWarp.Nuru.CommandResolver", LogLevel.Trace)
        // Suppress parsing logs
        .AddFilter("TimeWarp.Nuru.Parsing", LogLevel.None);
})
```

### Using appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "TimeWarp.Nuru": "Debug",
      "TimeWarp.Nuru.CommandResolver": "Trace",
      "TimeWarp.Nuru.Parsing": "Warning"
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

NuruApp app = new NuruAppBuilder()
    .UseLogging(loggerFactory)
    .Build();
```

## Troubleshooting Common Scenarios

### "Why isn't my route matching?"

Enable trace logging for command resolution:

```csharp
.UseConsoleLogging(builder =>
{
    builder.AddFilter("TimeWarp.Nuru.CommandResolver", LogLevel.Trace);
})
```

### "How are my routes being parsed?"

Enable debug logging for parsing:

```csharp
.UseConsoleLogging(builder =>
{
    builder.AddFilter("TimeWarp.Nuru.Parsing", LogLevel.Debug);
})
```

### "What routes are registered?"

Registration logs are at Information level by default:

```csharp
.UseConsoleLogging(LogLevel.Information)
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

NuruApp app = new NuruAppBuilder()
    .UseLogging(loggerFactory)
    .Build();
```

## Migration from Old System

If you were using the old environment variable-based logging:

| Old | New |
|-----|-----|
| `NURU_LOG_LEVEL=Debug` | `.UseConsoleLogging(LogLevel.Debug)` |
| `NURU_LOG_MATCHER=Trace` | `.AddFilter("TimeWarp.Nuru.CommandResolver", LogLevel.Trace)` |
| `NURU_LOG_PARSER=Debug` | `.AddFilter("TimeWarp.Nuru.Parsing", LogLevel.Debug)` |
| `NURU_DEBUG=true` | `.UseDebugLogging()` |

## Tips

1. **Production**: Use no logging (default) or `Warning` level for best performance
2. **Development**: Use `Information` or `Debug` level
3. **Troubleshooting**: Use `Trace` level temporarily for specific components
4. **Integration**: Use structured logging providers like Serilog for production monitoring
5. **Performance**: The default (no logging) has zero overhead - safe for high-performance scenarios
6. **Testing**: Use `InMemoryLoggerProvider` to capture logs in tests