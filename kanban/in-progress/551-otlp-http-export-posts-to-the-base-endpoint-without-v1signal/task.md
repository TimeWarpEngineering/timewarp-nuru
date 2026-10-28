# OTLP HTTP export posts to the base endpoint without /v1/{signal}

## Description

The telemetry emitter (`source/timewarp-nuru-analyzers/generators/emitters/telemetry-emitter.cs` ~96–134) reads `OTEL_EXPORTER_OTLP_ENDPOINT`, builds `new Uri(...)`, and passes it to every exporter with `o => o.Endpoint = __endpoint`. The OpenTelemetry .NET SDK appends the signal path (`/v1/traces`, `/v1/metrics`, `/v1/logs`) for `http/protobuf` **only** when the endpoint comes from the environment variable. An endpoint set in code is used as-is. With `OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf` and a base endpoint such as `https://otlp.timewarp.work/`, every export posts to `/`, and the collector returns 404. gRPC is unaffected.

Found by ganda task 488 (wire `UseTelemetry`), which parked overnight on 2026-10-28. Spans never reached TWE-001 SigNoz, because collector gRPC 4317 is closed and the HTTP posts went to the base path.

## Change

- When the endpoint comes from `OTEL_EXPORTER_OTLP_ENDPOINT` (not `options.OtlpEndpoint`), don't set `o.Endpoint`; let the SDK read the env vars, which handle the per-signal paths and `OTEL_EXPORTER_OTLP_{TRACES,METRICS,LOGS}_ENDPOINT` overrides. Or, if an explicit endpoint must be set, append `/v1/{signal}` for `HttpProtobuf` the way the SDK spec does, unless the path already ends in it.
- Same rule for a code-configured `options.OtlpEndpoint` with HTTP protocol: document whether it's a base URL (append) or a full signal URL (as-is), and test it.
- Keep the no-provider early return from task 517.

## Checklist

- [ ] Generated code exports HTTP traces, metrics, and logs to `{base}/v1/{signal}` for a base env endpoint
- [ ] gRPC behavior unchanged
- [ ] Per-signal env overrides respected
- [ ] Generator snapshot/emitter tests cover http/protobuf and grpc
- [ ] Release a Nuru beta and tell ganda (task 488 resumes on it)

## Notes

- why: ganda overnight stack 2026-10-28, task 488 parked on this; Steve's cockpit session filed it.
- ganda pins `TimeWarp.Nuru 4.0.0-beta.6`.

## Session

- Created: 252075b7-65c4-482c-b43d-14987934160a (2026-10-28)
