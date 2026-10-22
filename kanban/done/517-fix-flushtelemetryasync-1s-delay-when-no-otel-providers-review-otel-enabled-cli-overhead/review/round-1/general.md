# Round 1 — general
**Date:** 2026-10-22
**Scope reviewed:** branch `task/517-fix-flushtelemetryasync-1s-delay-when-no-otel-prov` vs `master`

## Summary

`NuruApp.FlushTelemetryAsync` returns before `ForceFlush` and `Task.Delay` when `TracerProvider`, `MeterProvider`, and `LoggerFactory` are all null. The rest of the method is the previous body: flush both providers, wait `delayMs` when it is positive (default 1000), then dispose and null all three. The generated call is still `await app.FlushTelemetryAsync()` with no `delayMs` argument, inside the `UseTelemetry()` `try`/`finally` in `RunAsync_Intercepted`. Provider setup is still inside the runtime `OTEL_EXPORTER_OTLP_ENDPOINT` check, so an unset endpoint leaves the three properties null and the new return runs.

The implementer restatement matches the original task. The required edit is the null check in the task snippet. Emitters are untouched. The grace delay remains the pre-dispose wait on the has-provider path.

`telemetry-03-flush-without-providers.cs` covers the three claims: a `UseTelemetry()` command with the endpoint cleared stays under 500 ms on the timed second run and sees null providers; `FlushTelemetryAsync(delayMs: 5000)` with a blank app returns in under 1 s; a tracer provider with a 60 s batch delay still exports the buffered span, waits the passed grace delay, shuts the exporter down, and nulls `TracerProvider`. That last test is the has-provider guard. It injects the provider instead of going through `UseTelemetry()` plus a live endpoint. That is the branch this change can break, and `telemetry-01` already checks that an explicit endpoint creates a tracer. The live-collector timings stay in the task notes, as the task asked.

The new runfile is mode `100755`, same as the other telemetry runfiles. The early return uses the same brace-less single-line `if` already used in this file. This session could not re-run `dotnet run` on the new file: the sandbox refused the runfile cache write and the NuGet restore mutex. The review is from the diff, the flush method, the emitter call site, and the test source.

## Issues

<!-- None. -->
