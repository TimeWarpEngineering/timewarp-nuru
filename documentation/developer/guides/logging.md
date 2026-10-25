# Logging System

TimeWarp.Nuru uses Microsoft.Extensions.Logging. You configure it one way: call `AddLogging(...)` inside `ConfigureServices(...)`, then inject `ILogger<T>` where you need it.

## Quick Start

By default, logging is disabled (zero overhead). To log to the console:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeWarp.Mediator;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
    .ConfigureServices(services => services.AddLogging(logging => logging
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information)))
    .Map<WorkCommand>()
    .Build();

return await app.RunAsync(args);

[NuruRoute("work", Description = "Do some work")]
public sealed class WorkCommand : ICommand<Unit>
{
    public sealed class Handler(ILogger<WorkCommand> logger) : ICommandHandler<WorkCommand, Unit>
    {
        public Task<Unit> Handle(WorkCommand command, CancellationToken cancellationToken)
        {
            logger.LogInformation("Working");
            return Unit.Task;
        }
    }
}
```

The source generator reads the `AddLogging` lambda at build time and turns it into the app's logger factory. Every `ILogger<T>` the app injects comes from that factory.

## Installation

`TimeWarp.Nuru` brings `Microsoft.Extensions.Logging` and `Microsoft.Extensions.Logging.Console`, so `AddConsole()` needs no extra package:

```bash
dotnet add package TimeWarp.Nuru
```

Or in a script file:
```csharp
#!/usr/bin/env dotnet run
#:package TimeWarp.Nuru
```

Other providers (Serilog, NLog, Application Insights) need their own package.

## Where ILogger&lt;T&gt; Can Be Injected

| Injection point | Logger source |
|-----------------|---------------|
| Endpoint handler constructor (`Handler(ILogger<MyCommand> logger)`) | the `AddLogging` factory |
| Pipeline behavior constructor (`AddBehavior(typeof(MyBehavior))`) | the `AddLogging` factory |
| Any service, with `UseMicrosoftDependencyInjection()` | the runtime container |

A delegate handler parameter of type `ILogger<T>` (`.WithHandler((ILogger<X> logger) => ...)`) is not resolved by the source-generated DI yet. Use an endpoint handler or a behavior.

When you inject `ILogger<T>` and the app has no `AddLogging(...)`, the build warns with `NURU_H007` and the logger discards every message.

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

## Configuration

Everything goes inside the `AddLogging` lambda, with the standard `ILoggingBuilder` methods:

```csharp
.ConfigureServices(services => services.AddLogging(logging => logging
    .AddConsole()
    .SetMinimumLevel(LogLevel.Debug)
    .AddFilter("Microsoft", LogLevel.Warning)
    .AddFilter("MyApp.Data", LogLevel.Trace)))
```

### Logging to stderr

Console logging writes to stdout by default. To keep stdout for command output, send every level to stderr:

```csharp
.ConfigureServices(services => services.AddLogging(logging => logging
    .AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace)))
```

### Other Providers

#### Serilog

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

NuruApp app = NuruApp.CreateBuilder()
    .ConfigureServices(services => services.AddLogging(logging => logging.AddSerilog()))
    .Map<WorkCommand>()
    .Build();

try
{
    return await app.RunAsync(args);
}
finally
{
    Log.CloseAndFlush();
}
```

Packages: `Serilog`, `Serilog.Extensions.Logging`, and the sinks you use.

#### NLog

```csharp
using NLog.Extensions.Logging;

NuruApp app = NuruApp.CreateBuilder()
    .ConfigureServices(services => services.AddLogging(logging => logging.AddNLog()))
    .Map<WorkCommand>()
    .Build();
```

#### Custom Provider

Implement `ILoggerProvider` and add it with `AddProvider`:

```csharp
NuruApp app = NuruApp.CreateBuilder()
    .ConfigureServices(services => services.AddLogging(logging =>
        logging.AddProvider(new CustomLoggerProvider())))
    .Map<WorkCommand>()
    .Build();

public sealed class CustomLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new CustomLogger(categoryName);
    public void Dispose() { }
}

public sealed class CustomLogger(string categoryName) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine($"[{categoryName}] {formatter(state, exception)}");
    }
}
```

The generator copies the `AddLogging` lambda into generated code, so a type the lambda names (such as `CustomLoggerProvider`) must be accessible from the app's own assembly.

## Nuru's Own Log Messages

Nuru parses route patterns in the source generator, at build time. The parser, lexer and compiler messages below are written there, not in your running app, so `AddLogging` does not show them. To see them, call the parser yourself with a logger factory:

```csharp
using Microsoft.Extensions.Logging;
using TimeWarp.Nuru;

using ILoggerFactory loggerFactory = LoggerFactory.Create(logging => logging
    .AddConsole()
    .AddFilter("TimeWarp.Nuru.Parser", LogLevel.Debug)
    .AddFilter("TimeWarp.Nuru.Lexer", LogLevel.Trace));

CompiledRoute route = PatternParser.Parse("add {x:double} {y:double}", loggerFactory);
```

| Component | Logger category | Messages that are logged |
|-----------|-----------------|--------------------------|
| Parser | `TimeWarp.Nuru.Parser` | `ParsingPattern` (1100), `DumpingTokens` (1101), `DumpingAst` (1102) |
| Lexer | `TimeWarp.Nuru.Lexer` | `StartingLexicalAnalysis` (1050), `CompletedLexicalAnalysis` (1051), `DumpingTokens` (1101) |
| Compiler | `TimeWarp.Nuru.Compiler` | `SettingBooleanOptionParameter` (1103) |

There is no `TimeWarp.Nuru.CommandResolver` or `TimeWarp.Nuru.Parsing` category. Route matching does not write log events. `ParsingLoggerMessages` still defines registration and matcher messages (events 1000–1001 and 1200–1355), and nothing calls them.

## Performance Characteristics

When the app has no `AddLogging(...)`, injected loggers come from `NullLoggerFactory.Instance` and every logging call is a no-op. With `AddLogging(...)`, the factory is a static field built once from the lambda, with no runtime container.

Nuru's internal logging uses `LoggerMessage.Define`, which allocates nothing when the level is disabled.

## Migration

### Removed builder methods

`UseLogging(ILoggerFactory)`, `UseConsoleLogging(...)`, `UseDebugLogging()` and `ConfigureLogging(...)` were removed. The source generator never supported them: any app that called one failed to build with `NURU_S999`. Use `AddLogging` in `ConfigureServices`:

| Removed | Replacement |
|---------|-------------|
| `.UseConsoleLogging()` | `.ConfigureServices(s => s.AddLogging(b => b.AddConsole()))` |
| `.UseConsoleLogging(LogLevel.Debug)` | `.ConfigureServices(s => s.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug)))` |
| `.UseDebugLogging()` | `.ConfigureServices(s => s.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace)))` |
| `.UseConsoleLogging(b => ...)` / `.ConfigureLogging(b => ...)` | `.ConfigureServices(s => s.AddLogging(b => ...))` |
| `.UseLogging(loggerFactory)` | move the factory's configuration into `AddLogging(b => ...)` |

### Environment variables

`NURU_LOG_*` and `NURU_DEBUG` are not read. Configure levels in `AddLogging`.

| Old variable (not read) | Replacement |
|-----|-----|
| `NURU_LOG_LEVEL=Debug` | `.SetMinimumLevel(LogLevel.Debug)` in `AddLogging` |
| `NURU_LOG_MATCHER=Trace` | No matcher logger |
| `NURU_LOG_PARSER=Debug` | `PatternParser.Parse(pattern, loggerFactory)` (see above) |
| `NURU_DEBUG=true` | `.SetMinimumLevel(LogLevel.Trace)` in `AddLogging` |

## Tips

1. **Production**: Use no logging (default) or `Warning` level for best performance
2. **Development**: Use `Information` or `Debug` level
3. **Troubleshooting**: Use `Trace` level temporarily, filtered to the categories you care about
4. **Integration**: Use structured logging providers like Serilog for production monitoring
5. **Testing**: Add a capturing `ILoggerProvider` with `AddProvider` to assert on log entries
