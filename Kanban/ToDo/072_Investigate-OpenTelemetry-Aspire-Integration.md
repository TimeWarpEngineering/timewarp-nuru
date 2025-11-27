# Investigate OpenTelemetry and Aspire Integration

## Description

Research and design the `AddOpenTelemetry()` extension method for TimeWarp.Nuru to enable observability and .NET Aspire integration. This task focuses on investigation and design before implementation.

## Parent

071_Implement-Static-Factory-Builder-API (reference to OpenTelemetry in feature matrix)

## Requirements

- Investigate OpenTelemetry SDK integration patterns for CLI applications
- Research .NET Aspire dashboard compatibility for CLI tools
- Determine appropriate telemetry data: traces, metrics, logs
- Design API that follows existing `Add*` extension method patterns
- Consider AOT compatibility implications

## Checklist

### Research
- [ ] Review OpenTelemetry .NET SDK documentation
- [ ] Investigate `System.Diagnostics.ActivitySource` for tracing
- [ ] Research Aspire dashboard requirements for CLI apps
- [ ] Examine how other CLI frameworks handle telemetry
- [ ] Evaluate OTLP exporter options (gRPC vs HTTP)

### Design
- [ ] Define `AddOpenTelemetry()` API signature and options
- [ ] Determine what activities/spans to create (command execution, route matching, etc.)
- [ ] Design metrics collection (command duration, success/failure counts)
- [ ] Plan integration with existing logging infrastructure
- [ ] Document AOT compatibility considerations

### Prototype
- [ ] Create proof-of-concept implementation
- [ ] Test with Aspire dashboard
- [ ] Validate minimal overhead when telemetry disabled

## Notes

- OpenTelemetry integration was listed in the feature matrix for `CreateBuilder` (task 071)
- Should integrate seamlessly with .NET Aspire's service defaults pattern
- Consider making this a separate NuGet package: `TimeWarp.Nuru.OpenTelemetry`
- Activity names should follow semantic conventions where applicable
