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

## Goal (implementer restatement)

`UseTelemetry()` only subscribes its generated tracer and meter providers to Nuru's own
sources, so spans a library such as TimeWarp.Taratibu puts on its own `ActivitySource` are
created and then dropped. Give apps a compile-time way to name extra sources and meters.

Acceptance criteria:

- `NuruTelemetryOptions` gains `ActivitySources` and `Meters` (string collections). An app
  writes `UseTelemetry(o => o.ActivitySources.Add("TimeWarp.Taratibu"))`.
- The extractor reads each `Add(...)` whose argument is a compile-time constant string (a
  literal or a `const`) into `TelemetryModel`. No reflection or scanning; the names reach the
  generated code as string literals.
- The emitter adds one `AddSource("<name>")` per configured source (when tracing is on) and
  one `AddMeter("<name>")` per configured meter (when metrics are on).
- No extra names means generated code identical to today's.
- Tests: generator output has the calls (and none when unconfigured); a running app that names
  a custom source and meter exports that span and metric to an in-process collector.
- Telemetry docs describe the option with Taratibu as the example.

## Checklist

- [x] Options + IR/generator support for extra ActivitySources and Meters
- [x] Generated AddSource/AddMeter per name; unchanged output when none
- [x] Tests (generator output + in-memory export)
- [x] Docs

## Session

- Created: ganda OpenCode session (2026-10-31)
- 2026-10-31 implementer-claude (ganda task work): implemented options, extractor, model,
  emitter, tests, docs. CI multi-mode 2161 passed / 6 skipped / 0 failed; standalone phase
  0 failed; `ganda repo audit` clean (no audit fixes needed).

## Results

- `NuruTelemetryOptions` gets two get-only `IList<string>` properties, `ActivitySources` and
  `Meters` (`source/timewarp-nuru/telemetry/nuru-telemetry-options.cs`).
- `TelemetryModel` carries them as `EquatableArray<string>`, so generator caching still compares
  by value.
- `TelemetryOptionsExtractor` now reads `options.ActivitySources.Add(x)` and
  `options.Meters.Add(x)` in block and expression lambdas. `x` must be a compile-time constant
  string, read with `SemanticModel.GetConstantValue`, so a literal or a `const` such as
  `Tel43Names.WalkSource` works. Empty names and repeats are dropped. I also replaced the
  growing list of `ref` parameters with one `Apply` that returns an updated model. Side effect:
  the existing scalar options (`ServiceName`, `OtlpEndpoint`, the `Enable*` flags) now accept
  `const` values as well as literals.
- `TelemetryEmitter` emits one `__tracerBuilder.AddSource("<name>")` per source (tracing on)
  and one `__meterBuilder.AddMeter("<name>")` per meter (metrics on), after Nuru's own calls.
  With no names, the output is byte-for-byte what it was before.
- Tests:
  - `generator-43-telemetry-options.cs` (standalone): three new cases. Configured names are
    emitted in order and deduped, including a `const`. With none configured, only Nuru's two
    calls of each kind appear. With tracing disabled, configured sources are skipped and meters
    are still emitted.
  - New `telemetry/telemetry-06-extra-activity-sources.cs` (runs in the CI multi-mode
    assembly): runs a real app against an in-process OTLP/HTTP collector. It asserts that the
    library's span name, source name, and counter name show up in the `/v1/traces` and
    `/v1/metrics` payloads. A negative case shows the library span is not exported without
    the option.
  - Deviation: the requirement says "in-memory exporter". The generated providers only wire
    the OTLP exporter and give an app no hook to add another one, so the test uses an
    in-process HTTP collector instead (same pattern as telemetry-04). It goes further, since it
    covers the real generated export path.
- Docs: `documentation/user/features/telemetry.md` has a new section, "Exporting other
  ActivitySources and Meters", with Taratibu as the example. The options table and example in
  `documentation/user/reference/nuru-app-options.md` and the example in
  `documentation/user/reference/builder-api.md` are updated.
- Follow-ups (not filed):
  - A non-constant `Add(...)` argument is silently skipped. An analyzer warning would make
    that visible.
  - ganda can now subscribe `TimeWarp.Taratibu` and move its own spans onto a `TimeWarp.Ganda`
    source once a Nuru release with this change ships.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-43-telemetry-options.cs
dotnet run tests/timewarp-nuru-tests/telemetry/telemetry-06-extra-activity-sources.cs
dotnet run tests/ci-tests/run-ci-tests.cs
```

Expect:

- generator-43: 7/7 pass, including `Should_emit_add_source_and_add_meter_per_configured_name`,
  `Should_emit_only_nuru_sources_when_none_configured`, and
  `Should_skip_extra_sources_when_tracing_disabled`.
- telemetry-06: 2/2 pass. `Should_export_spans_and_metrics_from_configured_sources` finds the
  library span and counter in the OTLP payloads, and `Should_not_export_library_span_without_option`
  does not find the span.
- CI suite: 0 failed in both the multi-mode and standalone phases.
