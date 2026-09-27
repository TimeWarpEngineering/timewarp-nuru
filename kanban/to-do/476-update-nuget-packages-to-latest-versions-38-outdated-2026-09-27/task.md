# Update NuGet packages to latest versions (38 outdated, 2026-09-27)

## Description

`ganda nuget outdated` on timewarp-nuru master (2026-09-27) reports **38 outdated packages: 5 major,
10 minor, 23 patch**. Previous rounds: tasks 407, 424, 447.

Run `ganda nuget outdated` first for the full, untruncated list with exact versions and paste it into Results.
Summary of what it showed:

| Group | Current → latest stable | Kind |
|-------|-------------------------|------|
| Microsoft.Extensions.* (16 packages), Microsoft.Data.Sqlite | 10.0.9 → 10.0.x | patch |
| Microsoft.CodeAnalysis.NetAnalyzers, SQLitePCLRaw.bundle, System.CommandLine (2.0.9 → 2.0.12), CliFx (3.0.0 → 3.0.1), TimeWarp.Terminal (1.0.0 → 1.0.2), TimeWarp.Jaribu | | patch |
| Microsoft.CodeAnalysis.* (3 packages) | 5.6.0 → 5.9.0 | minor |
| Microsoft.Build.* | 18.7.1 → 18.x | minor |
| OpenTelemetry, OpenTelemetry.Exporter.* (3) | 1.16.0 → 1.19.1 | minor |
| Serilog | 4.3.1 → 4.4.0 | minor |
| TimeWarp.Amuru, TimeWarp.Amuru.* | 1.0.0 → 1.1.1 | minor |
| ICSharpCode.Decompiler | 10.x → 11.x | **major** |
| ModelContextProtocol | 1.4.0 → 2.2.0 (1.4.1 same-major) | **major** |
| Roslynator.Analyzers / CodeAnalysis.Analyzers / Formatting.Analyzers | 4.15.0 → 5.0.0 (4.16.1 same-major) | **major** |

## Requirements

1. Update every package to its **latest stable** version (`ganda nuget outdated --update` or by hand in
   `Directory.Packages.props`). Stay on stable; do not move stable packages to prereleases.
2. Commit in groups so a regression is easy to bisect: patches, then minors, then each major on its own commit.
3. **Take care with these:**
   - **Microsoft.CodeAnalysis.\*** (analyzers/generator): raising the Roslyn reference raises the minimum
     compiler for Nuru consumers. Keep the generator/analyzer projects on the Roslyn version they are
     deliberately pinned to (check for a comment or an explicit pin); update only where it is safe, and record
     the decision.
   - **ICSharpCode.Decompiler 11**: shipped inside the package's `analyzers/` and `build/` payload. The package
     layout gate (task 462, `NupkgLayoutCheck.NuruRequiredPackageEntries`) and the csproj parity test must
     still pass.
   - **ModelContextProtocol 2.x**: the MCP server is frozen for features; this is a dependency bump only.
     If 2.x needs non-trivial API changes, take the latest 1.x (1.4.1) instead and record why.
   - **Roslynator 5**: new analyzers under warnings-as-errors. Fix real findings; do not blanket-suppress.
     If fixes are large, take 4.16.1 instead and record why.
4. Release build with warnings as errors, the full CI test gate, the samples check, and the package layout
   gate all pass.
5. Re-run `ganda nuget outdated` at the end and paste the result: every package latest, or a stated reason
   for each one left behind.

## Checklist

- [ ] Patch updates
- [ ] Minor updates (Roslyn decision recorded)
- [ ] ICSharpCode.Decompiler major
- [ ] ModelContextProtocol major (or 1.4.1 with reason)
- [ ] Roslynator major (or 4.16.1 with reason)
- [ ] Build, tests, samples, package layout gate pass
- [ ] Final `ganda nuget outdated` in Results

## Notes

- Implementer: **commit and push your changes before reporting done.**
- Run the build and test gate in the foreground. If a major needs work that cannot be finished here, return
  `ORACLE_RESULT: Blocked — <what is needed>` rather than half-migrating.
