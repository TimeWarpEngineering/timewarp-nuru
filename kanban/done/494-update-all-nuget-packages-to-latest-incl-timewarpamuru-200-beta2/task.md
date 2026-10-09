# Update all NuGet packages to latest (incl. TimeWarp.Amuru 2.0.0-beta.2)

## Description
Steven wants every repo on the newest packages, pre-releases included (latest, not stable). Run `ganda nuget outdated --update` and take every package to the newest version it can, not just Amuru. TimeWarp.Amuru and TimeWarp.Amuru.Tools must end on 2.0.0-beta.2 or newer.

## Checklist
- [x] `ganda nuget outdated --dry-run`, then `ganda nuget outdated --update`
- [x] Bump `#:package ...@version` pins in runfiles; .githooks are ganda-owned, refresh via `ganda hooks install attest` instead of editing
- [x] Fix Amuru 1.x->2.0 breaking changes (timewarp-amuru documentation/release-notes/2.0.0.md): Git *Master* helpers removed (use *Default*), Git methods return result objects not bool, no "master" default branchName, removed/renamed dotnet builder options, WithStandardInput("") closes stdin
- [x] Fix other breaking changes from bumped packages
- [x] Build warning-free, tests green, `ganda repo audit` clean
- [ ] One PR, merge via `ganda pr merge`

## Session

- Created: 1429458 (2026-10-09)
- Implementation: 01a120a3 (2026-10-09)
- Review: review oracle, Claude Opus 5.5 headless (2026-10-09)

## Notes
Filed 2026-10-09 at Steven's request (Amuru 2.0 sweep). If a package can't move (e.g. a dependency cycle), record why instead of forcing it.
Related: task 481 (.NET 11 upgrade) touches the same package pins; coordinate ordering if both are in flight. Task 481 is unclaimed to-do, so this walk left the .NET 11 runtime-major pins for that task.
Release gate: TimeWarp.Nuru and TimeWarp.Nuru.Search now depend on prerelease TimeWarp.Amuru 2.0.0-beta.2. A stable Nuru 3.0.0 pack would hit NU5104, so Amuru 2.0.0 stable has to ship first (review M1).

## Results

Central package management now pins `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` at `2.0.0-beta.2`. `Shouldly` is `5.0.0-preview.2`. `BenchmarkDotNet` is `0.16.0-preview.2`. `ganda nuget outdated` stays on the stable band when the current pin is stable, so it reported Amuru 1.1.1 as up to date; the 2.0 prerelease was set by hand. `--update --force` then wrote the only two stable bumps it found, and both were reverted (see below).

No runfile uses a `#:package ...@version` pin. Unversioned directives take the central version, including `.githooks`. `dotnet list .githooks/pre-commit.cs package` and `dotnet list tools/dev-cli/dev.cs package` both resolve Amuru and Amuru.Tools to `2.0.0-beta.2`. `ganda hooks install attest` reported the hooks already installed and changed no files.

Amuru 2.0 call sites in this repo do not use the removed Git `*Master*` helpers, bool-returning Git methods, or removed dotnet builder options. `WithStandardInput` is passed clipboard text. An empty string now means immediate EOF, which is the right pipe for an empty copy.

Shouldly 5 makes `ShouldContain` case-sensitive. `tests/timewarp-nuru-tests/repl/repl-10-error-handling.cs` asserted `"Error"` against stderr that is `Test error`. Both assertions now use `"Test error"`.

Pins that stayed:

- `Microsoft.CodeAnalysis.CSharp` `5.6.0`. `5.9.0` is the newest, and it raises the minimum Roslyn that can load `TimeWarp.Nuru.Analyzers` to SDK `10.0.400+`. `global.json` still rolls forward from `10.0.100`, so a `10.0.3xx` consumer must keep loading the analyzer. `--update` wrote `5.9.0`; reverted.
- `Microsoft.Build.Utilities.Core` `18.9.6`. `18.10.1` ships `lib/net11.0` and `net472` only. The build task packs that assembly for `net10.0`. `--update` wrote `18.10.1`; reverted.
- `Microsoft.Extensions.*` `10.0.12`, `Microsoft.Data.Sqlite.Core` `10.0.12`, and `Microsoft.CodeAnalysis.NetAnalyzers` `10.0.401`. Newer bits are the .NET 11 RC line. Task 481 owns that move together with the `net11.0` TFM, CI SDK, and pack layout. Those packages are already at the latest stable 10.0 line.
- `System.CommandLine` `2.0.12`. Newest is `3.0.0-rc.1.26425.128`. `System.CommandLine.NamingConventionBinder` `2.0.0-beta5.25306.1` is still the newest binder and depends on `System.CommandLine` `2.0.0-beta5`. The benchmark commands use that binder.
- `Serilog` `4.4.0`. The only newer listing is the CI build `4.4.1-dev-02447`, not a release prerelease.

`benchmarks/timewarp-nuru-benchmarks` is commented out of `timewarp-nuru.slnx`. It does not compile against CliFx `3.0.1` (`CliFx.Attributes` now lives in `CliFx.Binding`). That pin did not change here, and the project is outside the CI build.

The PR and `ganda pr merge` stay for the host open-pr and merge nodes.

### How to validate

Smoke:

```bash
dotnet build timewarp-nuru.slnx --nologo
dotnet run --file tests/ci-tests/run-ci-tests.cs
dotnet list tools/dev-cli/dev.cs package --include-prerelease
ganda repo audit
```

Expect:

- Solution build finishes with 0 warnings and 0 errors.
- CI tests report Total 1875, Passed 1869, Skipped 6, and exit 0. There is no Failed line.
- `dotnet list` shows `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` at `2.0.0-beta.2`.
- `ganda repo audit` reports Passed 30, Failed 0.

### Review disposition

- Rounds: 1. Effort 1, general reviewer only.
- Final counts: bug 0, suggestion 1 (wontfix), nit 0. Open: 0.
- Disposition: **accepted-exceptions**. M1 (prerelease Amuru dependency in shipped packages) is wontfix for now and recorded as a release gate for stable Nuru 3.0.0 in Notes.
- Artifacts: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.
