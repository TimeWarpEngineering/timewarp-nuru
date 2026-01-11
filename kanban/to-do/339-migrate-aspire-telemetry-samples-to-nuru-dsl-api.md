# Migrate _aspire telemetry samples to Nuru DSL API

## Description

Migrate `samples/_aspire-host-otel/` and `samples/_aspire-telemetry/` samples to use 
`NuruApp.CreateBuilder()` and Nuru interfaces.
Once complete, rename folders to `NN-aspire-*/` or consolidate.

## Dependencies

Requires:
- timewarp-nuru-telemetry library to be migrated away from Mediator
- Possibly timewarp-nuru-repl (aspire-host-otel uses REPL)

## Samples

- `_aspire-host-otel/nuru-client.cs` - Aspire host with OpenTelemetry
- `_aspire-telemetry/aspire-telemetry.cs` - Telemetry integration

## Checklist

- [ ] Verify timewarp-nuru-telemetry library is migrated to Nuru interfaces
- [ ] Update `_aspire-host-otel/nuru-client.cs` - remove Mediator, use CreateBuilder
- [ ] Update `_aspire-telemetry/aspire-telemetry.cs`
- [ ] Verify Aspire integration still works
- [ ] Verify OpenTelemetry traces are emitted correctly
- [ ] Rename folders to numbered convention (e.g., `14-aspire-telemetry/`)

## Notes

The aspire-host-otel sample uses:
- Mediator with TelemetryBehavior pipeline
- REPL functionality
- OpenTelemetry exports

This is a more complex migration that requires multiple library updates.
