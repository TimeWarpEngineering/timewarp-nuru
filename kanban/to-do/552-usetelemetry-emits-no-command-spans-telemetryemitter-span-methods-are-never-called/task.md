# UseTelemetry() emits no command spans: TelemetryEmitter span methods are never called

## Description

`UseTelemetry()` on its own emits no command spans. `TelemetryEmitter.EmitTelemetryStart`, `EmitTelemetrySuccess` and `EmitTelemetryCatch` exist but nothing calls them (`git log -S` shows no caller, ever). Command spans appear only when the app also adds `.AddBehavior(typeof(TelemetryBehavior))` or creates activities itself. Metrics and logs export; traces for commands don't.

Found by task 551 (PR #367). Blocks ganda 488 (wire `UseTelemetry` so ganda task spans reach SigNoz): Steven wants spans in the next beta, so beta.7 waits for this.

## Checklist

- [ ] Read the Purpose/Design regions of `telemetry-emitter.cs`, `interceptor-emitter.cs` and `TelemetryBehavior`; decide whether `UseTelemetry()` should emit spans directly or register `TelemetryBehavior` implicitly (one mechanism, no double spans when an app also adds the behavior)
- [ ] With only `UseTelemetry()`, every executed command produces one span: name, route pattern/command tags, status OK, and status Error with the exception recorded on failure
- [ ] An app that already adds `TelemetryBehavior` gets exactly one span per command, not two
- [ ] Span parentage: the command span is a child of any ambient `Activity` (so a host like ganda can nest it)
- [ ] End-to-end test with an OTLP HTTP listener (like `telemetry-04`) proving a span reaches `/v1/traces` from `UseTelemetry()` alone
- [ ] Update `documentation/user/features/telemetry.md` to state what `UseTelemetry()` emits

## Notes

- 551 (PR #367) fixed the OTLP HTTP `/v1/{signal}` paths; build on it.
- Related: 550 (configurable extra ActivitySources), 533 (non-literal OtlpEndpoint).

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
