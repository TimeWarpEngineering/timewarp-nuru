# UseTelemetry() emits no command spans: TelemetryEmitter span methods are never called

## Description

`UseTelemetry()` on its own emits no command spans. `TelemetryEmitter.EmitTelemetryStart`, `EmitTelemetrySuccess` and `EmitTelemetryCatch` exist but nothing calls them (`git log -S` shows no caller, ever). Command spans appear only when the app also adds `.AddBehavior(typeof(TelemetryBehavior))` or creates activities itself. Metrics and logs export; traces for commands don't.

Found by task 551 (PR #367). Blocks ganda 488 (wire `UseTelemetry` so ganda task spans reach SigNoz): Steven wants spans in the next beta, so beta.7 waits for this.

## Goal (implementer restatement)

`UseTelemetry()` alone must produce a trace span for every command it runs, so a host such as ganda
sees command spans in SigNoz without also calling `.AddBehavior(typeof(TelemetryBehavior))`.

Acceptance:

- One mechanism. The generator registers `TelemetryBehavior` itself when `UseTelemetry()` is
  called, as the outermost behavior. The dead `EmitTelemetryStart/Success/Catch` twins are deleted
  (they referenced counters that were never declared).
- With `UseTelemetry()` only, each command run produces one span named by its route pattern, with
  `command.name` / `command.type` tags, status Ok on success, status Error plus `error.type` on
  failure (no exception message, per 470-015 redaction).
- An app that also adds `TelemetryBehavior` gets one span per command, not two.
- The span is a child of `Activity.Current` when the caller has one.
- An end-to-end OTLP/HTTP test shows a command span reaching `/v1/traces` with no hand-made span.
- `documentation/user/features/telemetry.md` says what `UseTelemetry()` emits.

## Checklist

- [x] Read the Purpose/Design regions of `telemetry-emitter.cs`, `interceptor-emitter.cs` and `TelemetryBehavior`; decide whether `UseTelemetry()` should emit spans directly or register `TelemetryBehavior` implicitly (one mechanism, no double spans when an app also adds the behavior)
- [x] With only `UseTelemetry()`, every executed command produces one span: name, route pattern/command tags, status OK, and status Error with the exception recorded on failure
- [x] An app that already adds `TelemetryBehavior` gets exactly one span per command, not two
- [x] Span parentage: the command span is a child of any ambient `Activity` (so a host like ganda can nest it)
- [x] End-to-end test with an OTLP HTTP listener (like `telemetry-04`) proving a span reaches `/v1/traces` from `UseTelemetry()` alone
- [x] Update `documentation/user/features/telemetry.md` to state what `UseTelemetry()` emits

## Review

- Round 1: Blocked — open M1
- Fix pass for round 2: M1 fixed (see Session); round 2 review pending.

## Notes

- 551 (PR #367) fixed the OTLP HTTP `/v1/{signal}` paths; build on it.
- Related: 550 (configurable extra ActivitySources), 533 (non-literal OtlpEndpoint).

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
- 2026-10-28 implementer (claude, ganda task work): implicit `TelemetryBehavior` registration,
  dead emitter twins removed, method-group command class fix, tests and docs. CI multi-mode
  2128 passed / 6 skipped / 0 failed; standalone phase 0 failed. Audit fix: the pre-push audit
  failed `runfile-executable` on the new telemetry-05 file; `ganda repo audit --fix` set its
  executable bit.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session 36ace3e3-c175-4a27-9f0a-e64aa801ab45, max-turns 500, Done — 2026-10-28T06:17:43Z
- Review oracle round 1: reviewer-grok (grok, model grok-4.7), session id not exposed, turn cap 120 (Cursor uncapped), effort 2, roster general — 2026-10-28. Fix pass needed for open finding M1.
- Review oracle: review by reviewer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 120) — 2026-10-28T06:32:23Z
- 2026-10-28 implement-fix-2 (claude, ganda task work): fixed M1. The behavior emitter's
  command initializer now takes property names from `CommandClassEmitter`
  (`GetParameterPropertyName` / `GetOptionPropertyName`: `LongForm ?? ParameterName ?? ShortForm`,
  kebab split) and local names from the matcher's rule (flags `LongForm ?? ShortForm`; value
  options `ParameterName` as written, not lowercased). Writing the test found two more mismatches
  on the same line: `--target-name {targetName?}` emitted `Target-name = targetname`. The command
  construction emitter's `Supplied()` flag name also gets `?? ShortForm`. CI multi-mode 2131 passed /
  6 skipped / 0 failed; standalone 0 failed.

## Results

### Decision

One mechanism: `UseTelemetry()` registers the built-in `global::TimeWarp.Nuru.TelemetryBehavior`
as the app's first (outermost) behavior, in `IrAppBuilder.EffectiveBehaviors()` at
`FinalizeModel()`. It is skipped when the app already added `TelemetryBehavior` (that copy keeps
its position) or when `EnableTracing` and `EnableMetrics` are both false. The generated OTLP
setup already listened to the behavior's `TimeWarp.Nuru.Behavior` source and meter.

`TelemetryEmitter.EmitTelemetryStart/Success/Catch` are deleted. They had no caller and could
not have compiled (they used `__commandsInvoked` / `__commandDuration`, which were never
declared). telemetry-02's source-parity test on `EmitTelemetryCatch` went with them; the
redaction it guarded is now checked end to end in telemetry-05.

### Pre-existing bug found and fixed

A fluent route whose handler is a method group (`HandlerKind.Method`, e.g.
`.WithHandler(Tel01Capture.Probe)`) got no generated command class, so any behavior on it emitted
`Command = __command` with no `__command` declared (CS0103). Implicit registration made every
`UseTelemetry()` app with a method-group handler hit it (telemetry-01, -03, -04). Fix:
`CommandClassEmitter.HasCommandClass(route)` is true for `Delegate` and `Method`, used by the
class emitter and the behavior emitter's command and context creation.

### Span shape

Name = route pattern; tags `command.name` (pattern), `command.type` (endpoint class or generated
command class); `ActivityKind.Internal`; status Ok, or Error with `error.type` and no message or
status description (470-015 redaction kept). Parent = `Activity.Current`.

### Files

- `source/timewarp-nuru-analyzers/generators/ir-builders/ir-app-builder.cs`: `EffectiveBehaviors()`
- `source/timewarp-nuru-analyzers/generators/emitters/telemetry-emitter.cs`: dead span helpers removed, header updated
- `source/timewarp-nuru-analyzers/generators/emitters/command-class-emitter.cs`, `behavior-emitter.cs`: method-group command class
- `command-class-emitter.cs`, `behavior-emitter.cs`, `command-construction-emitter.cs`: one naming rule for the
  behavior command initializer (review M1: short-only flags, kebab long forms, camelCase value parameters)
- `source/timewarp-nuru/telemetry/telemetry-behavior.cs`: remarks
- `tests/timewarp-nuru-tests/telemetry/telemetry-05-use-telemetry-command-spans.cs` (new, CI multi-mode): one Ok span, Error span with `error.type`, no double span with explicit `AddBehavior`, parent = ambient activity; `-v`, `-i -t` and `-m {message} --target-name {targetName?}` routes compile, run and emit one span
- `tests/timewarp-nuru-tests/telemetry/telemetry-04-otlp-endpoint-paths.cs` (standalone): probe no longer starts its own span; new `Should_export_command_span_from_UseTelemetry_alone` waits for `/v1/traces`
- `tests/timewarp-nuru-tests/telemetry/telemetry-02-error-message-redaction.cs`: emitter-twin test removed
- `documentation/user/features/telemetry.md` ("What UseTelemetry() emits"), `pipeline-behaviors.md`, `changelog.md`

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/telemetry/telemetry-05-use-telemetry-command-spans.cs
dotnet run tests/timewarp-nuru-tests/telemetry/telemetry-04-otlp-endpoint-paths.cs
dotnet run tests/ci-tests/run-ci-tests.cs
```

Expect:

- telemetry-05: 7/7 passed (one `tel05-ok` span with status Ok from `UseTelemetry()` alone;
  `tel05-fail` span status Error, `error.type` = `InvalidOperationException`, no `error.message`;
  one `tel05-dup` span with `AddBehavior(typeof(TelemetryBehavior))` too; `tel05-child` parent
  span id = the host activity's span id; `tel05-build -v`, `tel05-docker run -i -t` and
  `tel05-commit -m {message} --target-name {targetName?}` compile under `UseTelemetry()`, bind
  their values, and emit one span each).
- telemetry-04: 9/9 passed, including `Should_export_command_span_from_UseTelemetry_alone`
  (the sink records `/v1/traces` though the handler starts no span).
- CI: multi-mode 0 failed (2131 passed, 6 skipped after the M1 fix), standalone 0 failed.

### Review

Round 1, effort 2, roster general. The goal restatement matches the task. Final disposition is not written: M1 is open.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

M1: `UseTelemetry()` fails to compile a fluent route with a short-only flag (`build -v`, `docker run -i -t`) because the behavior initializer emits `Option = flag` while the command class and the matcher use the short form. Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

