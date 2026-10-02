# Upgrade timewarp-nuru to .NET 11

## Description

Move TimeWarp.Nuru from **.NET 10** (`net10.0` / SDK `10.0.100`) to **.NET 11** (`net11.0`).

As of 2026-10-02 analysis on master (`06e267f9`):

- Repo TFM default is `net10.0` (root `Directory.Build.props`).
- `global.json` pins SDK `10.0.100` (`rollForward: latestMinor`, `allowPrerelease: false`).
- CI uses `actions/setup-dotnet@v4` with `dotnet-version: '10.0.x'`.
- Machine already has .NET 11 preview SDKs installed (`11.0.100-preview.5`, `preview.6`); RC1 exists publicly (go-live); GA expected November 2026.

This task covers the full product/CI/docs bump. Do **not** treat a preview-only pin as done unless the team explicitly accepts shipping on preview/RC until GA.

## Requirements

1. All shipping projects, samples, benchmarks, and test apps target **`net11.0`** (no leftover `net10.0` TFMs in props/csproj).
2. `global.json` SDK version matches an agreed .NET 11 SDK line (prefer latest RC/GA when available; if preview is required temporarily, set `allowPrerelease` deliberately and document why).
3. Central package versions that track the runtime major (`Microsoft.Extensions.*`, `Microsoft.Data.Sqlite.Core`, `Microsoft.CodeAnalysis.NetAnalyzers`, etc.) move to the .NET 11-aligned stable (or RC) line.
4. Hard-coded `net10.0` packaging paths and nupkg layout gates move to `net11.0` and still pass (`NupkgLayoutCheck`, csproj parity tests, pack output under `build/net11.0` / `lib/net11.0`).
5. GitHub Actions installs .NET 11 (`setup-dotnet` `11.0.x` or pinned RC/GA).
6. Docs that show `bin/Release/net10.0/...` paths and developer conventions are updated; stale `net9.0` mentions in conventions docs are fixed.
7. Upstream TimeWarp packages consumed by this repo (Amuru, SourceGenerators, Build.Tasks, Jaribu, Mediator.*, Terminal, Builder) either already support net11 or are upgraded/published first — record blockers.
8. Full CI gate green: restore/build (warnings-as-errors where enforced), tests, verify-samples, package layout gate.
9. No product API redesign in this task unless forced by a .NET 11 breaking change; record any breaking consumer impact in Results.

## Checklist

### Toolchain / SDK (do first)
- [ ] Decide SDK pin: RC vs wait-for-GA vs temporary preview; update `global.json` accordingly (`allowPrerelease` if needed)
- [ ] Ensure local + CI agents can install the chosen SDK
- [ ] Review `.config/dotnet-tools.json` tools for .NET 11 SDK compatibility

### TFMs
- [ ] Root `Directory.Build.props` → `net11.0`
- [ ] Nested TFM overrides → `net11.0`: `samples/`, `benchmarks/`, `tests/test-apps/`, `source/timewarp-nuru-devcli/`
- [ ] `source/Directory.Build.props` `AnalyzerDependencyTfm` → `net11.0`
- [ ] Confirm no csproj still hard-codes `net10.0` as TargetFramework

### Hard-coded TFM paths / packaging (coupled to TFM bump)
- [ ] `source/timewarp-nuru-build/...` PackagePath `build/net10.0` → `build/net11.0`
- [ ] `source/timewarp-nuru-build/build/TimeWarp.Nuru.Build.targets` default `_NuruBuildTaskDir`
- [ ] `samples/Directory.Build.targets` and `source/timewarp-nuru-search/` `_NuruBuildTaskDir`
- [ ] `source/timewarp-nuru/timewarp-nuru.csproj` packed `build/net10.0/*` includes
- [ ] `source/timewarp-nuru-devcli/.../nupkg-layout-check.cs` required entry list
- [ ] DevCli / layout tests under `tests/timewarp-nuru-tests/devcli/` (fixtures + assertions)
- [ ] `benchmarks/.../program.cs` `targetFrameworkMoniker`

### Packages (Directory.Packages.props)
- [ ] Bump `Microsoft.Extensions.*` (16 packages currently `10.0.12`) to .NET 11 line
- [ ] Bump `Microsoft.Data.Sqlite.Core` (`10.0.12`)
- [ ] Bump `Microsoft.CodeAnalysis.NetAnalyzers` (`10.0.401`)
- [ ] Revisit **`Microsoft.Build.Utilities.Core`**: currently pinned at `18.9.6` because `18.10.1` drops `lib/net10.0` (net11-only). On net11, move to latest that supplies `net11.0` and still copies Framework DLLs into the build-task payload
- [ ] Revisit Roslyn pins (`Microsoft.CodeAnalysis.CSharp` etc.) vs SDK-bundled compiler; keep consumer minimum deliberate
- [ ] Serilog.Extensions.Logging / OpenTelemetry / MCP / benchmark CLI packages: bump only as needed for net11
- [ ] Aspire sample (`samples/aspire-otel`, `#:sdk Aspire.AppHost.Sdk@13.5.3`): confirm Aspire line supports .NET 11; bump SDK directive if required
- [ ] Upstream TimeWarp packages: confirm net11-ready versions exist; bump CPM entries

### CI / images
- [ ] `.github/workflows/workflow.yml` `dotnet-version: '10.0.x'` → `11.0.x` (or pinned RC/GA)
- [ ] No Dockerfiles today; if any appear before merge, align base images

### Docs / conventions / samples text
- [ ] `documentation/user/guides/deployment.md` path examples
- [ ] `documentation/developer/guides/aot-compilation.md` path examples
- [ ] `documentation/developer/standards/dotnet-conventions.md` still says `Target net9.0` — update to `net11.0`
- [ ] changelog entry for the TFM/SDK bump when releasing

### Verification
- [ ] `dotnet restore` / `dotnet build` clean under warnings-as-errors policies
- [ ] Full test + verify-samples + nupkg layout gate
- [ ] Spot-check analyzer load in a sample (Logging.Abstractions TFM under `analyzers/dotnet/cs`)
- [ ] Record final SDK version, package versions, and any deferred upstream blockers in Results

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
