# Review framework — task 470-001

**Date:** 2026-09-22
**Host task:** kanban/in-progress/470-001-wire-or-obsolete-stub-nuruappbuilder-configure-apis/
**Diff scope:** branch `task/470-001-wire-or-obsolete-stub-nuruappbuilder-configure-api` vs `origin/master` (product + kitchen commit `3cebc444`). Product surface is generator IR extraction for `UseTelemetry(Action<NuruTelemetryOptions>)` and `ConfigureHelp`, telemetry/help emitters applying those models, `[Obsolete]` `NuruAppBuilder.Services`, deletion of unused DI leftover types, and tests (`help-10`, `telemetry-01`, `builder-01`, `generator-43`).
**Plan / brief:** Parent 470 M1–M3 (bugs) plus nits M34/M35. Documented builder configure APIs were compile-time stubs, looked for a removed `AddHelp` name, or threw with a method that does not exist (`AddDependencyInjection`). Implementer wired telemetry options and help filtering through generator IR, obsoleted `Services` in favor of `ConfigureServices` / `UseMicrosoftDependencyInjection`, updated XML/docs examples, and deleted `EmptyServiceProvider` / `NuruLoggingBuilder` / `NuruMetricsBuilder`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok session 01a0c727-75be-7e52-979a-c9b318b5b028 (2026-09-22)
**Round 2:** re-review of M1 fix (`IsPerCommandHelpRoute` LongForm-only + help-10 `--helper` regression) plus scan of the fix delta. Prior `round-1/` is frozen.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Requirements to verify

- Either wire each API end-to-end (runtime and/or generator IR) or obsolete/remove it and stop advertising it as live
- Locate `ConfigureHelp` (not `AddHelp`) if help filtering stays
- Fix `Services` exception text and assignment, or obsolete the property in favor of `ConfigureServices`
- Update XML docs/examples that still cref `AddHelp`, two-arg `Map`, and `AddDependencyInjection()` (M34)
- Delete or wire unused `EmptyServiceProvider` / `NuruLoggingBuilder` / `NuruMetricsBuilder` (M35)
- Tests for whichever behavior is kept

## Out of scope

- Sibling parent-470 findings already split into other 470-* tasks
- Opening a PR or moving the board to done
