# NuGet updates: .NET 11 GA packages, SDK pin, TimeWarp and OpenTelemetry bumps

## Description

`ganda nuget outdated` on master (2026-11-13) listed 28 outdated packages. .NET 11 went GA, so
the bulk is `11.0.0-rc.3.26522.148` → `11.0.0` for the Microsoft.Extensions.* and
Microsoft.Data.Sqlite.Core pins, plus NetAnalyzers `11.0.100`. global.json moves to SDK `11.0.100`.

Other bumps: TimeWarp.Build.Tasks 1.0.0 → 2.0.0 (major), System.CommandLine 2.0.14 → 3.0.0
(major, benchmarks only), TimeWarp.Builder 1.1.0, TimeWarp.SourceGenerators 1.0.0-beta.15,
TimeWarp.Mediator and Generators 15.0.0-beta.6, OpenTelemetry 1.21.0 (3 packages),
ModelContextProtocol 2.4.0, ConsoleAppFramework 5.8.1, Microsoft.Build.Utilities.Core 18.12.2.

## Requirements

- `ganda nuget outdated --update` applied; global.json SDK pin `11.0.100`.
- Major bumps compile: fix call sites rather than pin back (audit policy).
- `dev build` clean (warnings as errors) and `tests/ci-tests/run-ci-tests.cs` green.
- `ganda repo audit` no Error; PR CI green.

## Checklist

- [x] Packages updated via ganda nuget outdated --update
- [x] global.json SDK 11.0.100
- [x] Build clean
- [x] CI tests green (local run-ci-tests.cs)
- [ ] PR open and merged

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-13)

## Notes

- TimeWarp.SourceGenerators stays at 1.0.0-beta.11. beta.15 ships TWA0004 (#region Purpose, 290 of
  366 source files lack one) and TWA0038 (500-line cap, 44 files over). The `analyzer-suppressions`
  audit refuses `.editorconfig` severity overrides, so the pin is held through `package-pin-currency.hold` in `.editorconfig` (the sanctioned route).
  Tasks 580 (Purpose backfill) and 581 (file splits) unblock the bump.
- `ganda repo audit --fix` (`package-pin-currency`) also bumped TimeWarp.Amuru and Amuru.Tools
  2.0.0 → 3.0.0-beta.2 and TimeWarp.Terminal 1.0.2 → 2.0.0-beta.1, and refreshed `.githooks/*.cs`.
- `benchmarks/timewarp-nuru-benchmarks` does not build on master either (CliFx 3.0.1 moved its
  Attributes namespace); not part of CI and not touched here. `bench-systemcommandline` builds on
  System.CommandLine 3.0.0.
