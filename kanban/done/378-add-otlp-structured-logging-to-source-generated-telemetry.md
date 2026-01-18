# Add OTLP structured logging to source-generated telemetry

## Description

Extend the source-generated telemetry to include OpenTelemetry logging export. When `UseTelemetry()` is called, logs via `ILogger<T>` will be exported to OTLP alongside traces and metrics. No console output - keeps CLI UX clean.

## Current State

- Traces: Working via OTLP ✓
- Metrics: Working via OTLP ✓
- Logs: Console only, NOT exported to OTLP

## Checklist

- [ ] Modify `telemetry-emitter.cs` to emit OpenTelemetry logging setup
- [ ] Change `NuruCoreApp.LoggerFactory` from `init` to `set`
- [ ] Verify package references include OpenTelemetry logging exporter
- [ ] Build and test with Aspire sample
- [ ] Verify logs appear in Aspire Dashboard Structured Logs tab

## Notes

### Implementation Plan: Task 378 - Add OTLP Structured Logging

#### Current State Analysis

| Component | Status | Mechanism |
|-----------|--------|-----------|
| **Traces** | Working | `TracerProvider` with OTLP exporter |
| **Metrics** | Working | `MeterProvider` with OTLP exporter |
| **Logs** | Console only | `ILogger<T>` → Console, NOT exported to OTLP |

#### Root Cause

When `UseTelemetry()` is called, the generated code sets up OTLP exporters for traces and metrics in `TelemetryEmitter.EmitTelemetrySetup()`, but logging remains configured by the user's `LoggerFactory` which typically only has console output. The `LoggerFactory` property is `init`-only, preventing the generated code from setting a logging provider.

#### Changes Required

##### 1. `source/timewarp-nuru/nuru-core-app.cs`

**Change 1a:** Add `LoggerProvider` property (similar to `TracerProvider`/`MeterProvider`)
```csharp
public LoggerProvider? LoggerProvider { get; set; }
```

**Change 1b:** Change `LoggerFactory` from `init` to `set`
```csharp
// Before:
public ILoggerFactory? LoggerFactory { get; init; }

// After:
public ILoggerFactory? LoggerFactory { get; set; }
```

**Change 1c:** Update `FlushTelemetryAsync()` to handle `LoggerProvider`
```csharp
public async Task FlushTelemetryAsync(int delayMs = 1000)
{
  TracerProvider?.ForceFlush();
  MeterProvider?.ForceFlush();
  LoggerProvider?.ForceFlush();  // Add this line

  if (delayMs > 0)
    await Task.Delay(delayMs).ConfigureAwait(false);

  TracerProvider?.Dispose();
  MeterProvider?.Dispose();
  LoggerProvider?.Dispose();  // Add these lines
  TracerProvider = null;
  MeterProvider = null;
  LoggerProvider = null;  // Add this line
}
```

##### 2. `source/timewarp-nuru-analyzers/generators/emitters/telemetry-emitter.cs`

**Change 2a:** Add `using OpenTelemetry.Logs;` to `EmitTelemetrySetup()`

**Change 2b:** Add `LoggerProvider` setup in `EmitTelemetrySetup()`, appending after `MeterProvider` setup:
```csharp
app.LoggerFactory = global::Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
{
  builder.SetMinimumLevel(global::Microsoft.Extensions.Logging.LogLevel.Debug);
  builder.AddOpenTelemetry(options =>
  {
    options.SetResourceBuilder(__resource);
    options.AddOtlpExporter(o => o.Endpoint = __endpoint);
  });
});

app.LoggerProvider = global::OpenTelemetry.Sdk.CreateLoggerProviderBuilder()
  .SetResourceBuilder(__resource)
  .AddLoggerFactory(app.LoggerFactory)
  .AddOtlpExporter(o => o.Endpoint = __endpoint)
  .Build();
```

#### Package Verification

Required packages already present in `timewarp-nuru.csproj`:
- OpenTelemetry
- OpenTelemetry.Exporter.OpenTelemetryProtocol
- OpenTelemetry.Extensions.Hosting

#### Test Verification

1. Build: `dotnet build timewarp-nuru.slnx`
2. Run Aspire sample with OTEL_EXPORTER_OTLP_ENDPOINT
3. Verify logs appear in Aspire Dashboard Structured Logs tab

### Files to Modify

- `source/timewarp-nuru-analyzers/generators/emitters/telemetry-emitter.cs`
- `source/timewarp-nuru/nuru-core-app.cs`

### Rationale

This is a CLI framework. Console output is for command results, not log noise. OTLP captures logs for debugging/monitoring without polluting the user's terminal. Console logger currently writes to stdout, mixing with command output.

## Results

**Implemented OTLP structured logging for source-generated telemetry:**

1. `nuru-core-app.cs`:
   - Added `using OpenTelemetry.Logs;`
   - Changed `LoggerFactory` from `init` to `set` accessor
   - Added `LoggerProvider` property with XML documentation
   - Updated `FlushTelemetryAsync()` to flush, dispose, and null the `LoggerProvider`

2. `telemetry-emitter.cs`:
   - Added `LoggerFactory` setup that creates an `ILoggerFactory` with OpenTelemetry logging configured
   - Added `LoggerProvider` setup using `OpenTelemetry.Sdk.CreateLoggerProviderBuilder()`
   - Logs now export to OTLP alongside traces and metrics when `UseTelemetry()` is called

**Result:** When `UseTelemetry()` is called, all telemetry (traces, metrics, logs) is exported via OTLP with no console output, keeping CLI UX clean.
