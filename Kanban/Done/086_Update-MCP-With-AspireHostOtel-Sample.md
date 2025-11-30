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
- [x] Add `aspire-host-otel` example entry pointing to `Samples/AspireHostOtel/nuru-client.cs`
- [x] Include appropriate tags: `aspire`, `opentelemetry`, `telemetry`, `ihostapplicationbuilder`, `runfiles`, `dashboard`
- [x] Set difficulty to `advanced`

### Update MCP README.md
- [x] Add `aspire-host-otel` to the Available Examples section under `get_example`
- [x] Add sample prompt for Aspire Host OTEL integration
- [x] Add "Observability & Telemetry" use case section
- [x] Add "Testing" use case section
- [x] Add testing and pipeline middleware examples to Available Examples

## Implementation Notes

Updated examples.json:
- Added `aspire-host-otel` example with full tags and advanced difficulty

Updated README.md:
- Added 3 new example categories to Available Examples section
- Added 2 new sample prompts for Aspire integration
- Added "Observability & Telemetry" use case section with 5 prompts
- Added "Testing" use case section with 3 prompts

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
