# Aspire Host with OpenTelemetry and Nuru REPL Sample

This sample demonstrates a Nuru CLI/REPL application sending telemetry (traces, metrics, structured logs) to an OpenTelemetry-compatible dashboard.

## File-Based Apps (Runfiles)

Both the AppHost and NuruClient are implemented as **.NET 10 file-based apps** (runfiles) - single `.cs` files that run directly without a `.csproj`:

```bash
cd samples/_aspire-host-otel

# Run the AppHost using Aspire CLI
aspire run

# Run the NuruClient (in separate terminal)
./nuru-client.cs
```

The `aspire run` command uses the `.aspire/settings.json` configuration to locate the AppHost.

Each runfile specifies its launch profile in the shebang:

```csharp
#!/usr/bin/env -S dotnet run --launch-profile http --      # apphost.cs
#!/usr/bin/env -S dotnet run --launch-profile AppHost --   # nuru-client.cs
```

Both runfiles share a single `Properties/launchSettings.json` with multiple profiles.

## IHostApplicationBuilder Integration

**New in Nuru 3.0**: `NuruAppBuilder` implements `IHostApplicationBuilder`, enabling seamless integration with Aspire and other .NET ecosystem extensions:

```csharp
// NuruAppBuilder implements IHostApplicationBuilder!
NuruAppBuilder builder = NuruApp.CreateBuilder(args, options);

// Aspire-style extension methods work directly.
// You can write your own extensions targeting IHostApplicationBuilder:
builder.AddMyCustomDefaults();  // Uses builder.Logging, builder.Services, etc.
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
- Aspire CLI (`dotnet tool install -g aspire`)
- Aspire 13.0+

### Step 1: Start Aspire Host

```bash
cd samples/_aspire-host-otel
aspire run
```

Output:
```
     AppHost:  apphost.cs
   Dashboard:  http://localhost:15186/login?t=<token>
        Logs:  ~/.aspire/cli/logs/apphost-<pid>-<timestamp>.log

               Press CTRL+C to stop the apphost and exit.
```

This:
- Starts the Aspire Dashboard (URL shown with auth token)
- Launches NuruClient with `status` command (demonstrates managed telemetry)
- Opens OTLP receiver on port 19034

### Step 2: Run Interactive REPL (separate terminal)

```bash
cd samples/_aspire-host-otel
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
#!/usr/bin/env -S dotnet run --launch-profile http --
#:sdk Aspire.AppHost.Sdk@13.0.0

#pragma warning disable ASPIRECSHARPAPPS001

var builder = DistributedApplication.CreateBuilder(args);

builder.AddCSharpApp("nuruclient", "./nuru-client.cs")
  .WithArgs("status");

await builder.Build().RunAsync();
```

Aspire automatically:
- Injects `OTEL_EXPORTER_OTLP_ENDPOINT` pointing to the dashboard
- Shows the resource in the dashboard
- Correlates telemetry from both managed and interactive sessions

### Shared Launch Settings

Both runfiles use a single `Properties/launchSettings.json` with multiple profiles:

```json
{
  "profiles": {
    "http": {
      "environmentVariables": {
        "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL": "http://localhost:19034",
        ...
      }
    },
    "AppHost": {
      "environmentVariables": {
        "OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:19034",
        "OTEL_SERVICE_NAME": "nuru-repl-client"
      }
    }
  }
}
```

The shebang in each runfile specifies which profile to use:
- `apphost.cs` uses `--launch-profile http`
- `nuru-client.cs` uses `--launch-profile AppHost`

### Dual Output Pattern (Console + Telemetry)

Commands use BOTH `Console.WriteLine` for user feedback AND `ILogger<T>` for telemetry:

```csharp
// Console.WriteLine for user feedback (visible in terminal)
Console.WriteLine($"Hello, {request.Name}!");

// ILogger for telemetry (flows to Dashboard via OTLP)
logger.LogInformation("Greeting {Name} at {Timestamp}", request.Name, DateTime.UtcNow);
```

### IHostApplicationBuilder Extensions

NuruAppBuilder implements `IHostApplicationBuilder`, so you can write Aspire-style extensions:

```csharp
// Example: Create your own extension method targeting IHostApplicationBuilder
public static IHostApplicationBuilder AddMyDefaults(this IHostApplicationBuilder builder)
{
    builder.Logging.AddOpenTelemetry(logging => { ... });
    builder.Services.AddOpenTelemetry().WithTracing(tracing =>
    {
        tracing.AddSource(builder.Environment.ApplicationName);
    });
    return builder;
}
```

Note: Nuru's built-in telemetry is configured via `UseTelemetry()` in the fluent builder.

---

## Project Structure

```
AspireHostOtel/
├── apphost.cs              # Aspire Host runfile
├── nuru-client.cs          # Nuru REPL runfile
├── Properties/
│   └── launchSettings.json # Shared launch profiles
└── Overview.md             # This file
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
- [REPL Demo](../13-repl/) - REPL features without telemetry
- [Telemetry Source Generator](../../source/timewarp-nuru-analyzers/generators/emitters/telemetry-emitter.cs) - Source-generated telemetry infrastructure
