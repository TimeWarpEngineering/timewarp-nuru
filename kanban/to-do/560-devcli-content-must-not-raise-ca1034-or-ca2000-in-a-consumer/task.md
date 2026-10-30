# DevCli content must not raise CA1034 or CA2000 in a consumer

## Description

TimeWarp.Nuru.DevCli content is compiled into the consumer. 4.0.0-beta.9 still raises two analyzer codes there, so consumers keep `NoWarn` entries that exist only for package content.

- **CA1034** — nested `Handler` in four content files:
  - `source/timewarp-nuru-devcli/content/any/endpoints/clean-command.cs`
  - `source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs`
  - `source/timewarp-nuru-devcli/content/any/endpoints/check-version-command.cs`
  - `source/timewarp-nuru-devcli/content/any/endpoints/self-install-command.cs`
- **CA2000** — `new HttpClientHandler` in `NuGetVersionService()` (`source/timewarp-nuru-devcli/content/any/services/nuget-version-service.cs`). The handler is owned by the service, but the analyzer does not see that transfer.

Nuru 558 kept CA1034 as a repo preference (`.editorconfig` sets `dotnet_diagnostic.CA1034.severity = none` because nested types encapsulate handlers). That file does not ship inside the package. A consumer whose own `.editorconfig` leaves CA1034 at its default sees the four nested handlers. The same 558 Results list CA2000 as remaining when dev-cli `NoWarn` is cleared.

Fix the content, or ship a suppression that applies only to those content files when a consumer compiles them. A consumer build that does not copy Nuru's `.editorconfig` or `NoWarn` must be clean for CA1034 and CA2000.

## Requirements

- A consumer project that compiles DevCli content, with CA1034 and CA2000 at warning and absent from `NoWarn`, builds with neither code.
- Nuru may keep CA1034 off inside this repo. Package content must not depend on that preference.
- Do not ship a package `.editorconfig` that turns those rules off for the whole consumer project.

## Checklist

- [ ] Clear CA1034 on the four nested `Handler` types, or ship a content-scoped suppression the consumer compile picks up
- [ ] Clear CA2000 on `NuGetVersionService()` the same way (ownership the analyzer accepts, or a content-scoped suppression)
- [ ] Prove it with a consumer compile that does not copy this repo's `.editorconfig` or dev-cli `NoWarn`
- [ ] Ship in the next DevCli prerelease after 4.0.0-beta.9

## Notes

- Filed from timewarp-ganda task 530, review round 1 finding M1. Ganda keeps `CA1034;CA2000` in `tools/dev-cli/Directory.Build.props` until this ships, and cites this id there.
- Nuru 558 (`kanban/done/558-devcli-source-content-never-causes-a-consumer-diagnostic-auto-generated-headers-rcs1194-on-review-suppressed-rules`): CA1034 classified as preference (6 sites) and kept; CA2000 listed as one remaining DevCli-content hit when `NoWarn` is cleared.
- Ganda evidence: removing those two codes from dev-cli `NoWarn` fails the build only in the package content paths named above.

## Session

- Created: 1016182 (2026-10-30), from timewarp-ganda task 530
