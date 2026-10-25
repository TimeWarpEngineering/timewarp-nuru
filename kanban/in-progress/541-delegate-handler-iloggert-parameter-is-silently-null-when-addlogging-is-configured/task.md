# Delegate handler ILogger<T> parameter is silently null when AddLogging is configured

## Description

A delegate handler that takes `ILogger<T>` gets null. With `ConfigureServices(s => s.AddLogging(...))` configured, `.WithHandler((ILogger<X> logger) => ...)` generates `ILogger<X> logger = default!; // Service not registered`, with no diagnostic, so the handler throws a NullReferenceException the first time it logs.

`ServiceResolverEmitter.EmitServiceResolution` has no `ILogger<T>` case. `HandlerInvokerEmitter.ResolveServiceForCommand` does, which is why endpoint handler constructors and behaviors receive a logger. Found by task 527 (PR #356), which removed the legacy logging builder APIs; 527's logging docs currently say delegate parameters are not resolved yet. That sentence documents this bug and should go once it is fixed.

## Goal (implementer restatement)

A `.WithHandler((ILogger<X> logger) => ...)` delegate must get a working logger, the same one an
endpoint handler constructor gets. Today the delegate path (`ServiceResolverEmitter`) has no logger
case and emits `default!` with no diagnostic.

Acceptance:

- Delegate handlers and endpoint/behavior constructors resolve services through one shared path,
  so `ILogger<T>` (and typed `AddHttpClient` services) behave the same at every injection site.
- With `AddLogging` configured, a delegate's `ILogger<T>` writes through the app's logger factory.
- Without logging, the delegate gets a NullLogger and NURU_H007 warns, as behaviors do.
- No handler service parameter compiles to a silent `default!`: a type the emitter cannot resolve
  gets a diagnostic (NURU050) instead of being skipped by the validator.
- Regression test with `Map<T>()`/fluent; the "delegate parameters are not resolved yet" note is
  removed from both logging.md copies; the tw-nuru-verify `add-logging` probe covers a delegate.
- Runfile cache cleared, full CI suite and `ganda repo audit` green.

## Checklist

- [x] Read the Purpose/Design regions of `ServiceResolverEmitter` and `HandlerInvokerEmitter`; share one `ILogger<T>` resolution path rather than duplicating it (neither file has the regions; the three logger copies are now `LoggerExpressions`)
- [x] Delegate handlers resolve `ILogger<T>` from the app's logger factory when logging is configured
- [x] When logging is not configured, behave like other injection sites (NullLogger or NURU_H007), never a silent `default!`
- [x] Look for other service types that fall through to `default!` silently; report a diagnostic instead of emitting null
- [x] Regression test: delegate handler with `ILogger<T>` logs (use `Map<T>()`/fluent, not `DiscoverEndpoints()`)
- [x] Remove the "delegate parameters are not resolved yet" note from both logging.md copies
- [x] Extend the tw-nuru-verify `add-logging` probe (or add a case) to cover a delegate handler
- [x] `ganda runfile cache --clear`, full CI suite green

## Notes

- Source is the authority: `ILogger<T>` injection is supported for endpoints and behaviors, so a delegate handler getting null is a code defect.
- New analyzer diagnostics go in `AnalyzerReleases.Unshipped.md`, never Shipped. No trailing whitespace.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
- 2026-10-26 implementer (claude, ganda task work): shared resolver, validator and H007 changes,
  generator-66/67 tests, docs, probe. Full CI green. The pre-push audit then flagged
  `runfile-executable` on the new generator-67 runfile. `ganda repo audit --fix` set its executable bit.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-25T19:21:51Z

## Results

### What changed

- New `generators/emitters/logger-expressions.cs` (`LoggerExpressions`): the one logger path.
  It matches `ILogger<T>`, `ILogger`, and `ILoggerFactory` by full name and emits the static
  `__loggerFactory` read, the nullable `app.LoggerFactory` read (telemetry), or a NullLogger.
  `BehaviorEmitter` and `HandlerInvokerEmitter` had their own copies, both matching on
  `Contains("ILogger")`. Both now call this.
- `ServiceResolverEmitter.ResolveServiceExpression` is the shared service path:
  logger, runtime DI, mediator, registered service, typed HttpClient, and the error fallback.
  The delegate-parameter emitter (`EmitServiceResolution`) and the endpoint constructor
  resolver (`HandlerInvokerEmitter.ResolveServiceForCommand`) both use it. Delegates gained the
  logger and HttpClient cases. `HandlerInvokerEmitter.Emit` passes the logger factory and HttpClient
  configuration through. I removed three dead private helpers from `HandlerInvokerEmitter`
  (`ResolveServiceExpression`, `ResolveRegisteredService`, `ToVariableName`).
- Silent `default!` sweep (`ServiceValidator`): the validator skipped every type containing
  "ILogger" and every options type, but the delegate emitter binds only `IOptions<T>`. Handler
  parameters now skip only `IOptions<T>` and real logging types. `IOptionsSnapshot<T>`,
  `IOptionsMonitor<T>`, and a user interface named like `IAuditILogger` get NURU050 when nobody
  registers them. Constructor dependencies keep the broader options skip.
- NURU_H007 (`NuruGenerator.ReportLoggerWithoutConfigurationWarnings`) now covers
  delegate and method handler parameters as well as behaviors. It warns once per category. It no
  longer warns when `UseTelemetry()` logging is on, because that supplies `app.LoggerFactory`.
- Boy-scout fix: `InterceptorEmitter.GetHttpClientFieldName` kept the `global::` prefix for
  a global-namespace service type, which generated `__httpClient_global::IFoo`. That code does
  not compile. The new HttpClient delegate test found it.
- Docs: both `logging.md` copies list delegate parameters as an injection point. The "not
  resolved yet" note is gone, and the H007 sentence now says which sites warn. The changelog has a
  Fixed entry.
- tw-nuru-verify: `add-logging.cs` has a `work-delegate` route, with a new `cases.tsv` row and an
  updated SKILL.md table row. The probe runs against the published package, so this case fails
  until the release that contains 541 ships.

### Decisions / open items

- I did not extend NURU_H007 to endpoint handler constructors, though they also get a silent
  NullLogger. When I tried it, `timewarp-nuru-search` failed to build: its `SearchQuery.Handler`
  injects `ILogger<T>` and the app has no `AddLogging`. Any consumer app with warnings-as-errors
  would break the same way. That change needs its own decision, so the docs say endpoint
  constructors do not warn yet.
- Kanban 544 (apps in one compilation share the first app's logger factory) still applies, and
  delegates now share that behavior.

### Tests

- `tests/timewarp-nuru-tests/generator/generator-67-delegate-handler-service-resolution.cs` (new,
  Roslyn-hosted, standalone in CI) has 6 tests. Each one names the generated code or diagnostic it checks:
  - with AddLogging, a delegate gets `__loggerFactory.CreateLogger<T>()`
  - without logging, it gets a NullLogger, `ILoggerFactory` becomes `NullLoggerFactory.Instance`, and H007 is reported once
  - telemetry-only logging produces no H007
  - a typed HttpClient delegate parameter is constructed
  - `IOptionsMonitor<T>` reports NURU050
  - a service named like a logger reports NURU050
- `generator-66-add-logging-injected-logger.cs` adds an end-to-end test: a fluent
  `WithHandler((ILogger<T> logger) => ...)` writes Information through the AddLogging factory, and
  Debug is filtered out.
- `ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs`, exit 0:
  multi-mode passed 2111 with 6 skipped and 0 failed, and standalone had 0 failed.
- `ganda repo audit`: passes all checks.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-67-delegate-handler-service-resolution.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-66-add-logging-injected-logger.cs
dotnet run tests/ci-tests/run-ci-tests.cs
```

Expect: generator-67 reports `Total: 6, Passed: 6`, generator-66 reports `Total: 2, Passed: 2`
(including `Should_log_through_delegate_handler_logger_parameter`), and the CI runner exits 0
with 0 failed. After release, `nuru-consumer-check.sh` shows the new
`add-logging ... work-delegate` case PASS, with `probe-delegate-entry` present and `probe-delegate-debug` absent.
