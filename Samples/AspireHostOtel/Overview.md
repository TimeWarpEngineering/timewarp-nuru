# Aspire Host with OpenTelemetry and Nuru REPL Sample

This sample demonstrates .NET Aspire Host running an OpenTelemetry Collector and Dashboard, with a Nuru CLI/REPL application running externally in its own terminal. Telemetry (traces, metrics, structured logs) flows from the CLI to the Aspire Dashboard.

## Architecture

```
┌─────────────────────────────────────────────────┐
│ Terminal 1: Aspire AppHost                      │
│  ├─ Aspire Dashboard (traces, metrics, logs)    │
│  └─ OpenTelemetry Collector (OTLP receiver)     │
│       └─ Listens on localhost:19034             │
└─────────────────────────────────────────────────┘
                      ▲
                      │ OTLP (gRPC)
                      │
┌─────────────────────────────────────────────────┐
│ Terminal 2: NuruClient (external)               │
│  └─ REPL with telemetry                         │
│       ├─ Console.WriteLine for user feedback    │
│       └─ ILogger for telemetry to Dashboard     │
└─────────────────────────────────────────────────┘
```

**Why external?** CLI/REPL apps need direct console access for user interaction. Aspire-orchestrated console apps don't have an interactive terminal.

## Key Concepts

### 1. Aspire Host Orchestration

The AppHost project uses `CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector` to:
- Run an OpenTelemetry Collector container
- Expose an OTLP endpoint (localhost:19034) for external apps
- Display telemetry in the Aspire Dashboard

```csharp
// AppHost only runs the collector - NuruClient runs separately
builder.AddOpenTelemetryCollector("otel-collector")
  .WithAppForwarding();
```

### 2. Dual Output Pattern (Console + Telemetry)

Commands use BOTH `Console.WriteLine` for user feedback AND `ILogger<T>` for telemetry:

```csharp
// Console.WriteLine for user feedback (visible in their terminal)
Console.WriteLine($"Hello, {request.Name}!");

// ILogger for telemetry (flows to Aspire Dashboard via OTLP)
logger.LogInformation("Greeting {Name} at {Timestamp}", request.Name, DateTime.UtcNow);
```

This pattern ensures:
- Users see immediate feedback in their terminal
- Telemetry flows to Aspire Dashboard for monitoring
- Structured logs are searchable by property values

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

### Terminal 1: Start Aspire Host

```bash
cd Samples/AspireHostOtel
dotnet run --project AspireHostOtel.AppHost --launch-profile http
```

The Aspire Dashboard URL will be printed to the console. Open it in your browser.

### Terminal 2: Run the NuruClient

Open a **new terminal** and run the client with the OTLP endpoint:

```bash
cd Samples/AspireHostOtel
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:19034 dotnet run --project AspireHostOtel.NuruClient
```

### Interact with the REPL

In Terminal 2, you'll see the REPL prompt. Try these commands:

```
otel> greet Alice
otel> work 500
otel> status
otel> config
```

Each command will:
1. Print output to your terminal (via Console.WriteLine)
2. Send telemetry to Aspire Dashboard (via ILogger)

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
