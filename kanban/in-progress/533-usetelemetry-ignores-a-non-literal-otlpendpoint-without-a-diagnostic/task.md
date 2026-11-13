# UseTelemetry ignores a non-literal OtlpEndpoint without a diagnostic

## Description

`UseTelemetry(o => o.OtlpEndpoint = someVariable)` is silently ignored. The generator reads only string literals for `OtlpEndpoint` and otherwise falls back to `OTEL_EXPORTER_OTLP_ENDPOINT`, with no diagnostic, so a user who sets a non-literal endpoint gets no export and no explanation. Found by the task 523 consumer check (TimeWarp.Nuru 4.0.0-beta.5 from nuget.org, clean cache; evidence in `kanban/done/523-*/evidence/4.0.0-beta.5/` on branch `task/523-nuru-verification-skill-launch-and-drive-the-real`).

The public `UseTelemetry` options API invites assigning any string expression; the source (telemetry interpreter and emitter plus their Purpose/Design regions) decides which expressions are meant to be supported. Silent ignore is the one outcome that is never intended.

## Goal (implementer restatement)

`UseTelemetry(o => o.OtlpEndpoint = someVariable)` must not be dropped without a word. The
telemetry extractor's Design region already decides intent: option values are read at compile
time through `SemanticModel.GetConstantValue` and emitted as literals (AOT/trim safe), so a
non-constant value is not supported. The fix is therefore a generator diagnostic, not emitting
the expression.

Acceptance:
- A non-constant value assigned to a `UseTelemetry` option the generator reads (`OtlpEndpoint`,
  and the same rule for `ServiceName`, `ServiceVersion`, `Enable*`, and
  `ActivitySources.Add` / `Meters.Add` arguments) reports a warning at the value, naming the
  option and what applies instead (for `OtlpEndpoint`: `OTEL_EXPORTER_OTLP_ENDPOINT`).
- A configure argument that is not an inline lambda (a method group or delegate variable) also
  warns, because all its options are ignored.
- A string literal and a `const` (local or member) keep working with no diagnostic; no
  `UseTelemetry` options still falls back to the env var with no diagnostic.
- Rule listed in `AnalyzerReleases.Unshipped.md`, documented in the telemetry guide, changelog,
  and the tw-nuru skill; Purpose/Design regions on touched files.
- Tests: literal, const, variable, property, non-lambda, env fallback.

## Checklist

- [x] Read the telemetry interpreter/emitter and options type to establish intent; record in Notes (ask the human if unclear)
- [x] Decide: support non-literal expressions (emit the expression into generated code), or report an analyzer diagnostic when `OtlpEndpoint` is not a string literal
- [x] If a diagnostic: add it to `AnalyzerReleases.Unshipped.md` (never Shipped.md) and document it
- [x] Add `#region Purpose` / `#region Design` to the touched files
- [x] Tests: literal endpoint, non-literal endpoint (variable, const, property), and env-var fallback; use `.Map<T>()`, not `DiscoverEndpoints()`
- [x] `ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs` green (via `./bin/dev test`)
- [ ] Verify with the `tw-nuru-verify` telemetry probe (installed package, clean cache) — deferred: needs a published package carrying this change (next 4.0.0 beta)

## Review

- Round 1: Done

## Notes

- Related: the telemetry doc fix in the docs-alignment task.
- No trailing whitespace (RCS1037).
- Intent (2026-11-14): `telemetry-options-extractor.cs` Design region says values must be
  compile-time constants read through `GetConstantValue` and emitted as literals (AOT/trim safe).
  Emitting the expression is not viable: it can name locals the generated interceptor cannot
  reach. So the fix is a diagnostic, not support. A `const` (local or member) already worked.
- The same silent skip applied to every option and to a non-lambda configure argument, so the
  rule covers them all: NURU_T001 (value not constant) and NURU_T002 (configure not a lambda),
  both warnings, new category `Telemetry`.

## Results

- New `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.telemetry.cs`:
  NURU_T001 / NURU_T002 (Warning, category `Telemetry`), listed in `AnalyzerReleases.Unshipped.md`.
- `TelemetryOptionsExtractor.Extract` takes the interpreter's `CollectedDiagnostics`. A known
  option (`OtlpEndpoint`, `ServiceName`, `ServiceVersion`, `Enable*`) or an
  `ActivitySources.Add` / `Meters.Add` argument whose value has no constant reports NURU_T001 at
  the value with the fallback that applies (`OtlpEndpoint`: OTEL_EXPORTER_OTLP_ENDPOINT). A
  non-lambda configure argument reports NURU_T002. Behavior for literals/consts is unchanged.
- Docs: telemetry guide section "Options are read at compile time", nuru-app-options reference,
  `NuruTelemetryOptions` XML docs, changelog, tw-nuru skill.
- Tests (generator-43, standalone): const local + const member (no diagnostic, endpoint emitted),
  variable (one NURU_T001 at the value, env fallback emitted), property/method call across four
  options (four NURU_T001, the literal in the same lambda still applies), delegate variable
  (NURU_T002), literal and env fallback (no NURU_T diagnostics).
- Not done: `tw-nuru-verify` installed-package probe; it needs a release with this change.

### Gates

- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- `./bin/dev test` (after `ganda runfile cache --clear`) — passed: CI multi-mode 2184 passed,
  6 skipped, 0 failed; standalone phase 0 failed.
- `ganda repo audit` — passed with one advisory warning (`vscode-window-icon`: the worktree's
  git-ignored `.vscode/settings.json` was missing); `ganda repo audit --fix` wrote it (local only,
  nothing to commit) and the rerun passed all checks.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
(cd tests/timewarp-nuru-tests/generator && dotnet run generator-43-telemetry-options.cs)
```

Expect: 11 tests pass, including `Should_report_nuru_t001_for_otlp_endpoint_variable`,
`Should_report_nuru_t001_for_otlp_endpoint_property_and_call`,
`Should_report_nuru_t002_for_non_lambda_configure` and
`Should_emit_const_otlp_endpoint_without_diagnostic`. In an app,
`string e = ...; .UseTelemetry(o => o.OtlpEndpoint = e)` now builds with
`warning NURU_T001: UseTelemetry ignores this value for 'OtlpEndpoint' ... The app reads
OTEL_EXPORTER_OTLP_ENDPOINT instead.`

### Review

- Round 1. Roster: general. Effort 3 (by-diff). Turn cap 200.
- Counts: bug 0 open / 0 fixed / 0 wontfix; suggestion 0; nit 0. No findings.
- Disposition: clean. No wontfix and no escalation.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-11-13T20:47:31Z
- Review oracle: reviewed by general (grok, vendor cursor, model grok-4.7), session not reported, max-turns 200, clean — 2026-11-14
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 200), artifacts general.md, merged.md, disposition.md — 2026-11-13T20:53:40Z
