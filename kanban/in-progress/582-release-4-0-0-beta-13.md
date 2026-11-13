# Release 4.0.0-beta.13

## Description

[Brief description of the task]

## Checklist

- [ ] Item 1
- [ ] Item 2

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-13)

## Notes

[Additional context]

## Description

Cut `4.0.0-beta.13`. Ships what landed on master since beta.12: 572 (pwsh workflow default), 574 (CI code coverage via Jaribu 1.0.0-beta.16), 579 (.NET 11 GA package set), 580 (Purpose regions in every source file, TWA0004 clean for DevCli consumers), 581 (all files under the 500-line cap, TWA0038 clean for DevCli consumers, SourceGenerators 1.0.0-beta.15).

Why now: DevCli consumers on SourceGenerators beta.14+ hit TWA0038 on the beta.12 content files `release-command.cs` and `nuget-version-service.cs`.

## Checklist

- [x] Bump `<Version>` in `source/Directory.Build.props` to 4.0.0-beta.13
- [ ] PR #391 merged, master CI green
- [ ] `dev release --dry-run` then `dev release` from clean synced master
- [ ] Packages on nuget.org

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-14), Steve: "release it"
