# UseTelemetry ignores a non-literal OtlpEndpoint without a diagnostic

## Description

`UseTelemetry(o => o.OtlpEndpoint = someVariable)` is silently ignored. The generator reads only string literals for `OtlpEndpoint` and otherwise falls back to `OTEL_EXPORTER_OTLP_ENDPOINT`, with no diagnostic, so a user who sets a non-literal endpoint gets no export and no explanation. Found by the task 523 consumer check (TimeWarp.Nuru 4.0.0-beta.5 from nuget.org, clean cache; evidence in `kanban/done/523-*/evidence/4.0.0-beta.5/` on branch `task/523-nuru-verification-skill-launch-and-drive-the-real`).

The public `UseTelemetry` options API invites assigning any string expression; the source (telemetry interpreter and emitter plus their Purpose/Design regions) decides which expressions are meant to be supported. Silent ignore is the one outcome that is never intended.

## Checklist

- [ ] Read the telemetry interpreter/emitter and options type to establish intent; record in Notes (ask the human if unclear)
- [ ] Decide: support non-literal expressions (emit the expression into generated code), or report an analyzer diagnostic when `OtlpEndpoint` is not a string literal
- [ ] If a diagnostic: add it to `AnalyzerReleases.Unshipped.md` (never Shipped.md) and document it
- [ ] Add `#region Purpose` / `#region Design` to the touched files
- [ ] Tests: literal endpoint, non-literal endpoint (variable, const, property), and env-var fallback; use `.Map<T>()`, not `DiscoverEndpoints()`
- [ ] `ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs` green
- [ ] Verify with the `tw-nuru-verify` telemetry probe (installed package, clean cache)

## Notes

- Related: the telemetry doc fix in the docs-alignment task.
- No trailing whitespace (RCS1037).

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
