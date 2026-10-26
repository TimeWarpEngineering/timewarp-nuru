# nuru search drops all its log output because it never configures logging

## Description

`nuru search` runs, but every warning and error it logs is thrown away. `source/timewarp-nuru-search/program.cs` never calls `AddLogging`, so the generator hands each `ILogger<T>` a NullLogger:

- `SearchQuery.Handler` (endpoint constructor): auto-index messages.
- `CapabilitiesClient`: "CLI returned exit code", "empty capabilities", "failed to deserialize", and the Error-level "failed to parse capabilities JSON". These are exactly the messages that explain why a CLI didn't get indexed.
- `SearchIndex`: "search failed" (Error) and the index/clear messages.

So when indexing or a search fails, the user gets no output explaining why.

Found in 541 (PR #358): adding NURU_H007 for endpoint constructors made nuru-search fail to build, because it injects a logger with no `AddLogging`. 541 left endpoint constructors out of H007 and documented that.

## Goal (implementer restatement)

nuru-search injects `ILogger<T>` into its endpoint handler and services but never configures logging, so every
warning and error goes to a NullLogger. Configure logging in nuru-search with warnings and errors on stderr
(stdout stays results-only), and close the gap that hid this: NURU_H007 must also warn for endpoint handler
constructors that inject `ILogger<T>` with no logging configured.

Acceptance criteria:
- nuru-search calls `AddLogging` with a console provider routed to stderr at Warning and above.
- NURU_H007 fires for an endpoint handler constructor `ILogger<T>` without `AddLogging`, and not with it (tests).
- Both `logging.md` copies no longer say endpoint constructors don't warn.
- Any repo app/sample/test the new warning breaks is fixed; the CI and generator tests pass.
- Changelog entry notes the new warning (breaking for warnings-as-errors builds).
- `nuru search --cli <bad-path>` prints the warning on stderr.

## Checklist

- [x] Add logging to nuru-search (`ConfigureServices(s => s.AddLogging(...))`), with warnings and errors to stderr so stdout stays clean for results
- [x] Extend NURU_H007 to endpoint handler constructors (the part 541 deferred), and update the "endpoint constructors don't warn yet" note in both `logging.md` copies
- [x] Fix any other repo app/sample that the new warning breaks
- [x] Test: H007 fires for an endpoint constructor `ILogger<T>` without `AddLogging`, and not with it
- [x] Verify by hand: `nuru search --cli <bad-path>` prints the warning

## Notes

- Behaviors and delegate handlers already warn (541). Endpoint constructors are the remaining silent case.
- This is breaking for user apps that treat warnings as errors. Add a changelog entry.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
- 2026-10-26 implementer (claude, ganda task work): implemented. Hand check found a second bug: a
  registered service's `ILogger<T>` was always a NullLogger, even with `AddLogging`
  (`EnsureServicesInitialized` used `FrameworkServices.GetInitExpression`, which hard-coded
  `NullLoggerFactory`). So adding logging to nuru-search alone printed nothing from
  `CapabilitiesClient`/`SearchIndex`. Fixed in the emitter and extended H007 to service constructors too.
  `ganda repo audit` passed with no fixes needed.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-26T06:48:37Z
- Review oracle: tw-implementation-review, effort 2, roster general (Grok 4.7), session not reported, max-turns uncapped (review budget 120), round 1, disposition clean — 2026-10-26

## Results

### What changed

- **nuru-search** (`source/timewarp-nuru-search/program.cs`): `AddLogging(b => b.SetMinimumLevel(Warning).AddProvider(new StandardErrorLoggerProvider()))`.
  The new `services/standard-error-logger-provider.cs` writes `level: message` lines synchronously to stderr through
  `TimeWarpTerminal.Default` (`System.Console` is banned). The console logger was not used: its background queue can
  drop the last entries at process exit, and the generated static logger factory is never disposed.
- **NURU_H007** (`nuru-generator.cs` `ReportLoggerWithoutConfigurationWarnings`): now also covers endpoint handler
  constructors (`Handler.ConstructorDependencies`) and registered service constructors.
- **Service logger bug** (`interceptor-emitter.cs` `EmitEnsureServicesInitialized`): a service's `ILogger<T>` /
  `ILoggerFactory` field now uses `LoggerExpressions.Resolve`, the same expression handlers and behaviors use (the
  `AddLogging` static field, or `app.LoggerFactory` with telemetry). It was always `NullLoggerFactory` before.
  This was in scope because without it nuru-search's `CapabilitiesClient` and `SearchIndex` messages still went nowhere.
- **Docs**: both `logging.md` copies drop "endpoint constructors don't warn yet", and list registered service
  constructors as a logger site that the `AddLogging` factory serves and H007 covers.
- **Changelog**: Changed (H007, breaking for warnings-as-errors) and Fixed (service loggers, nuru search).
- **Tests**: new `tests/timewarp-nuru-tests/generator/generator-69-endpoint-constructor-logger-warning.cs` (5 tests,
  Roslyn-hosted, standalone; added to the CI excludes and standalone list). The endpoint-warn test fails on the
  old generator (checked).
- No other repo app or sample broke: `dev build` and `dev verify-samples` (68/68) are green.

### Verification

- `./bin/dev build`: succeeded.
- `./bin/dev verify-samples`: 68/68.
- `./bin/dev test` (after `ganda runfile cache --clear`): multi-mode 2111 passed, 6 skipped, 0 failed of 2117;
  standalone 41 files, 0 failed.
- `ganda repo audit`: passes all checks.
- Hand check: `nuru search --cli false hello` → stderr `warning: CLI /usr/bin/false returned exit code 1`;
  stdout unchanged. A bad path that is not in PATH (`--cli no-such-cli`) does not reach `CapabilitiesClient`: the
  handler prints its existing "not found in PATH" line, so `false`/`ls` (real binaries that fail `--capabilities`)
  exercise the logged path.

### How to validate

**Smoke:**

```bash
ganda runfile cache --clear
./bin/dev build
H=$(mktemp -d); HOME=$H dotnet source/timewarp-nuru-search/bin/Release/net10.0/TimeWarp.Nuru.Search.dll search --cli false hello 2>/tmp/err.txt; cat /tmp/err.txt; rm -rf $H
dotnet run tests/timewarp-nuru-tests/generator/generator-69-endpoint-constructor-logger-warning.cs
```

**Expect:**

- stdout shows `Warning: Failed to get capabilities from 'false'. Cannot auto-index.` and `No results found.`
- `/tmp/err.txt` contains `warning: CLI /usr/bin/false returned exit code 1` (before this task stderr was empty).
- generator-69 reports 5 passed, 0 failed.

### Review

Round 1, effort 2, roster general. Counts: bug 0, suggestion 0, nit 0, open 0. Disposition: clean. The restatement asks for a console provider; the checklist asks for warnings and errors on stderr. That misread is under `## Learnings`. Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

### Follow-ups (not filed)

- Pre-existing: the search handler's own "Warning: ..." lines go to stdout through `ITerminal.WriteLineAsync`, not stderr.
- Pre-existing: `FrameworkServices.GetFieldName` names logger fields by the simple category name, so two
  categories with the same simple name in different namespaces would collide (`__fw_ILogger_X`).

## Learnings

- Misread: the implementer restatement says nuru-search calls `AddLogging` with a console provider routed to stderr. The checklist says `AddLogging` with warnings and errors on stderr so stdout stays results-only, and does not name the console provider. The console logger queues writes, and the generated factory is never disposed, so the last lines can disappear. The implementation writes those lines synchronously and follows the checklist.

