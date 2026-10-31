# Upgrade timewarp-nuru to .NET 11

## Description

Move TimeWarp.Nuru from **.NET 10** (`net10.0` / SDK `10.0.100`) to **.NET 11** (`net11.0`).

As of 2026-10-02 analysis on master (`06e267f9`):

- Repo TFM default is `net10.0` (root `Directory.Build.props`).
- `global.json` pins SDK `10.0.100` (`rollForward: latestMinor`, `allowPrerelease: false`).
- CI uses `actions/setup-dotnet@v4` with `dotnet-version: '10.0.x'`.
- Machine has .NET 11 SDKs up to `11.0.100-rc.3.26522.148` (checked 2026-10-31); GA expected November 2026.

This task covers the full product/CI/docs bump. Do **not** treat a preview-only pin as done unless the team explicitly accepts shipping on preview/RC until GA.

## Goal (implementer restatement)

Move the whole repo from .NET 10 to .NET 11 in one PR and update every dependency to its latest line.

Acceptance criteria:

- No `net10.0` (or stale `net9.0`) TFM is left in props, csproj, runfile fixtures, packaging paths, the nupkg layout gate, tests, the dev CLI, or docs. The package lays out `lib/net11.0` and `build/net11.0`.
- `global.json` pins the newest installed .NET 11 SDK (RC 3, `11.0.100-rc.3.26522.148`) with `allowPrerelease: true`, and the reason is written down. The reason: GA is not out yet. CI's `setup-dotnet` installs the same line.
- Every `PackageVersion`, local tool, and `#:package` / `#:sdk` directive moves to its latest version. Runtime-aligned Microsoft packages go to `11.0.0-rc.3`. `TimeWarp.Mediator.Contracts` 14 is replaced by `TimeWarp.Mediator` 15.0.0-beta.4, and any API change that needed is recorded. Anything held back has a reason in Results.
- Build (warnings as errors), CI tests, verify-samples, and the package layout gate pass locally. `ganda repo audit` is clean.
- Results has an old-to-new version table, the SDK decision, consumer impact, and How to validate.

## Requirements

1. All shipping projects, samples, benchmarks, and test apps target **`net11.0`** (no leftover `net10.0` TFMs in props/csproj).
2. `global.json` SDK version matches an agreed .NET 11 SDK line (prefer latest RC/GA when available; if preview is required temporarily, set `allowPrerelease` deliberately and document why).
3. Central package versions that track the runtime major (`Microsoft.Extensions.*`, `Microsoft.Data.Sqlite.Core`, `Microsoft.CodeAnalysis.NetAnalyzers`, etc.) move to the .NET 11-aligned stable (or RC) line.
4. Hard-coded `net10.0` packaging paths and nupkg layout gates move to `net11.0` and still pass (`NupkgLayoutCheck`, csproj parity tests, pack output under `build/net11.0` / `lib/net11.0`).
5. GitHub Actions installs .NET 11 (`setup-dotnet` `11.0.x` or pinned RC/GA).
6. Docs that show `bin/Release/net10.0/...` paths and developer conventions are updated; stale `net9.0` mentions in conventions docs are fixed.
7. Upstream TimeWarp packages consumed by this repo (Amuru, SourceGenerators, Build.Tasks, Jaribu, Mediator.*, Terminal, Builder) either already support net11 or are upgraded/published first — record blockers.
8. Full CI gate green: restore/build (warnings-as-errors where enforced), tests, verify-samples, package layout gate.
9. **Update every Nuru dependency to its latest version** (Steve 2026-10-31: "update all of nuru deps roll it into task 481"). Cover every `PackageVersion` in `Directory.Packages.props` (61 entries), `.config/dotnet-tools.json`, and `#:package` / `#:sdk` directives in runfiles and samples. Prereleases are acceptable when that is the newest line we own or the net11-aligned line.
   - **TimeWarp.Mediator 15.0.0-beta.4** (released 2026-10-31 on Nuru 4.0.0-beta.11). 15 publishes only `TimeWarp.Mediator` and `TimeWarp.Mediator.Generators`; `TimeWarp.Mediator.Contracts` and `.Analyzers` stop at 14.0.0. Replace `TimeWarp.Mediator.Contracts` 14.0.0 with `TimeWarp.Mediator` 15.0.0-beta.4, bump Generators, and record every API change 15 required.
   - Other TimeWarp packages (Amuru, Jaribu, Terminal, SourceGenerators, Build.Tasks, Builder): latest published. Use the org feed `https://nuget.pkg.github.com/TimeWarpEngineering/index.json` as well as nuget.org when checking.
   - Record a table of old to new versions in Results, and any package held back with the reason.
9. No product API redesign in this task unless forced by a .NET 11 breaking change; record any breaking consumer impact in Results.

## Checklist

### Toolchain / SDK (do first)
- [x] Decide SDK pin: RC vs wait-for-GA vs temporary preview; update `global.json` accordingly (`allowPrerelease` if needed)
- [x] Ensure local + CI agents can install the chosen SDK
- [x] Review `.config/dotnet-tools.json` tools for .NET 11 SDK compatibility

### TFMs
- [x] Root `Directory.Build.props` → `net11.0`
- [x] Nested TFM overrides → `net11.0`: `samples/`, `benchmarks/`, `tests/test-apps/`, `source/timewarp-nuru-devcli/`
- [x] `source/Directory.Build.props` `AnalyzerDependencyTfm` → `net11.0`
- [x] Confirm no csproj still hard-codes `net10.0` as TargetFramework

### Hard-coded TFM paths / packaging (coupled to TFM bump)
- [x] `source/timewarp-nuru-build/...` PackagePath `build/net10.0` → `build/net11.0`
- [x] `source/timewarp-nuru-build/build/TimeWarp.Nuru.Build.targets` default `_NuruBuildTaskDir`
- [x] `samples/Directory.Build.targets` and `source/timewarp-nuru-search/` `_NuruBuildTaskDir`
- [x] `source/timewarp-nuru/timewarp-nuru.csproj` packed `build/net10.0/*` includes
- [x] `source/timewarp-nuru-devcli/.../nupkg-layout-check.cs` required entry list
- [x] DevCli / layout tests under `tests/timewarp-nuru-tests/devcli/` (fixtures + assertions)
- [x] `benchmarks/.../program.cs` `targetFrameworkMoniker`

### Packages (Directory.Packages.props)
- [x] Bump `Microsoft.Extensions.*` (16 packages currently `10.0.12`) to .NET 11 line
- [x] Bump `Microsoft.Data.Sqlite.Core` (`10.0.12`)
- [x] Bump `Microsoft.CodeAnalysis.NetAnalyzers` (`10.0.401`)
- [x] Revisit **`Microsoft.Build.Utilities.Core`**: currently pinned at `18.9.6` because `18.10.1` drops `lib/net10.0` (net11-only). On net11, move to latest that supplies `net11.0` and still copies Framework DLLs into the build-task payload
- [x] Revisit Roslyn pins (`Microsoft.CodeAnalysis.CSharp` etc.) vs SDK-bundled compiler; keep consumer minimum deliberate
- [x] Serilog.Extensions.Logging / OpenTelemetry / MCP / benchmark CLI packages: bump only as needed for net11
- [x] Aspire sample (`samples/aspire-otel`, `#:sdk Aspire.AppHost.Sdk@13.5.3`): confirm Aspire line supports .NET 11; bump SDK directive if required
- [x] Upstream TimeWarp packages: confirm net11-ready versions exist; bump CPM entries
- [ ] TimeWarp.Mediator 14.0.0 → 15.0.0-beta.4 (Contracts → TimeWarp.Mediator; Generators bumped); API changes recorded — **held at 14.0.0**, see Results (TimeWarp.Contracts upstream blocker; 15 removes the Nuru endpoint handler interfaces)
- [x] Every other dependency (CPM, dotnet-tools, `#:package`/`#:sdk`) at latest; old→new table in Results

### CI / images
- [x] `.github/workflows/workflow.yml` `dotnet-version: '10.0.x'` → `11.0.x` (or pinned RC/GA)
- [x] No Dockerfiles today; if any appear before merge, align base images

### Docs / conventions / samples text
- [x] `documentation/user/guides/deployment.md` path examples
- [x] `documentation/developer/guides/aot-compilation.md` path examples
- [x] `documentation/developer/standards/dotnet-conventions.md` still says `Target net9.0` — update to `net11.0`
- [x] changelog entry for the TFM/SDK bump when releasing

### Verification
- [x] `dotnet restore` / `dotnet build` clean under warnings-as-errors policies
- [x] Full test + verify-samples + nupkg layout gate
- [x] Spot-check analyzer load in a sample (Logging.Abstractions TFM under `analyzers/dotnet/cs`)
- [x] Record final SDK version, package versions, and any deferred upstream blockers in Results

## Notes

### Impact analysis (master, 2026-10-02)

**Current baseline**

| Area | Current |
|------|---------|
| SDK (`global.json`) | `10.0.100`, `allowPrerelease: false` |
| Default TFM | `net10.0` |
| CI | `setup-dotnet` `10.0.x` on `ubuntu-latest` |
| Extensions / Sqlite | `10.0.12` |
| NetAnalyzers | `10.0.401` |
| MSBuild.Utilities.Core | `18.9.6` (deliberate pin; 18.10.1 is net11-only) |
| Interceptors | `InterceptorsNamespaces` (already .NET 10+ property name) |
| Aspire sample | `Aspire.AppHost.Sdk@13.5.3` |
| Docker | none in repo |

**TFM touch points**

- Root + nested `Directory.Build.props` (samples, benchmarks, test-apps)
- Explicit `<TargetFramework>net10.0</TargetFramework>` in `timewarp-nuru-devcli.csproj`
- `AnalyzerDependencyTfm` in `source/Directory.Build.props`

**Hard-coded `net10.0` strings (not only TFM props)** — high risk if missed:

- Build task pack path and targets (`timewarp-nuru-build`)
- Main package payload includes (`timewarp-nuru.csproj` → `build/net10.0/...`)
- Layout gate + tests (`nupkg-layout-check.cs`, `nupkg-layout-0*.cs`, packable-projects fixtures)
- Search project local `_NuruBuildTaskDir`
- BenchmarkDotNet TFM string in `program.cs`
- Docs under `documentation/`

**No Aspire central packages** in `Directory.Packages.props` (empty Aspire ItemGroup); Aspire is file-based SDK reference in the sample only.

### Dependency update order (what must update first)

Order matters — follow this sequence:

1. **Toolchains / SDK** — `global.json` (+ `allowPrerelease` policy) and agent/CI ability to install .NET 11. Without this, TFM restore fails.
2. **Upstream blockers** — TimeWarp.* packages and Aspire AppHost SDK must offer net11-compatible builds *before* or *as* CPM bumps; otherwise restore/build blocks the TFM move.
3. **TFMs** — flip `net10.0` → `net11.0` in props/csproj and `AnalyzerDependencyTfm` in the same change set as the hard-coded path renames below (they must stay consistent).
4. **Hard-coded packaging paths + layout gates** — same commit/PR slice as TFMs (`build/net11.0`, tests, `_NuruBuildTaskDir`).
5. **NuGet / CPM packages** — runtime-aligned Microsoft.* (Extensions, Sqlite, NetAnalyzers), then lift `Microsoft.Build.Utilities.Core` off the 18.9.6 pin, then Roslyn/analyzer decisions, then other deps.
6. **CI workflows** — `setup-dotnet` to 11.0.x (can land with SDK pin; must be before merge to master so CI can build the TFM bump).
7. **Docs / conventions / changelog** — after green build, or same PR if cheap.

### Risks / blockers

- **Preview/RC vs GA**: GA expected Nov 2026. Shipping library consumers to `net11.0` before GA forces them onto preview/RC runtimes. Prefer RC go-live or wait for GA for the public package TFM; preview is OK for a branch spike.
- **`allowPrerelease: false`**: current global.json forbids preview SDKs — must change if targeting preview/RC builds that are still labeled prerelease.
- **Microsoft.Build.Utilities.Core pin**: today stuck on 18.9.6 for net10 payload copying. net11 unlocks 18.10.1+, but verify Framework DLL still lands in `build/net11.0` (historical MSB4062 / missing task payload — see tasks 389/461).
- **Roslyn / analyzer host**: raising `Microsoft.CodeAnalysis.CSharp` raises minimum compiler for analyzer consumers; keep the deliberate pin policy from task 476.
- **Upstream TimeWarp packages**: Amuru, SourceGenerators, Build.Tasks, Jaribu, Mediator.*, Terminal, Builder may need their own net11 releases first.
- **Aspire sample**: AppHost SDK 13.5.3 may not support net11 until a newer Aspire line — isolate sample breakage from core library upgrade if needed.
- **API / SDK behavior changes**: NativeAOT CLI and MSBuild server defaults shifted in .NET 11 previews — watch AOT publish samples and build-task hosting.
- **Stale docs**: `dotnet-conventions.md` still says net9.0; path examples still say net10.0 — easy to miss in review.

### Out of scope

- Product code implementation is for whoever executes this task; the analysis that created this card did not change product code.
- Unrelated package majors that are not required for net11.

## Results

### SDK and TFM

- `global.json` pins `11.0.100-rc.3.26522.148` (the newest installed .NET 11 SDK) with `rollForward: latestFeature` and `allowPrerelease: true`. The prerelease flag is there because .NET 11 is not GA yet (GA is expected November 2026). Once GA ships, move to `11.0.100` and `allowPrerelease: false`.
- CI: `.github/actions/setup-dotnet` installs `11.0.x` with `dotnet-quality: preview` (same comment: drop it at GA). The separate "Setup .NET 11" step in `workflow.yml` for the DevCli consumer check is gone because the whole job is on .NET 11 now. `tests/devcli-consumer-check/global.json` is deleted, so the check uses the root pin.
- Every `net10.0` TFM, packaging path, layout-gate entry, test fixture, dev-CLI string, and doc path is now `net11.0`. Historical mentions (changelog, 3.0 migration guide, blog posts, and the kanban 461 incident notes) still say `net10.0` on purpose. `dotnet-conventions.md` says `net11.0` (it said `net9.0`).
- **Release gate:** the package's `Microsoft.Extensions.*` dependencies are `11.0.0-rc.3`, so consumers get RC runtime packages. Do not cut a stable Nuru release from this until .NET 11 is GA and the RC pins move to `11.0.x` GA, unless Steve accepts shipping on RC.

### Dependency versions (old → new)

| Package | Old | New | Note |
|---|---|---|---|
| Microsoft.Extensions.* (14 kept) | 10.0.12 | 11.0.0-rc.3.26522.148 | .NET 11 line |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.12 | removed | in the .NET 11 shared framework (NU1510) |
| Microsoft.Extensions.Options | 10.0.12 | removed | in the .NET 11 shared framework (NU1510) |
| Microsoft.Data.Sqlite.Core | 10.0.12 | 11.0.0-rc.3.26522.148 | |
| Microsoft.CodeAnalysis.NetAnalyzers | 10.0.401 | 11.0.100-rc.3.26522.148 | |
| Microsoft.Build.Utilities.Core | 18.9.6 | 18.11.0 | pin lifted; `build/net11.0` still carries Microsoft.Build.Framework.dll and the rest (layout gate: 18/18) |
| Microsoft.CodeAnalysis.CSharp | 5.6.0 | 5.9.0 | every .NET 11 SDK ships a newer Roslyn (rc.3 has 5.12) |
| ICSharpCode.Decompiler | 11.1.0.9782 | 11.2.0.10050 | |
| TimeWarp.Contracts | 2.0.0-beta.24 | 2.0.0-beta.29 | |
| OpenTelemetry (+ Exporter.OTLP, Extensions.Hosting) | 1.19.1 | 1.20.1 | latest stable; 1.21.0-rc.1 not taken |
| ModelContextProtocol | 2.2.0 | 2.3.0 | `McpClient.ServerInfo` is nullable now (one test fixed) |
| ConsoleAppFramework | 5.7.13 | 5.8.0 | benchmarks |
| System.CommandLine | 2.0.12 | 2.0.14 | benchmarks; 3.0.0-rc.3 not taken (major, NamingConventionBinder is pinned to 2.0 beta5) |
| MessagePack | 2.5.303 | removed | the pin was only for Aspire 13.5.3; Aspire.Hosting 13.6.4 references 2.5.307 itself |
| Aspire.AppHost.Sdk (`#:sdk`) | 13.5.3 | 13.6.4 | `samples/aspire-otel` builds |
| roslynator.dotnet.cli (tool) | 0.10.1 | 1.0.1 | |
| dotnet-outdated-tool (tool) | 4.6.9 | 4.8.1 | |
| TimeWarp.Mediator.Contracts / .Generators | 14.0.0 | **14.0.0 (held)** | see below |

Already at their latest, so unchanged: TimeWarp.Amuru(.Tools) 2.0.0, TimeWarp.Builder 1.0.0, TimeWarp.Terminal 1.0.2, TimeWarp.Jaribu 1.0.0-beta.15, TimeWarp.Build.Tasks 1.0.0, TimeWarp.SourceGenerators 1.0.0-beta.11, Shouldly 5.0.0-preview.2, Serilog 4.4.0 (4.4.1 is a dev build only), Serilog.Sinks.Console 6.1.1, Serilog.Extensions.Logging 10.0.0, CliFx 3.0.1, Cocona(.Lite) 2.2.0, CommandLineParser 2.9.1, McMaster 5.1.0, PowerArgs 4.0.3, Spectre.Console.Cli 0.57.2 (only a 1.0 alpha is newer), BenchmarkDotNet 0.16.0-preview.2, SQLitePCLRaw.bundle_e_sqlite3 3.0.5, GlobalUsingsAnalyzer 1.4.0, Roslynator.* 5.0.1, BannedApiAnalyzers 5.6.0, CSharp.CodeStyle 5.9.0, CodeAnalysis.Analyzers 5.9.0, dotnet-format 5.1.250801, benchmarkdotnet.tool 0.12.1. None of the consumed TimeWarp packages are on the org GitHub feed (it only has Taratibu, Ganda, Flexbox, and Zana), so nuget.org is the source of truth for them. All of them have net10.0 or net11.0 builds that restore on net11.0, so no upstream release was needed for the TFM move.

### TimeWarp.Mediator 15 held back (needs its own task)

I tried 15.0.0-beta.4 and reverted it. Two independent blockers:

1. **Upstream:** every TimeWarp.Contracts release up to 2.0.0-beta.29 (timewarp-architecture `source/foundation/timewarp-contracts`) declares a nuspec dependency on `TimeWarp.Mediator.Contracts` 14.0.0. Contracts' DLL does not reference any Mediator type, so the dependency is unused. It still pulls the 14 contracts assembly into every Nuru consumer. Its `TimeWarp.Mediator.IMediator`/`IRequest` types collide with the same names in `TimeWarp.Mediator` 15, and the generator then fails with TWM008 ("cannot resolve TimeWarp.Mediator.IMediator"). The fix is a TimeWarp.Contracts release that drops the Mediator dependency or moves it to `TimeWarp.Mediator` 15.
2. **API redesign:** 15 removes `ICommandHandler<,>`, `IQueryHandler<,>`, `IBaseRequest` and `IStreamRequest`, and handlers return `ValueTask`. Those handler interfaces are the Nuru endpoint model: about 350 uses across source (the search endpoints, the DevCli content, the analyzer's handler and operation extraction, and diagnostics), samples, docs, and tests. A local experiment (excluding the 14 contracts by hand) showed the first CS0246/TWM001 errors in `timewarp-nuru-search`. That is a breaking Nuru API change, and this task's requirement forbids an API redesign unless .NET 11 forces it.

Follow-up: file a Nuru task "Adopt TimeWarp.Mediator 15", blocked on a timewarp-architecture task to drop or upgrade TimeWarp.Contracts' Mediator dependency.

### .NET 11 changes the code needed

- **CA1307 (58 call sites on 54 lines):** .NET 11 adds `StringComparison` overloads for `string.StartsWith/EndsWith/LastIndexOf(char)`, so CA1307 now fires on the char overloads. Each call now passes `StringComparison.Ordinal`. The char overloads were already ordinal, so behavior is unchanged. The sites are in the analyzers/generator, parsing, the core (repl-commands, type-converter-registry), mcp, and mcphost.
- **NU1510:** .NET 11 ships `Microsoft.Extensions.DependencyInjection.Abstractions`, `Logging.Abstractions`, `Options`, `Primitives`, `Configuration.Abstractions`, `Hosting.Abstractions` (and more) in `Microsoft.NETCore.App`. I removed the direct references from `timewarp-nuru` and `timewarp-nuru-parsing`, and removed `#:package Microsoft.Extensions.Options` from the 7 configuration samples. `Microsoft.Extensions.Logging.Abstractions` stays (with `NoWarn="NU1510"`) in `timewarp-nuru` and `timewarp-nuru-analyzers`, but only for `GeneratePathProperty`: its DLL is still packed into `analyzers/dotnet/cs`. The package checks confirmed that.
- **Consumer impact:** the TimeWarp.Nuru package now needs net11.0, and its nuspec no longer lists DI.Abstractions, Logging.Abstractions, or Options, because the framework provides them. There are no Nuru public API changes.

### Gates run (local, SDK 11.0.100-rc.3)

- `dotnet build timewarp-nuru.slnx`: 0 warnings, 0 errors.
- `dev workflow --stage build`: passed. "Package layout verified: TimeWarp.Nuru.4.0.0-beta.11.nupkg contains all 18 required payload entries" and "lib/net11.0/TimeWarp.Nuru.dll is a Release, optimized build".
- `dev workflow --stage samples`: 68/68 samples built, including `aspire-otel/apphost.cs` on Aspire 13.6.4.
- `tests/devcli-consumer-check`: `dotnet build` with 0 warnings and 0 errors.
- `dev workflow --stage test-multi`: 2176 total, 2170 passed, 6 skipped, 0 failed.
- `dev workflow --stage test-standalone`: 0 failed.
- `ganda repo audit`: passes all checks. Boy-scout fix: `cpm-consistency` flagged the two orphaned `PackageVersion` entries (DI.Abstractions, Options) after the NU1510 cleanup, so I removed them.

### Problems found but not fixed here

These are outside CI and unrelated to .NET 11. They failed before this change too, because the package versions involved did not change.

- `benchmarks/timewarp-nuru-benchmarks` and `benchmarks/aot-benchmarks/bench-clifx`: CliFx 3.0.1 moved its attributes to `CliFx.Binding` (bumped in task 476).
- `benchmarks/aot-benchmarks/bench-mcmaster`: the McMaster 5.1.0 source generator emits `namespace <global namespace>` for a top-level class.
- `spikes/roslynk-cli`: its Roslynk API calls have drifted from the local Roslynk project.

### How to validate

Smoke:

```bash
dotnet --version                                   # 11.0.100-rc.3.26522.148 (from global.json)
ganda runfile cache --clear
dotnet run --file tools/dev-cli/dev.cs -- workflow --stage build
unzip -l artifacts/packages/TimeWarp.Nuru.4.0.0-beta.11.nupkg | grep -E "lib/|build/net|analyzers/"
dotnet run --file tools/dev-cli/dev.cs -- workflow --stage samples
dotnet run --file tools/dev-cli/dev.cs -- workflow --stage test-multi
dotnet run --file tools/dev-cli/dev.cs -- workflow --stage test-standalone
(cd tests/devcli-consumer-check && dotnet build)
ganda repo audit
```

Expect:

- Every stage ends with "Pipeline SUCCEEDED". The build stage prints "contains all 18 required payload entries" and "lib/net11.0/TimeWarp.Nuru.dll is a Release, optimized build".
- The nupkg listing shows `lib/net11.0/TimeWarp.Nuru.dll`, eleven `build/net11.0/*.dll` files (including Microsoft.Build.Framework.dll), and `analyzers/dotnet/cs/Microsoft.Extensions.Logging.Abstractions.dll`. Nothing under `net10.0`.
- Samples report 68/68 built. test-multi reports 0 failed (2170 passed, 6 skipped). Standalone reports 0 failed.
- The consumer check reports 0 warnings and 0 errors, and the audit prints "Repository passes all audit checks."
- In CI, the `setup-dotnet` step installs an 11.0 preview/RC SDK.
