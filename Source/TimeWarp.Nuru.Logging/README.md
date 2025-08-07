# TimeWarp.Nuru.Logging

Console logging extensions for TimeWarp.Nuru CLI framework.

## Usage

```csharp
var app = new NuruAppBuilder()
    .UseConsoleLogging() // Default: Information level
    .AddRoute("test", () => Console.WriteLine("Test"))
    .Build();
```

## Options

- `UseConsoleLogging()` - Default console logging (Information level)
- `UseConsoleLogging(LogLevel.Debug)` - Custom log level
- `UseDebugLogging()` - Trace level logging for debugging
- `UseConsoleLogging(configure => ...)` - Custom configuration