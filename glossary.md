# Glossary

Shared meanings for words this repository uses.

When an agent or reviewer has to ask what a term or acronym means, or sees one used two ways, the definition goes into `glossary.md` in the same PR.

## Terms

| Term | Meaning |
|------|---------|
| built-in type | A route type constraint the generator converts with no registered converter: the `TypeConversionMap` set (the numeric and `char`/`bool`/`string` keywords, `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`, `Version`, `IPAddress`, `BigInteger`, `Uri`, `FileInfo`, `DirectoryInfo`). Most convert with `TryParse`; `Uri` uses `Uri.TryCreate` and `FileInfo`/`DirectoryInfo` their constructor. All of them also convert per element in a typed catch-all and a repeated option. Not limited to types with `TryParse`. |
| base endpoint | The OTLP collector URL from `OTEL_EXPORTER_OTLP_ENDPOINT` or `NuruTelemetryOptions.OtlpEndpoint`. With `http/protobuf`, each signal posts to `{base}/v1/{signal}`. |
| signal | One OpenTelemetry data kind: traces, metrics, or logs. Each has its own exporter and its own OTLP path. |

## Acronyms

| Acronym | Expansion |
|---------|-----------|
| OTLP | OpenTelemetry Protocol, the wire format `UseTelemetry()` exports with (gRPC or `http/protobuf`). |
