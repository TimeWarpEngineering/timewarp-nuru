# TimeWarp.Nuru.DevCli

Reusable dev-cli endpoints and services for TimeWarp repositories. This package provides source-only files that can be consumed by any TimeWarp repository.

## What's Included

### Endpoints

| Endpoint | Description | Dependencies |
|----------|-------------|--------------|
| `clean` | Clean solution and build artifacts | `IRepoCleanService` (TimeWarp.Amuru) |
| `self-install` | AOT compile dev CLI to ./bin | None (standalone) |
| `check-version` | Verify version is ready to release — three-state publish gate, plus an honest prerelease-increment distance (warns when more than one ahead; `--strict` exits non-zero) | `NuGetVersionService`, `IRepoConfigService`, `IPackableProjectService` |
| `release` | Cut a release: create tag `v{Version}` (from `source/Directory.Build.props`) and the GitHub Release, gated by working-tree/branch/sync/tag-availability/publish-state/CI-run/target-attestation guards; `--dry-run` runs every guard and previews the exact commands without creating anything | `NuGetVersionService`, `IRepoConfigService`, `IPackableProjectService` |
| `audit [--force] [--verbose,-v]` | Run this repo's fast pre-test checks, one line each with wall time, then a passed/failed summary; exits 1 when any check fails. With no checks it prints that there is nothing to run and exits 0. With a pass cache, skips the run when these inputs already passed (`--force` runs anyway). `ganda repo audit` runs it. See [Repo audit checks](dev-audit.md) | None (checks come from `DevAuditChecks.AddRepoChecks`, the optional cache from `DevAuditChecks.ConfigurePassCache`) |
| `benchmark list` | Print the repo's named benchmarks, one per line (`--json` for an array). Empty means the repo is not in a perf sweep. See [Benchmark contract](#benchmark-contract-dev-benchmark) | None (benchmarks come from `DevBenchmarks.AddRepoBenchmarks`) |
| `benchmark run <name> --base <ref>` | Build and run the named benchmark at the base ref and at the working tree in the same run, then print one JSON verdict (`win`, `noise`, or `regression`) on stdout | None |

### Services

| Service | Description |
|---------|-------------|
| `IRepoConfigService` / `RepoConfigService` | Reads per-repo config from `.timewarp/dev.jsonc` |
| `IPackableProjectService` / `PackableProjectService` | Derives the packable project set (`IsPackable=true`, via real MSBuild evaluation) under a repo's `source/` tree — no hand-maintained project/package-ID lists |
| `TagAssertion` | Pure release-gate check: tag must equal `v` + `<Version>` from `source/Directory.Build.props` |
| `CheckVersionConfig` | Config model for the check-version command (`packages`, `feeds`) |
| `PackageFeedsResolver` (`PackageFeeds`, `PackageFeedsResolution`) | Pure resolver for `checkVersionConfig.feeds`: absent → `nuget.org`; `nuget.org` / `github-packages`, comma-separated, case-insensitive → the flags; an unknown name or a value naming no feed → an error. `check-version` and `dev release` both use it |
| `RepoConfig` | Top-level config model for `.timewarp/dev.jsonc` |
| `CiMode` (enum) / `CiModeDetector` | Pure CI mode detection (`pr`/`merge`/`release`); `workflow_dispatch` auto-detects `merge`; unknown explicit mode throws |
| `NuGetVersionService` | Reads a package's versions from the feeds the repo declares (`LookupAsync(packageId, feeds, ct)`): nuget.org, the org GitHub Packages feed, or both. Both URLs are constants. An undeclared feed is never contacted, and without `github-packages` no GitHub token is resolved |
| `PackageFeedLookup` / `PackagePublishVerdict` / `PackageFeedUnreadableException` | One package's versions on the declared feeds. `Check(version)` is Published (on any declared feed), NotPublished, or Unknown (on no declared feed while the declared GitHub Packages feed is unreadable). `FormatUnknownRefusal` is the shared refusal text |
| `GitHubPackagesCredential` | The GitHub Packages token: `GITHUB_TOKEN`, then `GH_TOKEN`, then `gh auth token` |
| `CheckVersionOutcome` (enum) / `CheckVersionAbort` | How a check-version run ended (`CheckVersionCommand.Handler.Outcome`), and the pipeline abort line for it |
| `PublishState` (enum) / `PublishStateClassifier` | Pure none/some/all classification of published packages for the `check-version` gate; throws on zero packages or an out-of-range published count |
| `PrereleaseDistance` | Pure prerelease-increment distance between source and the latest published version. Defined only when major.minor.patch and the prerelease label match and only the numeric identifier differs |
| `CiRunPromotion` | Pure logic for release-mode artifact promotion: orders candidate CI runs for a commit, selects a run's `Packages-*` artifact (including expiry handling), and verifies a downloaded `.nupkg` set against the derived packable set — no process execution |
| `ReleaseGuard` (`GuardVerdict`) | Pure per-guard classifiers for the `release` gate: working tree clean, on master, synced with origin, tag `v{Version}` available (neither local nor remote), and publish state (none/partial/all) — no process execution |
| `AttestationVerifier` (`AttestationNoteDto`, `AttestationEvaluation`, `AttestationVerificationStatus`) | Pure verifier for ganda-audit attestation notes (kanban task 458-010): parses the frozen v1 note JSON, rebuilds the canonical signed payload, decodes the unpadded-base64url signature, resolves `key_id` against a baked-in key registry (or a test-only `keyOverride`), and compares the note's tree against the tree being verified — no process execution; the actual Ed25519 verify (via `openssl pkeyutl -verify -rawin`) runs in `AttestationVerifyRunner`, not here |
| `AttestationVerifyRunner` (`AttestationVerifyOutcome`) | The process half of the attestation verify (kanban task 592): fetches `refs/notes/ganda-audit`, reads the note for a commit's tree, runs `AttestationVerifier.Evaluate` and the openssl signature check. `dev workflow` verifies `HEAD`; `dev release` verifies the release target |
| `ReleaseAttestationGuard` (`ReleaseAttestationGuardVerdict`) | Pure verdict for the `dev release` target-attestation guard: release-mode `attestation.mode` (unset or typo → require), then pass / warn / refuse / skipped with the refusal block and remedy — no process execution |
| `AttestationConfig` | Config model for the attestation verify step's `mode` (`"off"`\|`"warn"`\|`"require"`; blank uses a context-sensitive default) |
| `AttestationConfigResolver` (`AttestationMode`, `AttestationModeResolution`) | Pure resolver for `attestation.mode`: blank/absent → `(null, null)`; `"off"`/`"warn"`/`"require"` (case-insensitive) → enum; unrecognized non-blank → `(null, trimmed)` so callers can warn about a typo. `EffectiveMode(resolution, whenUnset)` applies context defaults (`DefaultPrMode` = Warn, `DefaultReleaseMode` = Require). Typos never silently become Off |
| `PullRequestBuildClassifier` (`PullRequestBuildDecision`) | The fleet pull request build decision (kanban task 595). `DecideAsync(repoRoot, ct)` reads the GitHub event, runs `git diff --name-only base...head`, applies the non-build rule plus the repo's `pullRequestBuild` additions, and returns build or skip with the line to print. See [Pull request build decision](#pull-request-build-decision) |
| `PullRequestBuildConfig` | Config model for `pullRequestBuild` in `.timewarp/dev.jsonc`: `buildPaths` and `nonBuildPaths` |
| `IDevAuditCheck` / `DevAuditCheckResult` | One repo-specific `dev audit` check: `Name`, `RunAsync(DevAuditContext, CancellationToken)`, and `Pass(message?)` or `Fail(message)` |
| `DevAuditContext` | What one `dev audit` run hands each check and the pass cache: `RepoRoot`, `Force` (`--force`), `Verbose` (`--verbose`, `-v`) |
| `DevAuditChecks` | The `dev audit` registry. `All()` returns the checks the repo adds in its `AddRepoChecks` partial method; none by default. `Named(...)` / `Select(checks, names)` pick checks by name (an unknown name throws). `PassCache()` returns the cache the repo sets in `ConfigurePassCache`; none by default |
| `IDevAuditPassCache` / `DevAuditTreeStamp` | The optional `dev audit` pass cache: `KeyAsync` (null: cannot name these inputs, run everything), `HasPass`, `RecordPass`. `DevAuditTreeStamp.Create(ignoredEntries?)` keys the committed tree (without `kanban/` by default) plus the SDK version, and stores the last passing key in `artifacts/dev-audit/passed.key` |
| `DevAuditRunner` (`DevAuditSummary`) | Runs the checks in order with the context, prints the per-check lines and the summary, and returns the passed/failed counts, `Skipped`, and exit code. With a pass cache it skips a run whose inputs already passed and records a pass only when every check passed |
| `IDevBenchmark` / `IDevBenchmarkSide` / `DevBenchmarkTree` | One named `dev benchmark`: `PrepareAsync(tree)` builds it from a tree and returns a side whose `MeasureAsync` takes one sample (a cost; lower is better) |
| `DevBenchmarks` | The `dev benchmark` registry. `All()` returns the benchmarks the repo adds in its `AddRepoBenchmarks` partial method; none by default |
| `DevBenchmarkRunner` (`DevBenchmarkRunOptions`) | Prepares base and head, discards warmup rounds, takes interleaved samples (order alternates per round), and returns the comparison |
| `BenchmarkComparison` (`BenchmarkVerdict`, `BenchmarkSideStatistics`) | Pure statistics: medians, MADs, the head/base ratio, the noise band, and the `win` / `noise` / `regression` verdict |
| `DevBenchmarkResult` (`DevBenchmarkRevision`) | The `dev-benchmark/v1` JSON result and the stderr summary line |
| `BenchmarkBaseCheckout` | Resolves `--base`, checks it out into a temporary detached worktree (hooks off), reads the head commit and dirty flag, and removes the worktree afterwards |
| `ProcessInvocationBenchmark` | Opt-in ready-made benchmark (`content/any/benchmarks/`): publish a project (AOT by default) from each tree and time one process invocation per sample, in ms |
| `ReportedMetricBenchmark` | Opt-in ready-made benchmark (`content/any/benchmarks/`): build a project or runfile from each tree, run it once per sample, and read the sample from a `key=value` stdout line |

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
    // packages: comma-separated NuGet package IDs to check.
    // Optional — omit to use the derived (IsPackable=true) set under source/.
    "packages": "TimeWarp.Nuru,TimeWarp.Nuru.Analyzers",
    // feeds: nuget.org | github-packages, comma-separated. Default: nuget.org.
    "feeds": "nuget.org"
  }
}
```

If the file does not exist, `IRepoConfigService` returns defaults and the package set is fully
derived.

### Feeds (`checkVersionConfig.feeds`)

`feeds` names the feeds the repo publishes to. `check-version` and `dev release` read only
those feeds to decide whether a version is already published. Both commands resolve the key the
same way, and there is no separate switch.

| Value | Feeds read |
|-------|------------|
| absent | nuget.org (the default) |
| `"nuget.org"` | nuget.org |
| `"github-packages"` | the org GitHub Packages feed (`https://nuget.pkg.github.com/TimeWarpEngineering/index.json`) |
| `"nuget.org,github-packages"` | both; a version on either is published |

The grammar matches `packages`: comma-separated, entries trimmed, empty entries ignored. Names
are case-insensitive. An unknown name, or a value that names no feed (`""`, `","`), is a
configuration error. Both commands report it and exit 1 before any network call.

A repo that does not publish to GitHub Packages never needs `packages: read` for check-version.
Without `github-packages`, DevCli never contacts GitHub Packages and never looks for a token. A
repo that publishes there declares it, and then its token needs `read:packages` (in GitHub
Actions, `packages: read` on the job). See the 4.0.0-beta.8 migration note below for the
refusal when that feed cannot be read.

## Prerelease distance (`check-version`)

After the source version and the latest published NuGet version, `check-version` states the
distance when that distance is a single prerelease number:

```text
Source is 5 prerelease increments ahead of v2.0.0-beta.9
```

The pair has to share major, minor, and patch (NuGet core normalization applies, so `2.0` and
`2.0.0` match) and the same prerelease label, with only the numeric identifier different
(`beta.9` → `beta.14` is 5). A major, minor, or patch change, a different label, a prerelease
that is not `{label}.{number}`, or no published version prints both versions and omits the
distance line. The command does not invent a metric for those shapes.

A distance of 1 is a normal bump: the distance line prints, and there is no warning. A distance
of 0 is the existing already-released failure (exit 1); the distance line is the only addition.
A distance greater than 1 means intermediate numbers were bumped and never published. The warning
counts those intermediates (distance minus the source version, which is the one being released)
and the process exits 0, because a deliberate jump is legitimate:

```text
4 version(s) were bumped but never released — was a release step skipped?
```

Pass `--strict` to exit non-zero when that warning is present, for a CI job that should fail a
skipped release step. `--strict` does not change the distance-0 already-released failure, and it
does not fail a one-increment bump. The "safe to release" line means this exact source
version is absent from NuGet; `--strict` fails the gap, not that membership check.

`PrereleaseDistance` (namespace `DevCli`) is the pure function behind the line. A consuming repo
that already declares that type name fails to compile with `CS0101`.

## Pull request build decision

The fleet standard for pull request CI (decided in timewarp-ganda tasks 476, 479, 565, 587):

- One workflow, `.github/workflows/workflow.yml`. Its `pull_request` trigger has **no `paths`
  or `paths-ignore` filter**, so every pull request reports the `CI` check and a docs-only pull
  request is never refused at merge for a missing check. The `push` trigger may keep a filter.
- The decision "does this pull request need build and test?" is made in `dev workflow` (C#), not
  in YAML, by `PullRequestBuildClassifier` in this package. A rule change ships in a DevCli
  version, and the pin-currency audit carries it to every repo.

### The one call

A repo's `workflow` command makes this call before it builds, in pull request mode:

```csharp
PullRequestBuildDecision decision = await PullRequestBuildClassifier.DecideAsync(repoRoot, ct);
Terminal.WriteLine(decision.Message);
if (!decision.RequiresBuild)
{
  return Unit.Value; // skip build and test; the run still reports success
}
```

`repoRoot` is the repository root (the directory that holds `.timewarp/dev.jsonc`). The call:

1. Reads `GITHUB_EVENT_NAME`. Anything other than `pull_request` builds, including `push`,
   `release`, `workflow_dispatch`, and a local run with no event name.
2. Reads `pull_request.base.sha` and `pull_request.head.sha` from the file at
   `GITHUB_EVENT_PATH`.
3. Runs `git diff --name-only base...head` in `repoRoot`. The checkout needs the history between
   the two commits (`fetch-depth: 0`); a shallow checkout fails the diff, and a failed diff builds.
4. Reads `pullRequestBuild` from `.timewarp/dev.jsonc` and applies the rule below.

An unreadable event, a failed diff, and an unreadable or invalid `dev.jsonc` build. Only a
definite all-non-build diff skips.

### The rule

A pull request skips build and test only when **every** changed path is a non-build path.
It is ganda's `WorkflowChangeFilter.RequiresBuild`, ported exactly:

| Path | Result |
|------|--------|
| `.github/workflows/**` | build |
| `source/**`, `tests/**`, `tools/**`, `msbuild/**` (markdown too) | build, always |
| `kanban/**`, `documentation/**`, `assets/**`, `notes/**`, the rest of `.github/**` | skip |
| a file named `.editorconfig` anywhere else (warnings are errors) | build |
| a file named `.gitignore` | skip |
| any other `*.md` | skip |
| anything else | build |
| an empty diff | skip |

### Repo additions (`pullRequestBuild`)

A repo extends the lists in `.timewarp/dev.jsonc`. It does not fork the rule:

```jsonc
{
  "pullRequestBuild": {
    // Always build, even when a default non-build path covers them
    // (here: files packed into the nupkgs).
    "buildPaths": ["readme.md", "assets/logo.png"],
    // Extra non-build trees and files.
    "nonBuildPaths": ["skills/**", ".agent/**"]
  }
}
```

An entry is a repo-relative path. `dir`, `dir/`, and `dir/**` mean the directory tree; anything
else is one exact path (`readme.md` is the root readme only). No other wildcards: an entry with
`*` or `?` anywhere else is invalid, and an invalid entry makes the decision build with a message
naming it. `buildPaths` win over every non-build list. `nonBuildPaths` cannot make `source/`,
`tests/`, `tools/`, `msbuild/`, `.github/workflows/`, or `.editorconfig` non-build; the rule
checks those first.

The additions live in `dev.jsonc`, not in a call argument, so the call is the same line in
every repo (ganda's audit looks for it) and the additions are data that a person or an audit
reads without parsing C#.

### Split CI jobs

A workflow that runs build and test in several jobs runs the decision once, in a first job, and
gates the others on its output. timewarp-nuru does this: `dev workflow --stage classify` makes
the one call and writes `code=true|false` to `GITHUB_OUTPUT`; the stage jobs run only when
`code != 'false'`, and the required `ci` job is skipped (which reports success) on `false`.

## Repo audit checks (`dev audit`)

DevCli owns the `audit` route; a repo adds `IDevAuditCheck`s, an optional pass cache, and
in-process subset runs. See [dev-audit.md](dev-audit.md).

## Benchmark contract (`dev benchmark`)

Every TimeWarp repo answers the same perf-sweep questions the same way:

| Command | Answer |
|---------|--------|
| `dev benchmark list` | The named benchmarks this repo offers. Nothing printed (exit 0) means the repo does not take part in a perf sweep |
| `dev benchmark run <name> --base <git-ref>` | Is the working tree faster or slower than `<git-ref>` on this benchmark? One JSON object on stdout |

DevCli owns both routes and the plumbing: checking out the base, building and running both
sides, the ratio, the noise band and the verdict. A repo only names its benchmarks.

### What `run` does

1. Resolves `--base` with `git rev-parse --verify <ref>^{commit}`. It does not fetch, so run
   `git fetch` first for `origin/master`. A branch, tag or SHA works.
2. Checks the base out into a temporary detached `git worktree` under the system temp
   directory, with repo hooks off. Head is the current working tree, uncommitted changes
   included (`"dirty": true` in the result).
3. Calls the benchmark's `PrepareAsync` for base, then for head. Build output goes to
   `artifacts/benchmarks/<name>/base` and `/head`. The benchmark definition is the head's
   code, applied to both trees, so it must build from any tree that has the paths it names.
4. Takes `--warmup` rounds (default 3, discarded), then `--samples` rounds (default 30). Each
   round measures base and head once; even rounds measure base first, odd rounds head first,
   so drift on the machine hits both sides equally.
5. Compares the two sides and removes the temporary worktree.

Both sides run in one process on one machine, and only the ratio is compared. There is no
stored absolute number: shared CI runners are too noisy for that.

### Ratio, noise band, verdict

A sample is a cost (a wall time, nanoseconds per call); lower is better.

```
spread(side) = 1.4826 * MAD / median        robust relative spread
ratio        = median(head) / median(base)  below 1 is faster
noiseBand    = max(noiseFloor, 3 * 1.2533 * sqrt(spread(base)^2 / n(base) + spread(head)^2 / n(head)))

ratio < 1 - noiseBand  -> win
ratio > 1 + noiseBand  -> regression
otherwise              -> noise
```

The band is three standard errors of the relative difference between the medians.
`--noise-floor` (default `0.02`, 2%) keeps two very quiet sides from calling a tiny change a
win. Medians and MADs, not means, so one descheduled sample does not move the answer.

### Output

stdout carries only the result (schema `dev-benchmark/v1`); progress and one summary line go
to stderr:

```json
{
  "schema": "dev-benchmark/v1",
  "benchmark": "aot-startup",
  "unit": "ms",
  "base": { "ref": "origin/master", "commit": "<sha>", "dirty": false, "median": 2.713, "mad": 0.1517, "samples": 30 },
  "head": { "ref": "HEAD", "commit": "<sha>", "dirty": true, "median": 2.8074, "mad": 0.0828, "samples": 30 },
  "ratio": 1.0348,
  "noiseBand": 0.0788,
  "verdict": "noise"
}
```

Exit 0 means a verdict was produced, whatever it is; read `verdict`. Exit 1 means there is no
verdict (unknown name, no `--base`, a ref that names no commit, a failed build, a failing
sample) and nothing is written to stdout.

### What a repo implements

Implement `IDevBenchmark` (or use a ready-made one) in your own `tools/dev-cli`, and register
it by implementing the `AddRepoBenchmarks` partial method once.

The two ready-made benchmarks are opt-in. The package's props do not compile
`content/any/benchmarks/`: each is an internal class that only your `AddRepoBenchmarks`
constructs, so a repo that compiled one without constructing it would fail CA1812. Compile
the ones you use:

```xml
<ItemGroup>
  <Compile Include="$(NuruDevCliContentDirectory)benchmarks/process-invocation-benchmark.cs" />
  <Compile Include="$(NuruDevCliContentDirectory)benchmarks/reported-metric-benchmark.cs" />
</ItemGroup>
```

`NuruDevCliContentDirectory` is the package's `content/any/` folder, set by its props.
Then register them:

```csharp
namespace DevCli;

internal static partial class DevBenchmarks
{
  static partial void AddRepoBenchmarks(List<IDevBenchmark> benchmarks)
  {
    // Cold start: publish (AOT) from each tree, time one invocation per sample.
    benchmarks.Add(new ProcessInvocationBenchmark(
      name: "aot-startup",
      description: "Cold start of an AOT-published sample app",
      projectPath: "benchmarks/startup-app/startup-app.csproj",
      executableName: "startup-app",
      arguments: ["--version"]));

    // In-process metric: build from each tree, run once per sample, read `key=value` from stdout.
    benchmarks.Add(new ReportedMetricBenchmark(
      name: "dispatch",
      description: "Nanoseconds per dispatched call",
      unit: "ns",
      path: "benchmarks/dispatch/dispatch.cs",
      assemblyName: "dispatch",
      metricKey: "per-call-ns"));
  }
}
```

A custom benchmark implements `Name`, `Description`, `Unit`, and
`PrepareAsync(DevBenchmarkTree tree, CancellationToken)`. `PrepareAsync` builds from
`tree.Root` into `tree.OutputDirectory` and returns an `IDevBenchmarkSide` whose
`MeasureAsync` returns one sample. Throw `InvalidOperationException` with a full message when
the tree cannot be built or a sample fails. Keep one sample short (milliseconds to a second or
two): the run takes `2 * (warmup + samples)` of them.

The registry is a partial method for the same reasons as `DevAuditChecks` (see
[Repo audit checks](dev-audit.md)): source-only, AOT-safe, no reflection or DI.
A repo without benchmarks writes nothing.

Bench runs stay off the pull request path. They are for a perf sweep or a manual check.

## Installation

Add the package to your project:

```bash
dotnet add package TimeWarp.Nuru.DevCli
```

The source files will be automatically included in your project's compilation via the `.props` file.

## Requirements

- TimeWarp.Nuru (the CLI framework)
- TimeWarp.Amuru and TimeWarp.Amuru.Tools 3.0.0-beta.9+. The content calls the typed `Gh.*`
  and `DotNet.*` builders and `Git` from TimeWarp.Amuru.Tools, so the consumer references both
  packages (`<PackageReference Include="TimeWarp.Amuru" />` and
  `<PackageReference Include="TimeWarp.Amuru.Tools" />`)
- TimeWarp.Terminal (for ITerminal)

## Usage

The endpoints will be automatically discovered when you use `DiscoverEndpoints()`:

```csharp
NuruApp app = NuruApp.CreateBuilder()
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

### 4.0.0-beta.17+: shared `dev benchmark` contract (kanban task 578)

DevCli now owns `benchmark list` and `benchmark run`. A dev CLI with its own
`[NuruRoute("benchmark")]` endpoint keeps compiling (the routes differ), but a perf sweep only
reads the shared contract: register named benchmarks in `DevBenchmarks.AddRepoBenchmarks`
(see [Benchmark contract](#benchmark-contract-dev-benchmark)) and retire stored absolute
baselines.

### 4.0.0-beta.18+: `dev audit` options, pass cache and subset run (kanban task 601)

`IDevAuditCheck.RunAsync` takes a `DevAuditContext` first:
`RunAsync(DevAuditContext context, CancellationToken cancellationToken)`. Add the parameter to
each check; read `context.RepoRoot`, `context.Verbose` and `context.Force` where a check needs
them. `DevAuditRunner.RunAsync` takes the context and a pass cache:
`RunAsync(checks, context, terminal, passCache, ct)`.

A dev CLI with its own `audit` command for `--force`, `--verbose` or a pass stamp can drop it:
the shared route has both options, `ConfigurePassCache` with `DevAuditTreeStamp.Create()`
replaces a tree stamp like ganda's `DevAuditStamp` (same key rule and file path), and
`DevAuditChecks.Named(...)` replaces a scoped in-process run (see
[Repo audit checks](dev-audit.md)).

### 4.0.0-beta.17+: shared `dev audit` endpoint (`audit-command.cs`, kanban task 600)

DevCli now owns the `audit` route. A dev CLI that defines its own `[NuruRoute("audit")]`
endpoint gets a route clash after this upgrade. Delete that endpoint, move each check it ran
into an `IDevAuditCheck`, and add them in `DevAuditChecks.AddRepoChecks` (see
[Repo audit checks](dev-audit.md)).

### 4.0.0-beta.16+: typed Amuru builders (kanban task 598)

TimeWarp.Amuru 3.0.0-beta.9 adds AMURU0001, a compile error for a raw `Shell.Builder("gh")` or
`Shell.Builder("dotnet")` call that has a typed builder. The content now calls `Gh.Auth()`,
`Gh.Run().List()`, `Gh.Api()` and `Gh.Release().Create()`, so a consumer dev CLI builds against
Amuru beta.9. `ReleaseGuard.BuildReleaseCreateArguments` is replaced by
`ReleaseGuard.BuildReleaseCreate(tag, version)`, which returns the `GhReleaseCreateBuilder`; its
`ToArguments()` gives the printed command (`--prerelease` now comes before `--verify-tag`).
The packable-project scan keeps `dotnet msbuild … -nodeReuse:false` on `DotNet.Builder()`
until `DotNet.MsBuild()` has a node-reuse option (timewarp-amuru task 154).

Bump `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` to 3.0.0-beta.9 or later with this release.

### 4.0.0-beta.15+: shared pull request build decision (`pull-request-build-classifier.cs`, kanban task 595)

`PullRequestBuildClassifier` replaces the hand copies of the non-build path rule (ganda's
`WorkflowChangeFilter`, the factory template's `NonBuildPaths`, and per-repo copies).

**To adopt:**

- Delete the repo's own copy of the rule and call
  `PullRequestBuildClassifier.DecideAsync(repoRoot, ct)` from the `workflow` command (see
  [The one call](#the-one-call)). A local type named `PullRequestBuildClassifier`,
  `PullRequestBuildDecision`, or `PullRequestBuildConfig` in namespace `DevCli` fails the build
  with `CS0101`.
- Remove `paths` / `paths-ignore` from the `pull_request` trigger in `workflow.yml`.
- Move repo-specific additions to `pullRequestBuild` in `.timewarp/dev.jsonc`.

### Next release after 4.0.0-beta.10: publish gates read only the declared feeds (kanban task 564)

4.0.0-beta.8 through 4.0.0-beta.10 read nuget.org and GitHub Packages for every repo, so a
nuget.org-only repo needed a GitHub token with `read:packages` to release. The gates now read the
feeds named in `checkVersionConfig.feeds`, and the default is `nuget.org`.

**To adopt:**

- A repo that publishes to GitHub Packages adds `"feeds": "nuget.org,github-packages"` (or
  `"github-packages"` when nothing goes to nuget.org) under `checkVersionConfig`. Without it the
  gates stop seeing its GitHub Packages versions.
- A nuget.org-only repo needs no change, and can drop `packages: read` from jobs that held it
  only for the gate.
- A forked caller of `NuGetVersionService.LookupAsync(packageId, ct)` passes the resolved feed
  set: `LookupAsync(packageId, PackageFeedsResolver.Resolve(config.CheckVersionConfig?.Feeds).Feeds, ct)`,
  after reporting a non-null `Error`. `PackageFeedLookup`'s constructor takes the feed set as its
  second argument.

### 4.0.0-beta.10+: every DevCli type is `internal` (kanban task 560)

Every type in the content, top-level and nested, is `internal`. The files compile into your
`tools/dev-cli` assembly, so your code still uses them, and the package no longer raises CA1034
(nested public `Handler`) or CA2000 (`NuGetVersionService`) in your build.

- A public type of yours that exposes a DevCli type in a public signature now fails with CS0051
  or CS0053. The usual case is a public `WorkflowCommand.Handler` whose constructor takes
  `NuGetVersionService`, `IRepoConfigService` or `IPackableProjectService`. Make that type
  `internal`.
- Take TimeWarp.Nuru at the same version. Its source-generated DI now constructs internal
  services declared in your assembly; an older Nuru reports NURU054 for the DevCli services.
- Once you take this version, drop `CA1034` and `CA2000` from `tools/dev-cli` `NoWarn` if they
  were there only for package content.

### 4.0.0-beta.8+: publish gates read nuget.org and GitHub Packages (`package-feed-lookup.cs`)

`check-version` and `dev release` used to ask only nuget.org. A package on the org GitHub
Packages feed (`https://nuget.pkg.github.com/TimeWarpEngineering/index.json`) always looked
unpublished, so the gates let a version be released twice. Both gates now read both feeds and
count a version on either as published. (Superseded by the next release after
4.0.0-beta.10: the gates read only the feeds in `checkVersionConfig.feeds`, default nuget.org.)

GitHub Packages needs a token with `read:packages` (`GITHUB_TOKEN`, then `GH_TOKEN`, then
`gh auth token`). When a version is not on nuget.org and GitHub Packages cannot be read (no
token, 401, 403, unreachable), the gates refuse with a message that names the feed, the HTTP
status and the fix. A 404 from GitHub Packages means not found, so nuget.org-only repos behave as
before when the token works.

**To adopt:**

- Add `packages: read` to every workflow job that runs `check-version`, `dev release` or a
  `dev workflow` release pipeline. A job with explicit `permissions` gets no packages access
  otherwise, and the gate refuses.
- A forked `workflow` command that aborts with "version already released" on any non-zero
  check-version exit should call
  `CheckVersionAbort.FormatAbortReason(handler.Outcome, handler.GitHubPackagesFailure)` instead.
- Test runfiles that compile the DevCli services file by file must add
  `package-feed-lookup.cs`, `github-packages-credential.cs` and `check-version-outcome.cs`.

New types in namespace `DevCli` (internal since 4.0.0-beta.10): `PackageFeedLookup`, `PackagePublishVerdict`,
`PackageFeedUnreadableException`, `GitHubPackagesCredential`, `CheckVersionOutcome`,
`CheckVersionAbort`, `NuGetServiceIndex`, `NuGetServiceIndexResource`.

### 3.0.0-beta.72+: attestation verify step (`attestation-verifier.cs`, `attestation-config.cs`)

`dev workflow` includes an attestation verify step (kanban tasks 458-010, 458-011) — the
DevCli/public half of "sign locally in ganda, verify in CI." Ganda (private, operator-only)
audits a repo and signs evidence over `(tree sha, check-set hash, timestamp)` into
`refs/notes/ganda-audit`; this package's `AttestationVerifier` rebuilds that evidence and
`workflow-command.cs` shells out to `openssl pkeyutl -verify -rawin` to check the Ed25519
signature — no BCL Ed25519 verify exists, and a crypto NuGet dependency was rejected for this
source-only package's posture, so `openssl` must be on PATH (runners and operator machines) or
the step reports `VerifierUnavailable`.

#### `attestation.mode` policy (458-011)

One key for all pipeline modes. Precedence: CLI `--attestation off|warn|require` >
`.timewarp/dev.jsonc` `attestation.mode` > context default.

| `attestation.mode` | PR/merge | Release |
|--------------------|----------|---------|
| unset / blank | warn (advisory) | **require** (hard fail) |
| `off` | skip verify | skip verify |
| `warn` | advisory, do not fail | advisory, do not fail |
| `require` | hard fail if invalid | hard fail if invalid |

Unset release → `require` is the TimeWarp-first default: repos that never set the key keep a
safe release gate. Explicit `"warn"` is advisory on release too (blank ≠ explicit warn).
`"off"` skips verify on both paths (portable/no-ganda consumers). An unrecognized non-blank
value (e.g. `"requiree"`) never silently becomes `off`: PR fail-open treats it as `warn`,
release fail-closed treats it as `require`, and both print
`Warning: unrecognized attestation.mode '<value>' — treating as '<default>'. Valid values:
off, warn, require.`

TimeWarp repos that want **require on both PR and release** should set `"mode": "require"`
explicitly; blank is require-only on release.

#### Pipeline shape by mode

- **PR/merge mode**: Step 1 ("Attestation"), before Clean (`1/5..5/5`). Shared policy helper
  applies the table above (`DefaultPrMode` = Warn). In `warn`, the advisory repeats once more
  immediately before the `SUCCEEDED` banner so it survives scrollback. In `off`, one line
  `Attestation skipped (mode=off).` and no verify.
- **Release mode**: same policy helper in the Step 1/6 gate block after the ancestor-of-master
  check (`DefaultReleaseMode` = Require). **Honors** `attestation.mode` (no longer an
  unconditional hard gate). The runner never signs; when mode is `require` (including the
  unset default), a missing or invalid attestation aborts with "pull master locally so ganda
  can attest."

CLI break-glass without editing jsonc:

```bash
dev workflow --mode release --attestation off
dev workflow --attestation require
```

Outcomes and their operator-facing messages: `Valid` (check_set + ts printed); `NoNote` /
`RefMissing` ("tree `<short>` is unattested — pull master locally so ganda can attest (`ganda repo
attest`)"); `UnknownKey` ("update TimeWarp.Nuru.DevCli" — the key registry needs a bump);
`VerifierUnavailable` ("openssl not found — install openssl"); `TreeMismatch` / `BadSignature`
("attestation invalid (possible tampering) — re-attest via ganda"); `ParseFailure` (names the
malformed field). See `AttestationVerifier`'s Design region for the full frozen v1 contract
(notes ref, canonical payload bytes, signature encoding, key registry + rotation procedure) —
it must match ganda's `documentation/developer/attestation.md` byte-for-byte.

`DecodeSignature` is STRICT: `sig` must be unpadded base64url only (`[A-Za-z0-9_-]`, no `+`, `/`,
or `=`) — any character outside that alphabet is rejected before a decode is even attempted, so a
padded or standard-alphabet re-encoding of an otherwise-valid 64-byte signature is still refused
(same bytes, wrong wire format). The `RefMissing`/`NoNote` classification pins `LC_ALL=C`/`LANG=C`
on the underlying `git fetch`/`git notes show` calls so the English-substring match on their
stderr is locale-stable rather than assuming the runner's locale.

`.timewarp/dev.jsonc` example (see this repo's own `.timewarp/dev.jsonc` for a live, commented
one):

```jsonc
{
  "attestation": {
    "mode": "warn"
  }
}
```

The package also now ships the new public `AttestationNoteDto`, `AttestationVerifier`,
`AttestationEvaluation`, `AttestationVerificationStatus`, `AttestationConfig`,
`AttestationConfigResolver`, `AttestationMode`, `AttestationModeResolution` types (namespace
`DevCli`) — no known downstream equivalent, so no expected collision; listed here for
completeness (`TagAssertion` precedent above). If a consuming repo already declares its own type
under any of these names, the build fails with `CS0101` (duplicate type in namespace `DevCli`).

#### Key rotation (public half)

The production private key lives only at `~/.timewarp/ganda/keys/` on operator machines and is
touched only via `ganda repo attest`. DevCli holds public material: `AttestationVerifier.KnownKeys`
maps `key_id` → lowercase hex of the 32-byte raw Ed25519 public key (`tw-audit-1` =
`ea6d9ea94f07d0ffe4d46fa7021115f2d5130b715fced113c8742e1d3be94681`, post-rotation 2026-08-08).

To rotate:

1. Generate the new keypair with `ganda repo attest keygen` on an operator machine (ganda-side
   procedure is canonical in ganda `documentation/developer/attestation.md`).
2. Add the new `key_id` → hex to `KnownKeys` **before** ganda starts signing with it. Keep the
   old `key_id` in the map for a grace window so in-flight notes still verify.
3. Bump and ship TimeWarp.Nuru.DevCli. Consuming repos pick up the registry via the package bump.
4. Ganda then signs with the new key. After the grace window, remove the retired `key_id` in a
   later DevCli bump.

Tests and fixtures use a throwaway keypair via `Evaluate`'s `keyOverride` — never the production
key. **Never** point this verifier's tests, or any consuming repo's config, at
`~/.timewarp/ganda/keys/` — that path is the production signing key and is out of scope for
anything outside `ganda repo attest` itself. `AttestationVerifier.KnownKeys` holds only public
key material (hex).

### 3.0.0-beta.72+: `dev release` cuts tag+Release from props (`release-guard.cs`, `release-command.cs`)

`dev release` cuts tag+Release from props (convention rule 5): reads `<Version>` from
`source/Directory.Build.props`, runs the working-tree/branch/sync/tag-availability/publish-state/
CI-run guards (in that order — see `release-guard.cs`'s Design region), then creates annotated tag
`v{Version}` and the GitHub Release (`gh release create v{Version} --title v{Version}
--generate-notes --verify-tag`, plus `--prerelease` when `<Version>` has a SemVer prerelease suffix
such as `-beta.79`, mirroring NuGet) on the verified commit. `--dry-run` runs every guard and prints the
exact commands without creating anything. Humans type the version exactly once — in the props bump
PR; the tag is derived, never hand-typed.

The package also now ships the new public `GuardVerdict`, `ReleaseGuard`, `ReleaseCommand` types
(namespace `DevCli`) — no known downstream equivalent, so no expected collision; listed here for
completeness (`TagAssertion` precedent above). If a consuming repo already declares its own type
under any of these names, the build fails with `CS0101` (duplicate type in namespace `DevCli`).

This closes the untagged double-break-glass residual for tooling-cut releases noted in the two
entries below: because the tag is created and pushed BEFORE any package is published, any resume
attempt has a concrete, already-existing tag to pin against — `workflow-command.cs`'s tag-pin check
enforces that a partial-publish resume runs from the SAME commit the tag points at (tag precedes
publish → tag-pin enforces same-commit resume). The residual remains only for releases NOT cut
through `dev release` (e.g. a fully manual `git tag` + `gh release create` bypassing this command).

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

The package also now ships the new public `IPackableProjectService` / `PackableProjectService`,
`PackableProject` (record), and `MsBuildEvaluationOutput` types (namespace `DevCli`) — no known
downstream equivalent, so no expected collision for `IPackableProjectService`,
`PackableProjectService`, or `PackableProject`; listed here for completeness (`TagAssertion`
precedent above). `MsBuildEvaluationOutput` is a genuinely generic name — flagged explicitly in
case a consuming repo already declares its own type with that name, which would fail with
`CS0101` (duplicate type in namespace `DevCli`).

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

### 3.0.0-beta.72+: release mode promotes CI artifacts (`ci-run-promotion.cs`)

Release mode (`dev workflow --mode release`) no longer rebuilds from source. The pipeline was
`tag-gate -> check-version -> clean -> build -> pack -> push`; it is now `tag-gate ->
check-version -> locate-run -> download-artifact -> verify -> push`. Master-merge CI already
builds, tests, and uploads the `.nupkg` set (`Packages-{run_number}`, green-master
upload step in `workflow.yml`); release now locates the successful `workflow.yml` run at the release commit
(push-event preferred over a same-commit release-event run, then newest), downloads that run's
`Packages-*` artifact, verifies the downloaded file names against the derived packable set at the
source version, and pushes those exact bytes. There is no local `dotnet pack` in release mode
anymore — a green run of CI's `pr` pipeline (`build -> verify-samples -> test`) against the exact
commit is now a hard prerequisite for release, not just an aspiration; a commit with no matching
successful run, or one whose only artifact has expired, aborts with `gh run rerun <run-id>`
guidance instead of silently rebuilding and shipping untested bits (kanban task 458-002, parent
458 finding F2 — "release does not ship what is in master, it ships a new build of the same
source").

Locating and downloading the run uses the `gh` CLI (`gh run list`, `gh api
repos/{owner}/{repo}/actions/runs/{id}/artifacts`, `gh run download`) — `workflow.yml` sets
`GH_TOKEN: ${{ github.token }}` and grants the job `actions: write` for CI runners
(keep-last-two prune; release-mode list/download still works); a local or
break-glass release needs `gh` installed and `gh auth login` run first, or the pipeline aborts
with that guidance before attempting anything.

The package also now ships the new public `CiRunSummary`, `RunArtifact`,
`RunArtifactListResponse`, `CiRunPromotion`, `PackagesArtifactOutcome`,
`PackageSetVerification` types (namespace `DevCli`) — no known downstream equivalent, so no
expected collision; listed here for completeness (`TagAssertion` precedent above). If a consuming
repo already declares its own type under any of these names, the build fails with `CS0101`
(duplicate type in namespace `DevCli`).

**458-005 residual (improved, not fully closed):** release mode no longer ships untested local
builds — every pushed package is now the exact, CI-built-and-tested artifact for a specific commit
(a genuine improvement over the prior "rebuild from source at push time" behavior). The untagged
double-break-glass residual noted above is NOT fully closed by this alone, though: two break-glass
attempts from two DIFFERENT commits, run under one never-bumped, never-tagged version, can still
mix — each attempt downloads and pushes a genuinely CI-tested artifact, but `check-version`'s
Partial state plus `--skip-duplicate` do not verify that both attempts came from the SAME commit,
so packages tested at different commits can still end up published together under one version
number. Full closure needs release to be commit-consistent across attempts, which lands with
458-006 (tag-first tooling — tag before build, so a resume has a concrete commit to pin against).

## Source-Only Package

This is a **source-only NuGet package**. The endpoint and service files are included in your project's compilation, not as a compiled assembly. This is required for Nuru's source generators to work correctly.

### Why Source-Only?

Nuru uses source generators to create route matching code at compile time. The generator needs to see the endpoint class definitions in your project's source. Traditional NuGet packages with compiled DLLs would hide the source from the generator.

### Creating Your Own Source-Only Endpoint Packages

This package serves as a reference for creating your own reusable endpoint packages:

1. Create a project with `IncludeBuildOutput=false`
2. Place endpoint files in `content/any/endpoints/`
3. Place service files in `content/any/services/`. Put a type only the consumer constructs
   (such as this package's `content/any/benchmarks/`) in a folder the props do not compile,
   so a consumer that does not use it does not fail CA1812
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
