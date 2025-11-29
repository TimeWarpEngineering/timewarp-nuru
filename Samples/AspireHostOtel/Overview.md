# Aspire Host with OpenTelemetry and Nuru REPL Sample

This sample demonstrates a Nuru CLI/REPL application sending telemetry (traces, metrics, structured logs) to an OpenTelemetry-compatible dashboard.

## File-Based Apps (Runfiles)

Both the AppHost and NuruClient are implemented as **.NET 10 file-based apps** (runfiles) - single `.cs` files that run directly without a `.csproj`:

```bash
# Run the AppHost
./AppHost/apphost.cs

# Run the NuruClient
./NuruClient/nuru-client.cs
```

This demonstrates Aspire 13's support for single-file AppHosts using `#:sdk` directives.

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

## Architecture

```
┌─────────────────────────────────────────────────┐
│ Aspire AppHost (apphost.cs)                     │
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

---

## Running the Sample

### Prerequisites

- .NET 10 SDK
- Aspire 13.0+

### Step 1: Start Aspire Host

```bash
cd Samples/AspireHostOtel/AppHost
./apphost.cs
```

This:
- Starts the Aspire Dashboard at http://localhost:15186
- Launches NuruClient with `status` command (demonstrates managed telemetry)
- Opens OTLP receiver on port 19034

### Step 2: Run Interactive REPL (separate terminal)

```bash
cd Samples/AspireHostOtel/NuruClient
./nuru-client.cs
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

## Key Concepts

### Aspire C# App Registration

The AppHost registers NuruClient as a managed C# file-based app:

```csharp
#!/usr/bin/dotnet --
#:sdk Aspire.AppHost.Sdk@13.0.0

#pragma warning disable ASPIRECSHARPAPPS001

var builder = DistributedApplication.CreateBuilder(args);

builder.AddCSharpApp("nuruclient", "../NuruClient/nuru-client.cs")
  .WithArgs("status");

await builder.Build().RunAsync();
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
├── AppHost/
│   ├── apphost.cs              # Aspire Host runfile
│   └── Properties/
│       └── launchSettings.json # Dashboard configuration
├── NuruClient/
│   ├── nuru-client.cs          # Nuru REPL runfile
│   └── Properties/
│       └── launchSettings.json # OTLP endpoint configuration
└── Overview.md                 # This file
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

- [Aspire 13 File-Based App Support](https://aspire.dev/whats-new/aspire-13/#c-file-based-app-support)
- [REPL Demo](../ReplDemo/) - REPL features without telemetry
- [TimeWarp.Nuru.Telemetry](../../Source/TimeWarp.Nuru.Telemetry/) - Telemetry package
