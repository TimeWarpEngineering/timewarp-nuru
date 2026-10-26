# Generator doesn't treat services.AddLogging() with no arguments as logging setup

## Description

The generator does not recognize `services.AddLogging()` called with no arguments as logging setup. Handlers then get a NullLogger, and NURU_H007 warns that logging isn't configured, even though the app called `AddLogging`. Only `AddLogging(logging => ...)` is detected.

`AddLogging()` with no arguments is valid Microsoft.Extensions.Logging API: it registers `ILoggerFactory` and `ILogger<T>` with no providers. The generator should treat it as logging configured. Logs then go nowhere because no providers are registered, but that is the documented behavior of the M.E.L API, not a Nuru bug.

Found by the 541 implementer (fix pass 2). It predates 541; generator-67 uses `AddLogging(logging => ...)` to avoid it.

## Goal (implementer restatement)

`services.AddLogging()` with no configure delegate is real M.E.L setup: it registers
`ILoggerFactory` and `ILogger<T>` with no providers. The generator should treat it as logging
configured, the same as `AddLogging(b => ...)`. The app then gets a static logger factory with no
providers, injected loggers come from it (not `NullLogger`), and NURU_H007 stays quiet.

Acceptance:
- Detection resolves the `AddLogging` method symbol (M.E.DependencyInjection
  `LoggingServiceCollectionExtensions.AddLogging`) through the semantic model, not the name text.
- The zero-argument overload counts as logging configured and emits a provider-less factory.
- A test proves `ConfigureServices(s => s.AddLogging())` gives an `ILogger<T>` delegate parameter
  a real logger from the app factory, and that NURU_H007 does not fire.
- Other call shapes are checked and either supported or recorded.

## Checklist

- [x] Find the AddLogging detection in the generator and accept the zero-argument overload, using the semantic model (method symbol), not syntax text
- [x] Test: `ConfigureServices(s => s.AddLogging())` with an `ILogger<T>` delegate parameter gets a real logger from the app's factory, and NURU_H007 does not fire
- [x] Check for other overload shapes the detection misses

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
- Implementer (claude, ganda task work) 2026-10-26: semantic AddLogging detection, generator-68 test.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-26T05:03:29Z
- Review oracle: tw-implementation-review, effort 2, roster general (Grok 4.7), session not reported, max-turns uncapped (review budget 120), round 1, disposition clean — 2026-10-26
- Review oracle: review by reviewer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 120) — 2026-10-26T05:34:20Z

## Results

AddLogging detection in `ServiceExtractor` now gets the `IInvocationOperation` for each call in
the ConfigureServices body. It matches the target method symbol
(`Microsoft.Extensions.DependencyInjection.LoggingServiceCollectionExtensions.AddLogging`) instead
of the name text. It finds the configure delegate by its `Action<ILoggingBuilder>` parameter, not
by position.

Call shapes checked (tests in `generator-68-add-logging-call-shapes.cs`):

| Shape | Before | Now |
|-------|--------|-----|
| `s.AddLogging()` (expression or block body) | NullLogger + NURU_H007 | factory with no providers |
| `LoggingServiceCollectionExtensions.AddLogging(s)` | NullLogger + NURU_H007 | factory with no providers |
| `LoggingServiceCollectionExtensions.AddLogging(s, b => ...)` | NullLogger + NURU_H007 (the first arg was `s`) | lambda body emitted |
| `s.AddLogging(configure: b => ...)` | worked | worked |
| `s.AddLogging(delegate (ILoggingBuilder lb) { ... })` | NullLogger + NURU_H007 | block body emitted |
| `s.AddLogging(ConfigureLogging)` (method group or delegate variable) | NullLogger + NURU_H007 | unchanged, still warns |

Method groups and delegate variables stay unsupported on purpose. The generated class may not be
able to reach the method (it could be a local function), and an empty factory would quietly drop
the user's providers. NURU_H007 still fires for them, so the user hears about it. When there are
several AddLogging calls, only the first one found counts. M.E.L would apply all of them; that was
already the behavior before this task.

`InterceptorEmitter.EmitLoggingFactoryFields` emits an empty `LoggerFactory.Create(builder => { })`
for an empty configuration, with no stray `;`.

Gates: `dotnet run tests/ci-tests/run-ci-tests.cs` (cache cleared) gave 2111 passed, 6 skipped in
multi-mode and 40/40 standalone files passing, generator-68 included. `ganda repo audit` passes.
On the old service-extractor, 5 of the 8 generator-68 tests fail.

### Review

Round 1, effort 2, roster general. Counts: bug 0, suggestion 0, nit 0, open 0. Disposition: clean. The implementer restatement matches the original description and checklist; no misread. Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-68-add-logging-call-shapes.cs
```

Expect: 8/8 pass. `Should_give_delegate_a_real_logger_from_parameterless_add_logging` compiles an
app with `ConfigureServices(s => s.AddLogging())` and a delegate `ILogger<T>` parameter, checks
that NURU_H007 does not fire, then runs the compiled app. The logger's runtime type is `Logger`1`,
not `NullLogger`1`. The method-group and no-AddLogging tests still expect exactly one NURU_H007.

## Review

- Round 1: Done
