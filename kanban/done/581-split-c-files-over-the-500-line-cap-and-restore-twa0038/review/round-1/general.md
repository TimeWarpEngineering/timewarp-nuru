# Round 1 — general
**Date:** 2026-11-13
**Scope reviewed:** `git diff master...HEAD` for task 581. The 44 paths in master's `.timewarp/line-cap-baseline.json`, their new siblings, `Directory.Packages.props`, `.editorconfig`, the line-cap and suppression baselines, the devcli `Compile Include` lists, `CiTestExcludes`, and `run-standalone-tests.cs`.

## Summary

The branch splits all 44 grandfathered C# files and turns TWA0038 back on. `TimeWarp.SourceGenerators` moves from 1.0.0-beta.11 to 1.0.0-beta.15, the `package-pin-currency.hold` line is gone, and `.timewarp/line-cap-baseline.json` is `{ "files": {} }`. A read-only `find source tools tests -name "*.cs"` line count (obj/bin excluded) prints no file over 500. The task text still says task 579 left `dotnet_diagnostic.TWA0038.severity = none`; the claim tree never had that line. The live hold was the pin. Removing the hold and bumping the package is the restoration the title asks for, so the restatement is not a misread.

Product partials are statement-identical to master aside from one repeated `partial class` header per new file. The two deliberate edits are the ones Results names. `ReleaseCommand.Handler` steps 6, 7, and 10 are now helpers: every other statement matches master, and each former `Environment.ExitCode = 1; return Unit.Value;` inside those steps is a `return false` or a `return` that `Handle` turns back into the same exit code. The telemetry proof renames two local consts to `ExpectedTraceId` / `ExpectedParentSpanId` and makes one telemetry lambda expression-bodied. Test method names are unchanged except the copied `Setup` / `CleanUp` on `repl-18b`, which Results already counts. Standalone siblings of excluded runfiles are on both `CiTestExcludes` and `run-standalone-tests.cs`. The nuget-version-service partials are on both devcli `Compile Include` lists; endpoint and service globs (`*.cs`) pick up `release-command.guards.cs`. New test runfiles are mode `100755`. `InternalsVisibleTo` adds only the new runfile assembly names.

The two new CA1805 pragmas are the explicit `Mute = false` and `Unset = null` initializers the derivation tests pin. They were covered by the original file-level pragma; the new sites are narrow and explained. Fixture types that moved out of `operation-endpoint-derivation.cs` are `internal`. The extractor treats a same-compilation internal symbol as visible, which is the rule those tests already run under. `generator-26b` calls `ToString(CultureInfo.InvariantCulture)` on the transient instance id so CA1305 is fixed rather than re-baselined. None of these is a silenced or weakened check.

Recorded gates cover the change: `./bin/dev build` (8 projects, 0 warnings), `./bin/dev test` (multi-mode 2185, standalone 45 files, 0 failed), and `ganda repo audit` (exit 0, with the two pre-existing warnings named). This round did not re-run those gates.

## Issues

No issues.
