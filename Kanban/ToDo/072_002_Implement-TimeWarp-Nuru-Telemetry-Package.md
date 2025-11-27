# Implement TimeWarp.Nuru.Telemetry Package

## Description

Create the `TimeWarp.Nuru.Telemetry` NuGet package providing seamless OpenTelemetry integration for Nuru CLI applications. This enables telemetry export to Aspire Dashboard and other OTLP-compatible backends.

## Parent

072_Investigate-OpenTelemetry-Aspire-Integration

## Prerequisites

- 072_001_Create-Aspire-Telemetry-POC (must be completed and validated)

## Requirements

- Follow existing package patterns (`TimeWarp.Nuru.Logging`, `TimeWarp.Nuru.Repl`)
- Zero overhead when telemetry not configured
- Environment variable-based activation (OTEL_EXPORTER_OTLP_ENDPOINT)
- AOT compatibility where possible
- No ASP.NET Core framework dependency

## Checklist

### Project Setup
- [ ] Create `Source/TimeWarp.Nuru.Telemetry/` project
- [ ] Create `TimeWarp.Nuru.Telemetry.csproj` with package references
- [ ] Add project to solution file
- [ ] Configure package metadata

### Core Implementation
- [ ] Create `NuruTelemetryOptions` configuration class
- [ ] Create `NuruTelemetryExtensions` with `UseAspireTelemetry()` method
- [ ] Create internal `ActivitySource` for Nuru traces
- [ ] Create internal `Meter` for Nuru metrics
- [ ] Implement OTLP exporter configuration

### Nuru Core Integration
- [ ] Add instrumentation hooks in `NuruApp.ExecuteAsync()`
- [ ] Add instrumentation hooks in route matching
- [ ] Ensure hooks have zero cost when no listeners

### REPL Integration
- [ ] Add REPL session span tracking
- [ ] Add REPL command metrics
- [ ] Track session duration

### Testing
- [ ] Create test project `Tests/TimeWarp.Nuru.Telemetry.Tests/`
- [ ] Test telemetry configuration
- [ ] Test zero-overhead when disabled
- [ ] Test with Aspire Dashboard

### Documentation
- [ ] Create README.md for package
- [ ] Add usage examples
- [ ] Document configuration options
- [ ] Add sample to `Samples/` directory

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
