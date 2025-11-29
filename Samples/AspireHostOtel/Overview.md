# Aspire Host with OpenTelemetry and Nuru REPL Sample

This sample demonstrates .NET Aspire Host orchestrating an OpenTelemetry Collector and a Nuru console REPL application. All telemetry (traces, metrics, structured logs) flows to the Aspire Dashboard.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ Aspire AppHost                                              │
│  ├─ OpenTelemetry Collector (OTLP receiver)                 │
│  ├─ Aspire Dashboard (traces, metrics, logs)                │
│  └─ Nuru Console App (REPL with telemetry)                  │
│       └─ Uses structured ILogger, not Console.WriteLine     │
└─────────────────────────────────────────────────────────────┘
```

## Key Concepts

### 1. Aspire Host Orchestration

The AppHost project uses `CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector` to:
- Run an OpenTelemetry Collector container
- Automatically forward telemetry from all resources to the collector
- Display telemetry in the Aspire Dashboard

```csharp
var collector = builder.AddOpenTelemetryCollector("otel-collector")
  .WithAppForwarding(); // Auto-forward all resources

builder.AddProject<Projects.AspireHostOtel_NuruClient>("nuru-client");
```

### 2. Structured Logging (Critical!)

Commands use `ILogger<T>` with semantic properties instead of `Console.WriteLine`:

```csharp
// CORRECT: Structured logging - appears in Aspire Dashboard
logger.LogInformation("Greeting {Name} at {Timestamp}", request.Name, DateTime.UtcNow);

// WRONG: Console output - does NOT flow to Aspire Dashboard
Console.WriteLine($"Hello, {request.Name}!");
```

This ensures:
- Logs are searchable by property values in Aspire Dashboard
- Logs include trace correlation IDs
- Logs are properly formatted as structured OTLP log records

### 3. Nuru Telemetry Integration

The Nuru app uses `UseTelemetry()` for one-line telemetry setup:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .UseTelemetry()  // OTLP export, tracing, metrics
  .ConfigureServices(services =>
  {
    services.AddMediator();
    services.AddSingleton<IPipelineBehavior<T, Unit>, TelemetryBehavior<T, Unit>>();
  })
  .Map<GreetCommand>(pattern: "greet {name}")
  .AddReplSupport(options => { ... })
  .Build();
```

## Running the Sample

### Prerequisites

- .NET 10 SDK
- Docker (for OpenTelemetry Collector container)

### Start the Application

```bash
cd Samples/AspireHostOtel
dotnet run --project AspireHostOtel.AppHost
```

### Open Aspire Dashboard

The dashboard URL is printed to the console. Open it in your browser.

### Interact with the REPL

In another terminal, find the NuruClient console or interact through Aspire Dashboard console:

```
otel> greet Alice
otel> work 500
otel> status
otel> config
```

### View Telemetry

In Aspire Dashboard:
- **Traces**: See command execution spans with timing
- **Logs**: View structured log entries with searchable properties
- **Metrics**: Monitor command counts and duration histograms

## Project Structure

```
AspireHostOtel/
├── AspireHostOtel.AppHost/         # Aspire Host (orchestrator)
│   ├── AspireHostOtel.AppHost.csproj
│   └── Program.cs                  # Adds collector and NuruClient
├── AspireHostOtel.NuruClient/      # Nuru REPL app with telemetry
│   ├── AspireHostOtel.NuruClient.csproj
│   └── Program.cs                  # Commands with structured logging
└── Overview.md                     # This file
```

## Key Packages

| Package | Purpose |
|---------|---------|
| `Aspire.Hosting.AppHost` | Aspire orchestration |
| `CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector` | OTEL Collector resource |
| `TimeWarp.Nuru` | CLI framework |
| `TimeWarp.Nuru.Repl` | REPL support |
| `TimeWarp.Nuru.Telemetry` | Telemetry integration |

## Telemetry Data Collected

### Traces (Activities)

Each command execution creates a span with:
- **Name**: Command class name (e.g., "GreetCommand", "WorkCommand")
- **Tags**: `command.type`, `command.name`, error info
- **Duration**: Execution time

### Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `nuru.commands.invoked` | Counter | Commands executed |
| `nuru.commands.errors` | Counter | Failed commands |
| `nuru.commands.duration` | Histogram | Execution time in ms |

### Structured Logs

Log entries include:
- Semantic properties (e.g., `Name`, `Duration`, `MachineName`)
- Trace correlation IDs
- Log level and timestamp
- Source context (logger category)

## Why Structured Logging?

The Aspire Dashboard can filter and search logs by property values:

```
# Find all greetings for "Alice"
Name = "Alice"

# Find slow work commands
Duration > 1000
```

This is impossible with unstructured `Console.WriteLine` output.

## See Also

- [Aspire Telemetry Sample](../AspireTelemetry/) - Standalone dashboard mode
- [REPL Demo](../ReplDemo/) - REPL features without telemetry
- [TimeWarp.Nuru.Telemetry](../../Source/TimeWarp.Nuru.Telemetry/) - Telemetry package
- [CommunityToolkit.Aspire](https://github.com/CommunityToolkit/Aspire) - Aspire extensions
