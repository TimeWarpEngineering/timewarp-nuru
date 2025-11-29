# Aspire Host with OpenTelemetry and Nuru REPL Sample

This sample demonstrates a Nuru CLI/REPL application sending telemetry (traces, metrics, structured logs) to an OpenTelemetry-compatible dashboard.

## IHostApplicationBuilder Integration

**New in Nuru 3.0**: `NuruAppBuilder` implements `IHostApplicationBuilder`, enabling seamless integration with Aspire and other .NET ecosystem extensions:

```csharp
// NuruAppBuilder implements IHostApplicationBuilder!
NuruAppBuilder builder = NuruApp.CreateBuilder(args, options);

// Aspire-style extension methods work directly:
builder.AddNuruClientDefaults();  // Uses builder.Logging, builder.Services, etc.
```

This means any extension method targeting `IHostApplicationBuilder` (like Aspire's `AddAppDefaults()`) works with Nuru out of the box.

---

## Two Modes

### AppHost Mode (Recommended)

NuruClient is registered as an Aspire-managed project. Telemetry flows automatically to the Aspire Dashboard - both from Aspire-launched commands AND interactive REPL sessions.

```
┌─────────────────────────────────────────────────┐
│ Aspire AppHost                                  │
│  └─ Dashboard (http://localhost:15186)          │
│       └─ OTLP receiver (port 19034)             │
│            └─ Shows ALL telemetry               │
└─────────────────────────────────────────────────┘
          ▲                         ▲
          │ OTLP                    │ OTLP
          │                         │
┌─────────────────────┐   ┌─────────────────────┐
│ Aspire-launched     │   │ Interactive REPL    │
│ (runs "status")     │   │ (your terminal)     │
│ source: nuruclient  │   │ source: nuru-repl   │
└─────────────────────┘   └─────────────────────┘
```

### Docker Mode (Standalone Dashboard)

The standalone Aspire Dashboard running in Docker accepts telemetry from any application.

---

## Running the Sample

### Prerequisites

- .NET 10 SDK
- Docker (optional, for standalone dashboard)

---

## Option 1: AppHost Mode (Recommended)

### Step 1: Start Aspire Host

```bash
cd Samples/AspireHostOtel/AspireHostOtel.AppHost
dotnet run --launch-profile http
```

This:
- Starts the Aspire Dashboard at http://localhost:15186
- Launches NuruClient with `status` command (demonstrates managed telemetry)
- Opens OTLP receiver on port 19034

### Step 2: Run Interactive REPL (separate terminal)

```bash
cd Samples/AspireHostOtel/AspireHostOtel.NuruClient
dotnet run --launch-profile AppHost
```

### Step 3: Interact with the REPL

```
otel> greet Alice
otel> work 500
otel> status
otel> config
```

Each command sends telemetry to the AppHost dashboard. Check:
- **Traces**: See command execution spans with timing
- **Structured Logs**: See log entries with semantic properties
- **Resources**: See both `nuruclient` (Aspire-launched) and `nuru-repl-client` (interactive)

---

## Option 2: Docker Mode

### Step 1: Start the Standalone Dashboard

```bash
docker run --rm -it \
  -p 18888:18888 \
  -p 4317:18889 \
  --name aspire-dashboard \
  mcr.microsoft.com/dotnet/aspire-dashboard:9.0
```

Open http://localhost:18888 in your browser.

### Step 2: Run the NuruClient

```bash
cd Samples/AspireHostOtel/AspireHostOtel.NuruClient
dotnet run --launch-profile Docker
```

---

## Key Concepts

### Aspire Project Registration

The AppHost registers NuruClient as a managed project:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AspireHostOtel_NuruClient>("nuruclient")
  .WithArgs("status");  // Run command (REPL needs interactive console)

builder.Build().Run();
```

Aspire automatically:
- Injects `OTEL_EXPORTER_OTLP_ENDPOINT` pointing to the dashboard
- Shows the resource in the dashboard
- Correlates telemetry from both managed and interactive sessions

### Dual Output Pattern (Console + Telemetry)

Commands use BOTH `Console.WriteLine` for user feedback AND `ILogger<T>` for telemetry:

```csharp
// Console.WriteLine for user feedback (visible in terminal)
Console.WriteLine($"Hello, {request.Name}!");

// ILogger for telemetry (flows to Dashboard via OTLP)
logger.LogInformation("Greeting {Name} at {Timestamp}", request.Name, DateTime.UtcNow);
```

### IHostApplicationBuilder Extensions

NuruAppBuilder implements `IHostApplicationBuilder`, so Aspire-style extensions work:

```csharp
public static IHostApplicationBuilder AddNuruClientDefaults(this IHostApplicationBuilder builder)
{
    builder.Logging.AddOpenTelemetry(logging => { ... });
    builder.Services.AddOpenTelemetry().WithTracing(tracing =>
    {
        tracing.AddSource(builder.Environment.ApplicationName);
    });
    return builder;
}
```

---

## Project Structure

```
AspireHostOtel/
├── AspireHostOtel.AppHost/         # Aspire Host
│   └── Program.cs                  # Registers NuruClient as managed project
├── AspireHostOtel.NuruClient/      # Nuru REPL app with telemetry
│   └── Program.cs                  # Commands with structured logging
└── Overview.md                     # This file
```

## Telemetry Data Collected

### Traces

Each command creates a span:
- **Name**: Command class name (e.g., "GreetCommand")
- **Attributes**: `command.type`, `command.name`
- **Duration**: Execution time in ms

### Structured Logs

Log entries include:
- Semantic properties (e.g., `Name`, `Duration`, `MachineName`)
- Trace/span correlation IDs
- Source context (logger category)

### Example Dashboard Query

```
# Find all greetings for "Alice"
Name = "Alice"

# Find slow work commands
Duration > 1000
```

---

## See Also

- [Aspire Telemetry Sample](../AspireTelemetry/) - Simpler telemetry example
- [REPL Demo](../ReplDemo/) - REPL features without telemetry
- [TimeWarp.Nuru.Telemetry](../../Source/TimeWarp.Nuru.Telemetry/) - Telemetry package
