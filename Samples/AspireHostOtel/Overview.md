# Aspire Host with OpenTelemetry and Nuru REPL Sample

This sample demonstrates a Nuru CLI/REPL application sending telemetry (traces, metrics, structured logs) to an OpenTelemetry-compatible dashboard.

## Two Dashboard Modes

### Docker Mode (Recommended for CLI Apps)

The standalone Aspire Dashboard running in Docker accepts telemetry from **any application** - it doesn't need to manage or launch your app. This is ideal for external CLI tools.

```
┌─────────────────────────────────────────────────┐
│ Docker: Standalone Aspire Dashboard             │
│  └─ OTLP receiver (port 4317)                   │
│       └─ Accepts telemetry from ANY app         │
└─────────────────────────────────────────────────┘
                      ▲
                      │ OTLP (gRPC)
                      │
┌─────────────────────────────────────────────────┐
│ Terminal: NuruClient (external CLI)             │
│  └─ REPL with telemetry                         │
│       ├─ Console.WriteLine for user feedback    │
│       └─ ILogger for telemetry to Dashboard     │
└─────────────────────────────────────────────────┘
```

### AppHost Mode (For Aspire-Orchestrated Apps Only)

The Aspire AppHost Dashboard is designed for apps that Aspire **launches and manages** as part of a distributed application. It only shows telemetry from resources it orchestrates.

```
┌─────────────────────────────────────────────────┐
│ Terminal 1: Aspire AppHost                      │
│  └─ Aspire Dashboard                            │
│       ├─ Built-in OTLP receiver (port 19034)    │
│       └─ Shows telemetry from managed resources │
└─────────────────────────────────────────────────┘
                      ▲
                      │ OTLP (gRPC)
                      │ ⚠️ External apps may not appear!
┌─────────────────────────────────────────────────┐
│ Terminal 2: NuruClient (external)               │
│  └─ NOT managed by Aspire                       │
└─────────────────────────────────────────────────┘
```

**Important**: The AppHost Dashboard is optimized for showing telemetry from apps Aspire launches. External CLI apps sending telemetry to the AppHost endpoint may not reliably appear in the dashboard.

## Running the Sample

### Prerequisites

- .NET 10 SDK
- Docker (for recommended mode)

---

## Option 1: Docker Mode (Recommended)

This is the recommended approach for CLI applications. The standalone dashboard accepts telemetry from any source.

### Step 1: Start the Standalone Dashboard

```bash
docker run --rm -it \
  -p 18888:18888 \
  -p 4317:18889 \
  --name aspire-dashboard \
  mcr.microsoft.com/dotnet/aspire-dashboard:9.0
```

This exposes:
- **Port 18888**: Dashboard UI
- **Port 4317**: OTLP gRPC endpoint (mapped from container's 18889)

Open http://localhost:18888 in your browser.

### Step 2: Run the NuruClient

```bash
cd Samples/AspireHostOtel/AspireHostOtel.NuruClient
dotnet run --launch-profile Docker
```

Or manually set the endpoint:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
export OTEL_SERVICE_NAME=nuru-repl-client
dotnet run
```

### Step 3: Interact with the REPL

```
otel> greet Alice
otel> work 500
otel> status
otel> config
```

Each command sends telemetry to the Docker dashboard where you can see it immediately.

---

## Option 2: AppHost Mode (Advanced)

Use this mode if you want to explore the Aspire AppHost pattern. Note that the AppHost dashboard is designed for apps Aspire manages directly.

### Step 1: Start Aspire Host

```bash
cd Samples/AspireHostOtel/AspireHostOtel.AppHost
dotnet run --launch-profile http
```

The Aspire Dashboard URL will be printed to the console.

### Step 2: Run the NuruClient

```bash
cd Samples/AspireHostOtel/AspireHostOtel.NuruClient
dotnet run --launch-profile AppHost
```

**Note**: External apps like NuruClient may not reliably appear in the AppHost dashboard since they aren't managed resources.

---

## Key Concepts

### Dual Output Pattern (Console + Telemetry)

Commands use BOTH `Console.WriteLine` for user feedback AND `ILogger<T>` for telemetry:

```csharp
// Console.WriteLine for user feedback (visible in their terminal)
Console.WriteLine($"Hello, {request.Name}!");

// ILogger for telemetry (flows to Dashboard via OTLP)
logger.LogInformation("Greeting {Name} at {Timestamp}", request.Name, DateTime.UtcNow);
```

This pattern ensures:
- Users see immediate feedback in their terminal
- Telemetry flows to the Dashboard for monitoring
- Structured logs are searchable by property values

### Nuru Telemetry Integration

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

## Project Structure

```
AspireHostOtel/
├── AspireHostOtel.AppHost/         # Aspire Host (for AppHost mode)
│   ├── AspireHostOtel.AppHost.csproj
│   └── Program.cs
├── AspireHostOtel.NuruClient/      # Nuru REPL app with telemetry
│   ├── AspireHostOtel.NuruClient.csproj
│   └── Program.cs                  # Commands with structured logging
└── Overview.md                     # This file
```

## Key Packages

| Package | Purpose |
|---------|---------|
| `Aspire.Hosting.AppHost` | Aspire orchestration (AppHost mode) |
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

- [Aspire Telemetry Sample](../AspireTelemetry/) - Another telemetry example
- [REPL Demo](../ReplDemo/) - REPL features without telemetry
- [TimeWarp.Nuru.Telemetry](../../Source/TimeWarp.Nuru.Telemetry/) - Telemetry package
