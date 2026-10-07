# Run ganda nuget outdated and update all packages to latest

## Description

Bring every NuGet reference in the repo to its latest stable version using `ganda nuget outdated`.
Snapshot on 2026-10-07 at master `fd5f4635` (57 packages checked, 6 outdated):

| Package | Current | Latest | Type |
|---------|---------|--------|------|
| Microsoft.CodeAnalysis.CSharp | 5.6.0 | 5.9.0 | minor |
| Microsoft.Build.Utilities.Core | 18.9.6 | 18.10.1 | minor |
| Spectre.Console.Cli | 0.55.0 | 0.57.2 | minor |
| Roslynator.Analyzers | 5.0.0 | 5.0.1 | patch |
| Roslynator.CodeAnalysis.Analyzers | 5.0.0 | 5.0.1 | patch |
| Roslynator.Formatting.Analyzers | 5.0.0 | 5.0.1 | patch |

Re-run the check at task start; the list may have moved.

## Requirements

- `Microsoft.CodeAnalysis.CSharp` is referenced by the source generator / analyzers package. Its version
  sets the minimum Roslyn a consumer's SDK must ship. Confirm the 5.9.0 bump does not raise the minimum
  SDK above what the TimeWarp repos use before accepting it; if it does, pin and note it.
- Roslynator patch bumps can introduce new diagnostics. Warnings are errors here, so fix any new findings
  rather than suppressing them.
- `Microsoft.Build.Utilities.Core` is used by `source/timewarp-nuru-build`; verify the build-tasks
  package still loads in a consumer `dotnet build`.

## Checklist

- [ ] `ganda nuget outdated` re-run at task start; table above refreshed if different
- [ ] `ganda nuget outdated --update` applied (or `--prompt` to skip any pinned package)
- [ ] `ganda runfile cache --clear` then `dotnet run tests/ci-tests/run-ci-tests.cs` green
- [ ] `dotnet publish -c Release -r linux-x64 -p:PublishAot=true` on a test app still clean (no new IL warnings)
- [ ] Any new Roslynator diagnostics fixed, not suppressed
- [ ] `changelog.md` Unreleased notes the dependency bumps that affect consumers (Roslyn minimum, if changed)
- [ ] PR merged

## Notes

- Follows the beta.79 dogfood cut (483). Keep this separate from the `3.0.0` bump so the release diff
  stays a version-only change.
- `ganda nuget outdated --dry-run` is read-only and safe to re-run from any checkout.

## Session

- Created: claude 2412bd45 (2026-10-07)
