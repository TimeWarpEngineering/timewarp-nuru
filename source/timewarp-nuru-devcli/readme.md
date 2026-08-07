# TimeWarp.Nuru.DevCli

Reusable dev-cli endpoints and services for TimeWarp repositories. This package provides source-only files that can be consumed by any TimeWarp repository.

## What's Included

### Endpoints

| Endpoint | Description | Dependencies |
|----------|-------------|--------------|
| `clean` | Clean solution and build artifacts | `IRepoCleanService` (TimeWarp.Amuru) |
| `self-install` | AOT compile dev CLI to ./bin | None (standalone) |
| `check-version` | Verify version is ready to release — three-state gate: none published (proceed), all published (abort), some published (resume with warning) | `NuGetVersionService`, `IRepoConfigService`, `IPackableProjectService` |

### Services

| Service | Description |
|---------|-------------|
| `IRepoConfigService` / `RepoConfigService` | Reads per-repo config from `.timewarp/dev.jsonc` |
| `IPackableProjectService` / `PackableProjectService` | Derives the packable project set (`IsPackable=true`, via real MSBuild evaluation) under a repo's `source/` tree — no hand-maintained project/package-ID lists |
| `TagAssertion` | Pure release-gate check: tag must equal `v` + `<Version>` from `source/Directory.Build.props` |
| `CheckVersionConfig` | Config model for the check-version command |
| `RepoConfig` | Top-level config model for `.timewarp/dev.jsonc` |
| `CiMode` (enum) / `CiModeDetector` | Pure CI mode detection (`pr`/`merge`/`release`); `workflow_dispatch` auto-detects `merge`; unknown explicit mode throws |
| `PublishState` (enum) / `PublishStateClassifier` | Pure none/some/all classification of published packages for the `check-version` gate; throws on zero packages or an out-of-range published count |

## Configuration

`check-version`'s package set is resolved with this precedence: `--package` (single ad-hoc
run) → `checkVersionConfig.packages` in `.timewarp/dev.jsonc` (explicit repo-level override) →
**derived** from every project under `source/**/*.csproj` with `IsPackable=true` (via
`dotnet msbuild -getProperty:IsPackable,PackageId`, no build/restore needed). Most repos need
no configuration at all — the derived set is correct as long as packable projects live under a
`source/` directory at the repo root, following the convention that project layout mirrors
package boundaries.

Create a `.timewarp/dev.jsonc` file in your repository root only to override the derived set:

```jsonc
{
  "checkVersionConfig": {
    // packages: comma-separated NuGet package IDs to check against NuGet.org.
    // Optional — omit to use the derived (IsPackable=true) set under source/.
    "packages": "TimeWarp.Nuru,TimeWarp.Nuru.Analyzers"
  }
}
```

If the file does not exist, `IRepoConfigService` returns defaults and the package set is fully
derived.

## Installation

Add the package to your project:

```bash
dotnet add package TimeWarp.Nuru.DevCli
```

The source files will be automatically included in your project's compilation via the `.props` file.

## Requirements

- TimeWarp.Nuru (the CLI framework)
- TimeWarp.Amuru 1.0.0-beta.22+ (for repo services)
- TimeWarp.Terminal (for ITerminal)

## Usage

The endpoints will be automatically discovered when you use `DiscoverEndpoints()`:

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(services =>
  {
    // Register required services
    services.AddSingleton<IRepoCleanService, RepoCleanService>();
    services.AddSingleton<NuGetVersionService>();
    services.AddSingleton<IRepoConfigService, RepoConfigService>();
    services.AddSingleton<IPackableProjectService, PackableProjectService>();
  })
  .UseMicrosoftDependencyInjection()
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);
```

## Migration Notes

### 3.0.0-beta.72+: package set derived from MSBuild `IsPackable` (`packable-project-service.cs`)

`check-version` (and this repo's own `pack`/`push` in `tools/dev-cli/endpoints/workflow-command.cs`)
used to require a hand-maintained package list — three separate hardcoded lists in some repos
(project paths for pack, package IDs for push, `checkVersionConfig.packages` for the release
gate) that had to be kept in sync by hand. Adding, renaming, or removing a packable project now
needs zero edits: `IPackableProjectService` derives the set from every `source/**/*.csproj` with
`IsPackable=true` (real MSBuild evaluation via `dotnet msbuild -getProperty:IsPackable,PackageId`,
not XML parsing — `IsPackable` is commonly a two-level props default and `PackageId` commonly
derives from `AssemblyName` with no explicit override).

`CheckVersionCommand.Handler` gains a fourth constructor parameter, `IPackableProjectService`;
update any direct construction site (`new CheckVersionCommand.Handler(...)`) to pass one — see
`tools/dev-cli/dev.cs` for the DI registration and `tools/dev-cli/endpoints/workflow-command.cs`
for the call site.

**To adopt:** delete `checkVersionConfig.packages` from `.timewarp/dev.jsonc` (it becomes an
optional override — no longer required) as long as your repo's packable projects live under
`source/` at the repo root.

### 3.0.0-beta.72+: partial-publish resume in `check-version` (`publish-state.cs`)

`check-version` used to be a two-state gate: `alreadyPublished.Count == 0` meant "safe to
release", anything else aborted. That meant a release run that failed partway through the push
loop (some packages published, some not) could never be resumed through the pipeline — the
retry aborted at `check-version` before the push loop's `--skip-duplicate` even got a chance to
make the re-push a no-op.

`check-version` now classifies the package set with the new `PublishStateClassifier` into three
states:

- **None** published — unchanged: "safe to release" (exit 0).
- **All** published — unchanged: abort with the existing "already released" message (exit 1).
- **Partial** (some but not all) — **new, behavior change**: exit 0 with a loud warning block
  listing already-published and missing packages, and a note that the run will resume the push.

Single-package repos are unaffected in shape: 1 of 1 published is still the **All** state and
still aborts.

Resuming a partial publish is only byte-safe when the resume run is the SAME commit that
produced the earlier partial push — this package's warning says so, but does not enforce it.
Enforcement is a release-gate tag-pin check in `tools/dev-cli/endpoints/workflow-command.cs`
(this repo's own dev-cli, not shipped package content): if a local tag `v<version>` already
exists, `HEAD` must be at that tag's commit, or the release aborts with a mismatch error.
**Known residual (accepted):** an untagged double break-glass — two different commits under one
never-bumped, never-tagged version — leaves tag-pin nothing to pin against; full closure arrives
once releases are always cut by tooling that tags first (458-006) or by build-once/promote
(458-002), and until then this narrow case relies on operator discipline.

### 3.0.0-beta.72+: shared `CiMode` / `CiModeDetector` (`ci-mode.cs`)

`workflow-command.cs` previously declared its own internal `CiMode` enum; `ci-mode.cs` now ships
that type in package content. When updating the package, delete any local `CiMode` enum from your
repo's `workflow-command.cs` and use the shared `CiModeDetector` — otherwise the build fails with
`CS0101` (duplicate `'CiMode'` in namespace `DevCli`).

Also note: `workflow_dispatch` now auto-detects `merge` mode (never publishes); break-glass release
requires explicit `--mode release`.

### 3.0.0-beta.72+: git-tag strategy removed

`check-version` now has exactly one methodology: props-version membership in the published NuGet
versions. The `git-tag` strategy is gone — `GitTagCheckService`, the `CheckVersionStrategy` enum,
and the `--strategy`/`--tag` options on `check-version` no longer exist. The release gate instead
asserts (in `dev workflow --mode release`, on `GITHUB_EVENT_NAME == "release"`) that the triggering
tag equals `v` + `<Version>` from `source/Directory.Build.props`, via the new pure `TagAssertion`
service; it also asserts the released commit is an ancestor of master in every release mode
(release event, break-glass, local).

A lingering `checkVersionStrategy` key in `.timewarp/dev.jsonc` is silently ignored on deserialize
(unknown-key tolerance) rather than erroring — remove the key; it does nothing. Repos that copy
this config shape (e.g. timewarp-architecture-style configs) should drop it too.

**Resurrect condition:** tags-as-a-release-ledger only comes back for a repo that ships versioned
releases through a channel other than NuGet (no such repo today). If it does, re-add it deliberately
with membership-across-all-tags semantics (search all tags for the version, like the old
nuget-search membership check) — never compare against `GITHUB_REF_NAME`, which names the tag being
released, not a released tag to search against.

The package also now ships the new public `TagAssertion` / `TagAssertionResult` types (namespace
`DevCli`) — no known downstream equivalent, so no expected collision; listed here for completeness.

## Source-Only Package

This is a **source-only NuGet package**. The endpoint and service files are included in your project's compilation, not as a compiled assembly. This is required for Nuru's source generators to work correctly.

### Why Source-Only?

Nuru uses source generators to create route matching code at compile time. The generator needs to see the endpoint class definitions in your project's source. Traditional NuGet packages with compiled DLLs would hide the source from the generator.

### Creating Your Own Source-Only Endpoint Packages

This package serves as a reference for creating your own reusable endpoint packages:

1. Create a project with `IncludeBuildOutput=false`
2. Place endpoint files in `content/any/endpoints/`
3. Place service files in `content/any/services/`
4. Create a `.props` file in `build/` to include the content files
5. Pack the project as a NuGet package

Example `.props` file:

```xml
<Project>
  <ItemGroup>
    <Compile Include="$(MSBuildThisFileDirectory)../content/any/endpoints/*.cs" 
             Visible="false" />
    <Compile Include="$(MSBuildThisFileDirectory)../content/any/services/*.cs" 
             Visible="false" />
  </ItemGroup>
</Project>
```
