# UseTelemetry exports extra ActivitySources an app names (for TimeWarp.Taratibu walk spans)

## Description

`UseTelemetry()` builds the tracer provider in generated code and subscribes only to Nuru's own sources: `telemetry-emitter.cs:117-118` emits `AddSource(__activitySource.Name)` (`TimeWarp.Nuru`) and `AddSource("TimeWarp.Nuru.Behavior")`. `NuruTelemetryOptions` (`source/timewarp-nuru/telemetry/nuru-telemetry-options.cs`) has no way to add more.

TimeWarp.Taratibu 1.0.0-beta.17 (PR #77, task 059) now emits per-node walk spans on a BCL `ActivitySource` named **`TimeWarp.Taratibu`** (contract: taratibu `documentation/developer/guides/walk-events.md`). ganda hosts its walks with Taratibu and exports through Nuru's `UseTelemetry()`, so those spans are created but never exported. ganda already works around the limitation for its own spans by putting them all on the `TimeWarp.Nuru` source (`task-lifecycle-telemetry.cs:62`, `claim-pipeline-telemetry.cs:32`, zana `claim-path-telemetry.cs:26`). That workaround can't be applied to a library such as Taratibu.

## Requirements

- Add a supported way for an app to name extra ActivitySources (and Meters, for symmetry) that `UseTelemetry` subscribes to. For example `NuruTelemetryOptions.ActivitySources` / `Meters` (string lists) through `UseTelemetry(o => o.ActivitySources.Add("TimeWarp.Taratibu"))`, with the generator/IR extracting it the same way it extracts the existing options (`TelemetryModel`).
- AOT and trim safe: no reflection or assembly scanning. Source names are literals carried into the generated `AddSource` calls.
- When no extra source is configured, the generated code is unchanged.
- Tests: the generator emits `AddSource` for each configured name; a sample app that names a custom source exports its spans (in-memory exporter).
- Document in the telemetry docs, with Taratibu as the example.

## Notes

- Consumer: timewarp-ganda (follow-up there: subscribe `TimeWarp.Taratibu` once this ships, then let ganda's own spans move off the borrowed `TimeWarp.Nuru` source onto a `TimeWarp.Ganda` source).
- Filed from the ganda OpenCode session 2026-10-31 after the Taratibu 1.0.0-beta.17 release.

## Checklist

- [ ] Options + IR/generator support for extra ActivitySources and Meters
- [ ] Generated AddSource/AddMeter per name; unchanged output when none
- [ ] Tests (generator output + in-memory export)
- [ ] Docs

## Session

- Created: ganda OpenCode session (2026-10-31)
