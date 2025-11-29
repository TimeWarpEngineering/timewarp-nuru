# Update MCP With AspireHostOtel Sample

## Description

Update the MCP server's examples.json manifest and README.md to include the new AspireHostOtel sample. This sample demonstrates critical Nuru 3.0 features that AI assistants need to know about.

## Requirements

The AspireHostOtel sample demonstrates:
- IHostApplicationBuilder integration (new in Nuru 3.0)
- .NET 10 file-based apps (runfiles) with Aspire
- OpenTelemetry with Aspire Dashboard
- Dual output pattern (Console.WriteLine for user + ILogger for telemetry)
- TelemetryBehavior pipeline registration

## Checklist

### Update examples.json
- [ ] Add `aspire-host-otel` example entry pointing to `Samples/AspireHostOtel/nuru-client.cs`
- [ ] Include appropriate tags: `aspire`, `opentelemetry`, `telemetry`, `ihostapplicationbuilder`, `runfiles`, `dashboard`
- [ ] Set difficulty to `advanced`

### Update MCP README.md
- [ ] Add `aspire-host-otel` to the Available Examples section under `get_example`
- [ ] Add sample prompt for Aspire Host OTEL integration
- [ ] Update "Adding Interactive Features" use case section to mention telemetry integration

## Notes

The AspireHostOtel sample is more comprehensive than the simpler AspireTelemetry sample:
- AspireTelemetry: Basic OTLP export to Aspire Dashboard
- AspireHostOtel: Full Aspire AppHost integration with C# runfile support

Key code patterns to highlight:
```csharp
// NuruAppBuilder implements IHostApplicationBuilder
NuruAppBuilder builder = NuruApp.CreateBuilder(args, options);
builder.AddNuruClientDefaults();  // Aspire-style extensions work directly

// TelemetryBehavior registration (AOT-compatible)
services.AddMediator(options =>
{
  options.PipelineBehaviors = [typeof(TelemetryBehavior<,>)];
});
```
