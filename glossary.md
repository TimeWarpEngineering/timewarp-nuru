# Glossary

Shared meanings for words this repository uses.

When an agent or reviewer has to ask what a term or acronym means, or sees one used two ways, the definition goes into `glossary.md` in the same PR.

## Terms

| Term | Meaning |
|------|---------|
| base endpoint | The OTLP collector URL from `OTEL_EXPORTER_OTLP_ENDPOINT` or `NuruTelemetryOptions.OtlpEndpoint`. With `http/protobuf`, each signal posts to `{base}/v1/{signal}`. |
| signal | One OpenTelemetry data kind: traces, metrics, or logs. Each has its own exporter and its own OTLP path. |

## Acronyms

| Acronym | Expansion |
|---------|-----------|
| OTLP | OpenTelemetry Protocol, the wire format `UseTelemetry()` exports with (gRPC or `http/protobuf`). |
