# OTLP HTTP export posts to the base endpoint without /v1/{signal}

## Description

The telemetry emitter (`source/timewarp-nuru-analyzers/generators/emitters/telemetry-emitter.cs` ~96–134) reads `OTEL_EXPORTER_OTLP_ENDPOINT`, builds `new Uri(...)`, and passes it to every exporter with `o => o.Endpoint = __endpoint`. The OpenTelemetry .NET SDK appends the signal path (`/v1/traces`, `/v1/metrics`, `/v1/logs`) for `http/protobuf` **only** when the endpoint comes from the environment variable. An endpoint set in code is used as-is. With `OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf` and a base endpoint such as `https://otlp.timewarp.work/`, every export posts to `/`, and the collector returns 404. gRPC is unaffected.

Found by ganda task 488 (wire `UseTelemetry`), which parked overnight on 2026-10-28. Spans never reached TWE-001 SigNoz, because collector gRPC 4317 is closed and the HTTP posts went to the base path.

## Change

- When the endpoint comes from `OTEL_EXPORTER_OTLP_ENDPOINT` (not `options.OtlpEndpoint`), don't set `o.Endpoint`; let the SDK read the env vars, which handle the per-signal paths and `OTEL_EXPORTER_OTLP_{TRACES,METRICS,LOGS}_ENDPOINT` overrides. Or, if an explicit endpoint must be set, append `/v1/{signal}` for `HttpProtobuf` the way the SDK spec does, unless the path already ends in it.
- Same rule for a code-configured `options.OtlpEndpoint` with HTTP protocol: document whether it's a base URL (append) or a full signal URL (as-is), and test it.
- Keep the no-provider early return from task 517.

## Goal (implementer restatement)

`UseTelemetry()` must send HTTP OTLP exports to the right per-signal URL. Today the generated
setup sets `o.Endpoint` on every exporter. That disables the SDK's `/v1/{signal}` append for
`http/protobuf`, so every export posts to the base URL and the collector returns 404.

Acceptance:

- With `OTEL_EXPORTER_OTLP_ENDPOINT` as the only source (no `OtlpEndpoint` in code), the
  generated code leaves `o.Endpoint` alone. The SDK then reads the env vars itself: traces,
  metrics, and logs post to `{base}/v1/traces|metrics|logs` over HTTP, and
  `OTEL_EXPORTER_OTLP_{TRACES,METRICS,LOGS}_ENDPOINT` overrides are used as-is.
- A code-set `options.OtlpEndpoint` is documented as a base URL, like the env var. For
  `http/protobuf`, Nuru appends `/v1/{signal}` unless the path already ends with it. For gRPC
  the endpoint passes through unchanged, as before. Code beats env.
- gRPC behavior does not change in either path.
- The no-provider early return in `FlushTelemetryAsync` (task 517) stays.
- Tests: generator tests cover the emitted code for the env path and the code path. Runtime
  tests cover http/protobuf and grpc endpoint resolution, plus an end-to-end HTTP capture that
  shows the real request paths.
- The beta release and the ganda notification run after merge (release flow), not in this
  implement node.

## Checklist

- [x] Generated code exports HTTP traces, metrics, and logs to `{base}/v1/{signal}` for a base env endpoint
- [x] gRPC behavior unchanged
- [x] Per-signal env overrides respected
- [x] Generator snapshot/emitter tests cover http/protobuf and grpc
- [ ] Release a Nuru beta and tell ganda (task 488 resumes on it)

## Notes

- why: ganda overnight stack 2026-10-28, task 488 parked on this; Steve's cockpit session filed it.
- ganda pins `TimeWarp.Nuru 4.0.0-beta.6`.

## Session

- Created: 252075b7-65c4-482c-b43d-14987934160a (2026-10-28)
- 2026-10-28 implementer (claude, headless task work): implemented the fix, tests, and docs.
  Boy-scout: `ganda repo audit` failed on the new `glossary` check. `ganda repo audit --fix`
  scaffolded `glossary.md`, and I seeded it with OTLP, signal, and base endpoint.

## Results

### What changed

- New `NuruOtlpEndpoint` (`source/timewarp-nuru/telemetry/nuru-otlp-endpoint.cs`), public and
  `[EditorBrowsable(Never)]`, used by generated code. `Configure(options, codeEndpoint, signal)`
  applies the OTel precedence:
  1. A code-set `OtlpEndpoint` wins. It is a base URL: for `http/protobuf`, `ForSignal` appends
     `/v1/{signal}` unless the path already ends with it. gRPC passes through unchanged.
  2. Otherwise `OTEL_EXPORTER_OTLP_{TRACES,METRICS,LOGS}_ENDPOINT` is used as-is.
  3. Otherwise `Endpoint` is left alone, so the SDK's own `OTEL_EXPORTER_OTLP_ENDPOINT`
     handling, including the append, stays on.
  `OTEL_EXPORTER_OTLP_{SIGNAL}_PROTOCOL` overrides the protocol for its signal.
- The telemetry emitter no longer emits `o => o.Endpoint = __endpoint`. Every exporter gets
  `o => NuruOtlpEndpoint.Configure(o, __otlpCodeEndpoint, NuruOtlpEndpoint.{Signal})`.
  `__otlpCodeEndpoint` is the literal for a code-set endpoint and null for the env path.
- Why Nuru reads the per-signal vars itself: OpenTelemetry 1.19.1's per-signal `AddOtlpExporter`
  builds its options from the base env vars only. `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` and the
  other per-signal vars are read only by `UseOtlpExporter`. Confirmed by decompiling
  `OtlpExporterOptions.ApplyConfiguration`.
- The export gate is unchanged: export runs only when `OtlpEndpoint` or
  `OTEL_EXPORTER_OTLP_ENDPOINT` is set. The 517 no-provider early return in
  `FlushTelemetryAsync` is untouched.
- Docs: `NuruTelemetryOptions.OtlpEndpoint` XML remarks, `documentation/user/features/telemetry.md`
  (new env vars and an "Endpoint paths" section), and `documentation/user/reference/nuru-app-options.md`.

### Tests

- `generator-43-telemetry-options.cs`: two new cases. A code endpoint and the env path both emit
  `Configure` for traces, metrics, and logs, and neither emits `o.Endpoint = __endpoint`.
- New `telemetry-04-otlp-endpoint-paths.cs` (8 tests):
  - `ForSignal` for http/protobuf: append, trailing slash, base with a path prefix, and a path
    already ending in the signal path. `ForSignal` for gRPC: unchanged.
  - `Configure`: code endpoint as a base URL for http and grpc, code endpoint beats the
    per-signal env var, and per-signal env endpoint and protocol are applied as-is.
  - End to end: an `HttpListener` sink records request paths. With the base env endpoint and
    `http/protobuf`, the app posts `/v1/traces`, `/v1/metrics`, and `/v1/logs`. With
    `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT=…/custom/traces`, traces go there and the others still
    use `/v1/…`.
  - Regression proof: with the old emitter restored, the end-to-end tests fail with paths
    `["/", "/", "/", "/"]`, the bug as reported.
- telemetry-04 runs in the standalone phase (`CiTestExcludes` plus the standalone list). Inside
  the shared multi-mode CI assembly, OpenTelemetry log export never reaches the sink, even from a
  plain `LoggerFactory` with no Nuru code, while traces and metrics do. That build also has
  `EventSource.IsSupported=false`, so the SDK cannot report why. As its own app it passes.
- `./bin/dev build`: green. `./bin/dev test`: exit 0, with multi-mode passing and standalone at
  0 failed. `ganda repo audit`: passes.

### Follow-ups for the cockpit (not fixed here)

1. **`UseTelemetry()` alone emits no command spans.** `TelemetryEmitter.EmitTelemetryStart`,
   `EmitTelemetrySuccess`, and `EmitTelemetryCatch` are never called (`git log -S` shows no
   caller ever). Spans come only from `.AddBehavior(typeof(TelemetryBehavior))` or app code.
   If ganda 488 uses only `UseTelemetry()`, SigNoz still gets metrics and logs after this fix,
   but no command spans.
2. A method-group handler (`.WithHandler(SomeClass.Method)`) combined with `AddBehavior(...)`
   generates `Command = __command`, where `__command` is undefined (CS0103).
3. A lambda handler body under `AddBehavior(...)` is inlined without qualifying type names:
   `() => Tel04Capture.Probe()` fails CS0103 when the type is in another namespace.
4. The checklist item "Release a Nuru beta and tell ganda" happens after merge, through the
   release flow (tw-release), not in this implement node.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/telemetry/telemetry-04-otlp-endpoint-paths.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-43-telemetry-options.cs
```

Expect: telemetry-04 shows `Total: 8, Passed: 8`, including
`Should_export_http_signals_to_v1_paths_for_base_env_endpoint` and
`Should_export_to_signal_env_endpoint_when_set`. generator-43 shows `Total: 4, Passed: 4`.
Optional manual check: run an app with `UseTelemetry()` and set
`OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf` and
`OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp.timewarp.work/`. The collector then logs POSTs to
`/v1/metrics` and `/v1/logs`, plus `/v1/traces` when the app adds `TelemetryBehavior`, and no
404s on `/`.
