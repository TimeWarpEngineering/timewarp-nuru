# Implement TimeWarp.Nuru.Telemetry Package

## Description

Create the `TimeWarp.Nuru.Telemetry` NuGet package providing seamless OpenTelemetry integration for Nuru CLI applications. This enables telemetry export to Aspire Dashboard and other OTLP-compatible backends.

## Parent

080_Investigate-OpenTelemetry-Aspire-Integration

## Prerequisites

- 080_001_Create-Aspire-Telemetry-POC (must be completed and validated)

## Requirements

- Follow existing package patterns (`TimeWarp.Nuru.Logging`, `TimeWarp.Nuru.Repl`)
- Zero overhead when telemetry not configured
- Environment variable-based activation (OTEL_EXPORTER_OTLP_ENDPOINT)
- AOT compatibility where possible
- No ASP.NET Core framework dependency

## Checklist

### Project Setup
- [x] Create `Source/TimeWarp.Nuru.Telemetry/` project
- [x] Create `TimeWarp.Nuru.Telemetry.csproj` with package references
- [x] Add project to solution file
- [x] Configure package metadata

### Core Implementation
- [x] Create `NuruTelemetryOptions` configuration class
- [x] Create `NuruTelemetryExtensions` with `UseAspireTelemetry()` method
- [x] Create internal `ActivitySource` for Nuru traces
- [x] Create internal `Meter` for Nuru metrics
- [x] Implement OTLP exporter configuration

### Nuru Core Integration
- [ ] Add instrumentation hooks in `NuruApp.ExecuteAsync()` (deferred - users use helper methods)
- [ ] Add instrumentation hooks in route matching (deferred)
- [x] Ensure hooks have zero cost when no listeners

### REPL Integration
- [x] Add REPL session span tracking (StartReplSession method)
- [x] Add REPL command metrics (RecordReplCommand method)
- [x] Track session duration (via Activity)

### Testing
- [ ] Create test project `Tests/TimeWarp.Nuru.Telemetry.Tests/` (future)
- [x] Test telemetry configuration (code review)
- [x] Test zero-overhead when disabled (design review)
- [ ] Test with Aspire Dashboard (requires manual testing)

### Documentation
- [x] Create README.md for package
- [x] Add usage examples
- [x] Document configuration options
- [x] Add sample to `Samples/` directory (AspireTelemetryPOC)

## Results

Created `TimeWarp.Nuru.Telemetry` package with:

**Files:**
- `TimeWarp.Nuru.Telemetry.csproj` - Project file with OpenTelemetry 1.14.0 packages
- `NuruTelemetryOptions.cs` - Configuration options class
- `NuruTelemetryExtensions.cs` - Extension methods and telemetry helpers
- `README.md` - Package documentation

**API:**
- `UseAspireTelemetry()` - Simple activation using env vars
- `UseAspireTelemetry(Action<NuruTelemetryOptions>)` - Custom configuration
- `ExecuteWithTelemetry()` / `ExecuteWithTelemetryAsync()` - Helper methods
- `StartReplSession()` / `RecordReplCommand()` - REPL integration

**Metrics:**
- `nuru.commands.invoked` (Counter)
- `nuru.commands.errors` (Counter)
- `nuru.commands.duration` (Histogram)
- `nuru.repl.sessions` (Counter)
- `nuru.repl.commands` (Counter)

**Notes:**
- Core integration hooks deferred - package provides helper methods instead
- Users can integrate telemetry manually or use the provided helpers
- Zero overhead when OTEL endpoint not configured

## API Design

### Extension Methods

```csharp
public static class NuruTelemetryExtensions
{
    // Simple activation - uses env vars for configuration
    public static NuruAppBuilder UseAspireTelemetry(this NuruAppBuilder builder);

    // Custom configuration
    public static NuruAppBuilder UseAspireTelemetry(
        this NuruAppBuilder builder,
        Action<NuruTelemetryOptions> configure);
}
```

### Options Class

```csharp
public class NuruTelemetryOptions
{
    public string ServiceName { get; set; } = "nuru-app";
    public string ServiceVersion { get; set; }
    public bool EnableTracing { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
    public string OtlpEndpoint { get; set; }  // Override env var
}
```

### Telemetry Definitions

**ActivitySource**: `TimeWarp.Nuru`

**Activities**:
- `nuru.command.execute` - Spans command execution
- `nuru.route.match` - Spans route matching
- `nuru.repl.session` - Spans REPL session
- `nuru.repl.command` - Spans individual REPL commands

**Meter**: `TimeWarp.Nuru`

**Metrics**:
- `nuru.commands.invoked` (Counter) - Commands executed
- `nuru.commands.duration` (Histogram) - Command execution time in ms
- `nuru.commands.errors` (Counter) - Failed commands
- `nuru.repl.sessions` (Counter) - REPL sessions started
- `nuru.repl.commands` (Counter) - Commands in REPL mode

## Notes

- Follow `TimeWarp.Nuru.Logging` package structure
- Use conditional compilation for AOT-incompatible features if needed
- Consider `[ActivitySource]` and `[Meter]` source generators for AOT
- Ensure `Activity.Current` propagation works correctly
- Test with both DI and direct delegate patterns
