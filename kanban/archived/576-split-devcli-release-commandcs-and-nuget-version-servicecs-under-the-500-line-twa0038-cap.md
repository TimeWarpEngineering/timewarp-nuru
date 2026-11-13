# Split DevCli release-command.cs and nuget-version-service.cs under the 500-line TWA0038 cap

## Description

TimeWarp.SourceGenerators 1.0.0-beta.14 added TWA0038, a 500-line cap per C# file. `TimeWarp.Nuru.DevCli` ships two content files over the cap, and every consumer compiles them into its dev CLI:

- `source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs`: 609 lines
- `source/timewarp-nuru-devcli/content/any/services/nuget-version-service.cs`: 704 lines

A consumer on SourceGenerators beta.14 cannot build its dev CLI. timewarp-mediator task 036 (2026-11-12) hit this and suppressed TWA0038 in `tools/dev-cli/dev.cs` as a stopgap. timewarp-mediator task 037 removes that suppression after this ships.

## Requirements

- Split both files so each resulting file is at most 500 lines. Split by responsibility (for example, release guards vs. tag and GitHub Release creation; feed queries vs. version comparison), not by cutting at line 500.
- Behavior is unchanged. `dev release`, `dev release --dry-run`, and `dev check-version` print the same output and refuse under the same conditions.
- New files follow the content-file layout the package already uses, so consumers pick them up with no change on their side.
- Check every other file under `content/` against the cap too.
- Release a new `TimeWarp.Nuru.DevCli` (and `TimeWarp.Nuru` if they version together) through tw-release.

## Checklist

- [ ] `release-command.cs` split, each file at most 500 lines
- [ ] `nuget-version-service.cs` split, each file at most 500 lines
- [ ] No other `content/` file over 500 lines
- [ ] Behavior unchanged; tests green
- [ ] New Nuru.DevCli version released

## Session

- Created: 4bddab61-0b48-4f1d-8a19-57e7f9d047b3 (2026-11-12), from timewarp-mediator task 036

## Notes

- Consumer follow-up: timewarp-mediator task 037.
