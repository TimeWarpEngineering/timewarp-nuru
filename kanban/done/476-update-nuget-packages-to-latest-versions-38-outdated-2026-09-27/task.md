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

- [x] Patch updates
- [x] Minor updates (Roslyn decision recorded)
- [x] ICSharpCode.Decompiler major
- [x] ModelContextProtocol major (or 1.4.1 with reason)
- [x] Roslynator major (or 4.16.1 with reason)
- [x] Build, tests, samples, package layout gate pass
- [x] Final `ganda nuget outdated` in Results

## Results

Initial `ganda nuget outdated` (2026-09-27, this worktree, before bumps) reported **38 outdated
packages (5 major, 10 minor, 23 patch)**. Untruncated current → latest stable lines:

```
Microsoft.Extensions.DependencyInjection 10.0.9 -> 10.0.12
Microsoft.Extensions.DependencyInjection.Abstractions 10.0.9 -> 10.0.12
Microsoft.Extensions.Configuration 10.0.9 -> 10.0.12
Microsoft.Extensions.Configuration.Binder 10.0.9 -> 10.0.12
Microsoft.Extensions.Configuration.CommandLine 10.0.9 -> 10.0.12
Microsoft.Extensions.Configuration.Json 10.0.9 -> 10.0.12
Microsoft.Extensions.Configuration.EnvironmentVariables 10.0.9 -> 10.0.12
Microsoft.Extensions.Hosting 10.0.9 -> 10.0.12
Microsoft.Extensions.Logging 10.0.9 -> 10.0.12
Microsoft.Extensions.Logging.Console 10.0.9 -> 10.0.12
Microsoft.Extensions.Logging.Abstractions 10.0.9 -> 10.0.12
Microsoft.Extensions.Options 10.0.9 -> 10.0.12
Microsoft.Extensions.Options.ConfigurationExtensions 10.0.9 -> 10.0.12
Microsoft.Extensions.Options.DataAnnotations 10.0.9 -> 10.0.12
Microsoft.Extensions.Configuration.UserSecrets 10.0.9 -> 10.0.12
Microsoft.Extensions.Http 10.0.9 -> 10.0.12
TimeWarp.Amuru 1.0.0 -> 1.1.1
TimeWarp.Amuru.Tools 1.0.0-beta.2 -> 1.1.1
TimeWarp.Terminal 1.0.0 -> 1.0.2
TimeWarp.Jaribu 1.0.0-beta.13 -> 1.0.0-beta.15
Serilog 4.3.1 -> 4.4.0
OpenTelemetry 1.16.0 -> 1.19.1
OpenTelemetry.Exporter.OpenTelemetryProtocol 1.16.0 -> 1.19.1
OpenTelemetry.Extensions.Hosting 1.16.0 -> 1.19.1
CliFx 3.0.0 -> 3.0.1
System.CommandLine 2.0.9 -> 2.0.12
ModelContextProtocol 1.4.0 -> 2.2.0
Microsoft.Data.Sqlite.Core 10.0.9 -> 10.0.12
SQLitePCLRaw.bundle_e_sqlite3 3.0.3 -> 3.0.5
Roslynator.Analyzers 4.15.0 -> 5.0.0
Roslynator.CodeAnalysis.Analyzers 4.15.0 -> 5.0.0
Roslynator.Formatting.Analyzers 4.15.0 -> 5.0.0
Microsoft.CodeAnalysis.NetAnalyzers 10.0.301 -> 10.0.401
Microsoft.CodeAnalysis.CSharp.CodeStyle 5.6.0 -> 5.9.0
Microsoft.CodeAnalysis.CSharp 5.6.0 -> 5.9.0
Microsoft.CodeAnalysis.Analyzers 5.6.0 -> 5.9.0
ICSharpCode.Decompiler 10.1.1.8388 -> 11.1.0.9782
Microsoft.Build.Utilities.Core 18.7.1 -> 18.10.1
```

`Microsoft.CodeAnalysis.BannedApiAnalyzers` 5.6.0 was already latest. The ASCII table from
`ganda nuget outdated` truncates names; the lines above are the per-package scan.

Commits, in bisect order:

1. `f549d5d3` — 23 patch bumps (Extensions 10.0.12, Sqlite.Core 10.0.12, NetAnalyzers 10.0.401,
   SQLitePCLRaw.bundle_e_sqlite3 3.0.5, System.CommandLine 2.0.12, CliFx 3.0.1, TimeWarp.Terminal 1.0.2,
   TimeWarp.Jaribu 1.0.0-beta.15). Jaribu has no stable release; it stayed on the prerelease line.
2. `a4fcb5f4` — minors, with two pins (see below).
3. `acadfd60` — ICSharpCode.Decompiler 11.1.0.9782.
4. `b1a9a3e0` — ModelContextProtocol 2.2.0.
5. `dfc3cee3` — Roslynator.Analyzers / CodeAnalysis.Analyzers / Formatting.Analyzers 5.0.0.

**Roslyn.** `Microsoft.CodeAnalysis.CSharp` stays at **5.6.0**. `timewarp-nuru-analyzers` compiles
against it and sets `SuppressDependenciesWhenPacking`, so the consumer's compiler supplies
`Microsoft.CodeAnalysis`. Moving to 5.9.0 would raise that minimum. There was no separate pin comment
before this task; the shared CPM version is the pin, and the comment now lives next to the
`PackageVersion` in `Directory.Packages.props`. `Microsoft.CodeAnalysis.CSharp.CodeStyle` (repo-local
style analyzer, no dependency on the CSharp package) and `Microsoft.CodeAnalysis.Analyzers`
(analyzer-authoring rules) moved to 5.9.0. `BannedApiAnalyzers` was already current at 5.6.0.

**MSBuild.** `Microsoft.Build.Utilities.Core` moved 18.7.1 → **18.9.6**, not 18.10.1. 18.10.1 ships
only `lib/net11.0`, warns that it does not support net10.0, and no longer copies
`Microsoft.Build.Framework.dll` into the build-task output. Packing TimeWarp.Nuru then fails with
NU5019 (file not found). 18.9.6 is the latest stable that still has `lib/net10.0`. The packed DLL set
is unchanged.

**ICSharpCode.Decompiler 11.1.0.9782** still ships `lib/netstandard2.0/ICSharpCode.Decompiler.dll`.
The analyzer and `timewarp-nuru.csproj` pack paths did not change. The analyzer compiled with 0
warnings. The layout gate found all 16 required entries.

**ModelContextProtocol 2.2.0** compiled with no source changes. The server already uses
`AddMcpServer`, `WithStdioServerTransport`, and `[McpServerTool]`. No fallback to 1.4.1.

**Roslynator 5.0.0** did not produce new warnings on the Release build (warnings as errors). No
suppressions were added. No fallback to 4.16.1.

`TimeWarp.Amuru.Tools` moved from prerelease `1.0.0-beta.2` to stable `1.1.1` (same version as
`TimeWarp.Amuru`).

Final `ganda nuget outdated` reports **2 outdated packages (2 minor)**:

```
Microsoft.CodeAnalysis.CSharp 5.6.0 -> 5.9.0
Microsoft.Build.Utilities.Core 18.9.6 -> 18.10.1
```

Both are the pins above. Every other central package is at its latest stable (Jaribu and
`System.CommandLine.NamingConventionBinder` remain on their existing prerelease lines; no stable
exists for them).

Verification (`dotnet run --file tools/dev-cli/dev.cs -- workflow --attestation off`):

- Release build of the shipping projects: 0 warnings, 0 errors.
- Package layout gate: `TimeWarp.Nuru.3.0.0-beta.78.nupkg` contains all 16 required payload entries.
- Samples: 64/64 built.
- CI tests: multi-mode grand total 1747, 1740 passed, 7 skipped, 0 failed. Standalone phase passed.
  Pipeline SUCCEEDED (exit 0).

### How to validate

Smoke:

```bash
dotnet build timewarp-nuru.slnx -c Release
dotnet run --file tools/dev-cli/dev.cs -- workflow --attestation off
ganda nuget outdated
```

Expect:

- `dotnet build timewarp-nuru.slnx -c Release` exits 0 with 0 warnings and 0 errors.
- Workflow prints `Package layout verified: TimeWarp.Nuru.3.0.0-beta.78.nupkg contains all 16 required payload entries.`,
  `64/64 samples built successfully`, multi-mode `Total: 1747`, `Passed: 1740`, `Skipped: 7`, then
  `Pipeline SUCCEEDED`.
- `ganda nuget outdated` lists only `Microsoft.CodeAnalysis.CSharp 5.6.0 -> 5.9.0` and
  `Microsoft.Build.Utilities.Core 18.9.6 -> 18.10.1`, and ends with `2 outdated package(s) (2 minor)`.

### Review disposition

- **Outcome:** clean
- **Effort / roster:** 1 — general only
- **Rounds:** 1
- **Final counts:** bug 0, suggestion 0, nit 0 (0 open, 0 fixed, 0 wontfix)
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`
- Re-verified during review: `dotnet build timewarp-nuru.slnx -c Release` is 0 warnings, 0 errors. `TimeWarp.Nuru.3.0.0-beta.78.nupkg` contains all 16 required payload entries; packed decompiler, MSBuild, and Roslyn DLLs match the pinned package assets. `ganda nuget outdated` reports the two pins above and `2 outdated package(s) (2 minor)`.

## Notes

- Implementer: **commit and push your changes before reporting done.**
- Run the build and test gate in the foreground. If a major needs work that cannot be finished here, return
  `ORACLE_RESULT: Blocked — <what is needed>` rather than half-migrating.
- 2026-09-28: review oracle (ganda task work, tw-implementation-review effort 1). Round 1 general — disposition clean. Next host nodes: open-pr / done (no apply-review sibling).
