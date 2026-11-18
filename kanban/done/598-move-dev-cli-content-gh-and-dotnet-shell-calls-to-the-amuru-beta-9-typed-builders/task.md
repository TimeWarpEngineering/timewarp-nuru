# Move DevCli content gh and dotnet Shell calls to the Amuru beta.9 typed builders

## Description

`TimeWarp.Nuru.DevCli` ships its endpoints and services as source (`content/any/…`) that
compile into each consumer's dev CLI. Those files still call `Shell.Builder("gh")` and
`Shell.Builder("dotnet")`. TimeWarp.Amuru 3.0.0-beta.9 adds the AMURU0001 analyzer, which
reports these calls as compile errors. Any repo that bumps Amuru to beta.9 cannot build its
dev CLI against DevCli 4.0.0-beta.15.

Found by timewarp-jaribu task 077 (the dev CLI self-install failed after the bump):

```
content/any/endpoints/release-command.cs(142,30): error AMURU0001: Use Gh.Auth().Status() … "auth status"
content/any/endpoints/release-command.guards.cs(266,45): error AMURU0001: Use Gh.Run().List() … "run list"
content/any/endpoints/release-command.guards.cs(287,49): error AMURU0001: Use Gh.Api() … "api"
content/any/services/github-packages-credential.cs(47,22): error AMURU0001: Use Gh.Auth().Token() … "auth token"
content/any/services/packable-project-service.cs(221,12): error AMURU0001: Use DotNet.MsBuild() … "msbuild"
```

`release-command.guards.cs:220` (`gh release create`) is also in the `Gh` table
(`Gh.Release().Create()`), so expect a sixth hit once the others are fixed.

## Requirements

- Bump `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` to 3.0.0-beta.9 in `Directory.Packages.props`.
- Move every AMURU0001 site under `source/timewarp-nuru-devcli/content/` (and anywhere else
  in the repo) to the typed `Gh.*` / `DotNet.*` builder. No `#pragma`, no `.editorconfig`
  downgrade, no `ExcludeAssets="analyzers"`.
- Where a typed builder lacks an option the call needs, use the base `DotNet.Builder()`
  (same argument list) and file a task in timewarp-amuru naming the missing option.
- The DevCli content package must declare (or document) the Amuru.Tools dependency the
  typed builders need, so consumers compile the content.
- Release a new `TimeWarp.Nuru.DevCli` beta so consumers can bump.

## Goal (implementer restatement)

Amuru 3.0.0-beta.9 ships AMURU0001, which turns every raw `Shell.Builder("gh"|"dotnet")` call that
has a typed `Gh.*` / `DotNet.*` builder into a compile error. DevCli content compiles inside each
consumer's dev CLI, so consumers that bump Amuru cannot build. Fix it at the source:

- Pin `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` at 3.0.0-beta.9 in `Directory.Packages.props`.
- Rewrite every AMURU0001 site (DevCli content first, then the rest of the repo: tools/dev-cli,
  tests, benchmarks, hooks) to the typed builder. Where the typed builder lacks an option, fall back
  to `DotNet.Builder().WithArguments(...)` and record a timewarp-amuru task for the missing option.
  No pragma, no editorconfig downgrade, no analyzer exclusion.
- Document in the DevCli readme that consumers need `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools`
  3.0.0-beta.9+ (the content uses the typed builders from Amuru.Tools).
- Bump the repo version so a new DevCli beta can be released after merge (`dev release` from master
  is the post-merge step).

Acceptance: `./bin/dev build` is green on beta.9 with zero AMURU0001, the dev CLI (which compiles
DevCli content) self-installs, the touched runfiles still run, and `ganda repo audit` passes.

## Checklist

- [x] Amuru pins at 3.0.0-beta.9, zero AMURU0001 in the repo and in DevCli content
- [x] DevCli content compiles in a consumer dev CLI against Amuru beta.9
- [ ] DevCli beta released (version bumped to 4.0.0-beta.16 here; `dev release` from master after merge)

## Depends on

- None. Amuru 3.0.0-beta.9 is published.

## Session

- Created: 1894826 (2026-11-18)
- 2026-11-18 implementer (Claude Opus 5.5): bumped Amuru, moved every AMURU0001 site, filed
  timewarp-amuru 154, bumped to 4.0.0-beta.16. Boy-scout: `tests/scripts/run-*.cs` had pre-existing
  RS0030 (`Console` banned) errors; they now write through `await Terminal.WriteLineAsync`. Audit
  advisory `vscode-window-icon` fixed with `ganda repo audit --fix` (ignored local file).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-11-18T06:14:06Z
- 2026-11-18 review oracle (Grok, session `01a1ed4a-a831-71b2-8465-f26eb670ce0b`): effort 2, roster general, round 1, disposition clean. Max-turns 120.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1ed4a-a831-71b2-8465-f26eb670ce0b, max-turns 120, artifacts general.md, merged.md, disposition.md — 2026-11-18T06:25:55Z

## Review

- Round 1: Done

## Notes

Blocks timewarp-jaribu 077 (Amuru beta.9 bump). The ganda sibling (ganda 670) likely hits
the same errors if it consumes Nuru.DevCli content.

## Results

### What changed

- `Directory.Packages.props`: `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` 3.0.0-beta.5 → 3.0.0-beta.9.
- DevCli content (`source/timewarp-nuru-devcli/content/any/`):
  - `release-command.cs`: `gh auth status` → `Gh.Auth().Status()`.
  - `release-command.guards.cs`: `gh run list` → `Gh.Run().List().WithWorkflow/WithCommit/WithStatus/WithJson`,
    `gh api` → `Gh.Api(endpoint)`, `gh release create` → the `GhReleaseCreateBuilder` from
    `ReleaseGuard.BuildReleaseCreate(tag, version)` (replaces `BuildReleaseCreateArguments`; the
    printed command comes from the same builder's `ToArguments()`, so `--prerelease` now precedes
    `--verify-tag`; `release-02` expectations updated).
  - `github-packages-credential.cs`: `gh auth token` → `Gh.Auth().Token()`.
  - `packable-project-service.cs`: `dotnet msbuild … -nodeReuse:false` → `DotNet.Builder()` with the
    same argument list, because `DotNet.MsBuild()` has no node-reuse option (timewarp-amuru 154).
- `tools/dev-cli`: analyze → `DotNet.Tool().Run("roslynator")`; format → `DotNet.Builder()` (no
  `DotNet.Format()` yet, amuru 154); test → `DotNet.Run().WithFile()`; publish →
  `DotNet.NuGet().Push()`; attestation → `Gh.Run().List()`, `Gh.Api()`, `Gh.Run().Download()`.
- Tests/benchmarks/scripts: mcphost-02/04, run-standalone-tests, run-ci-tests, tests/scripts/run-*.cs →
  `DotNet.Run()` / `DotNet.Build()`; search-05/07 → `DotNet.Builder()` (`dotnet <dll>`); benchmark →
  `DotNet.Publish()`.
- stdin sites (routing-33b, daemon-03): no typed dotnet builder takes stdin (amuru 154), and the
  analyzer also flags `Shell.Run("dotnet")`. Both now build the app once with `DotNet.Build(file).WithOutput(dir)`
  and run the built apphost directly with `Shell.Builder(apphost).WithStandardInput(...)`. daemon-03's
  MCP client launches the same apphost (`mcp`) instead of `dotnet run --no-build`.
- `.NET 11` collects `#:package` directives from files compiled into the CI multi assembly, so a
  `#:package TimeWarp.Amuru.Tools` there duplicated the ci-tests `PackageReference` (NU1504). The
  mcphost, daemon, search and routing test directories get `PackageReference TimeWarp.Amuru.Tools`
  in their `Directory.Build.props` instead.
- Docs: DevCli readme Requirements (Amuru + Amuru.Tools 3.0.0-beta.9+) and a 4.0.0-beta.16 migration
  note; changelog Unreleased/Changed entry. `source/Directory.Build.props` → 4.0.0-beta.16.
- Follow-up filed: timewarp-amuru task 154 (`DotNetMsBuildBuilder.WithNodeReuse`, `DotNet.Format()`,
  `WithStandardInput` / `WithTimeout` on the run builder), published to amuru to-do.
- No `#pragma`, `.editorconfig` downgrade, `NoWarn` or `ExcludeAssets="analyzers"` added.

### Gates

- `./bin/dev build` (after `dotnet run --file tools/dev-cli/dev.cs -- self-install`): Build completed successfully, 0 errors.
- `dotnet build tests/devcli-consumer-check` (DevCli content as a net11 consumer on Amuru beta.9): Build succeeded, 0 errors.
- `dotnet build tests/ci-tests/run-ci-tests.cs` (CI multi assembly compile, includes search/mcphost/daemon/devcli services): rc=0, no AMURU0001.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/release-02-prerelease-flag.cs`: 6/6 passed.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/release-01-guard-matrix.cs`: 37/37 passed.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/packable-projects-01-parse.cs`: 21/21 passed.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/packable-projects-02-derivation-fixture.cs`: 4/4 passed.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/packable-projects-03-parallel.cs`: 8/8 passed.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/check-version-09-two-feed-lookup.cs`: 16/16 passed.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/check-version-09b-declared-feed-lookup.cs`: 10/10 passed.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/check-version-04-endpoint-zero-package.cs`: 1/1 passed.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/check-version-07-endpoint-fail-closed.cs`: 9/9 passed.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/check-version-07b-endpoint-declared-feeds.cs`: 2/2 passed.
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-33b-json-args-stdin-and-defaults.cs`: 4/4 passed.
- `dotnet run --file tests/timewarp-nuru-daemon-tests/daemon-03-served-app.cs`: 1/1 passed.
- `dotnet run --file tests/timewarp-nuru-mcphost-tests/mcphost-02-calculator-parity.cs`: 2/2 passed.
- `dotnet run --file tests/timewarp-nuru-mcphost-tests/mcphost-04-schema-fidelity.cs`: 6/6 passed.
- `dotnet run --file tests/timewarp-nuru-search-tests/search-05-search-query-output.cs`: does not compile standalone (CS0436 `Mediator` conflict), pre-existing before this change; it compiles in the CI multi assembly, CI runs it.
- `dotnet run --file tests/timewarp-nuru-search-tests/search-07-cli-key.cs`: does not compile standalone (CS1729 `SearchIndex` ctor), pre-existing before this change; it compiles in the CI multi assembly, CI runs it.
- `benchmarks/operation-dispatch/operation-dispatch-overhead.cs`, `tests/ci-standalone/run-standalone-tests.cs`, `tests/ci-tests/run-ci-tests.cs`, `tests/scripts/run-*.cs`: compiled with `dotnet build <file>` (rc=0); not run (benchmark / suite runners; CI runs the suites).
- `ganda repo audit`: passes, 1 advisory (`vscode-window-icon`); `ganda repo audit --fix` → passes all checks.
- Post-commit attest then failed `package-pin-currency` (TimeWarp.Jaribu 1.0.0-beta.17 published the same day). Boy-scout fix: Jaribu pin 1.0.0-beta.16 → 1.0.0-beta.17 in `Directory.Packages.props`. Reran: release-02 6/6, routing-33b 4/4, `dotnet build tests/ci-tests/run-ci-tests.cs` rc=0, `ganda repo audit` passes all checks.
- Smoke: `./bin/dev check-version` → `Evaluating 9 projects for IsPackable…`, `Version in source is new`, exit 0; `./bin/dev release --dry-run` → `✓ gh authenticated` (then refuses on the dirty tree, as expected).

### How to validate

Smoke:
```bash
./bin/dev build
(cd tests/devcli-consumer-check && dotnet build)
dotnet run --file tests/timewarp-nuru-tests/devcli/release-02-prerelease-flag.cs
dotnet run --file tests/timewarp-nuru-tests/routing/routing-33b-json-args-stdin-and-defaults.cs
grep -rnE 'Shell\.(Builder|Run)\(\s*"(gh|dotnet)"' --include=*.cs source tools tests benchmarks
```

Expect:
- Build and the consumer check succeed with 0 errors (no AMURU0001) on Amuru 3.0.0-beta.9.
- release-02 passes 6/6 with `gh release create v3.0.0-beta.79 --title v3.0.0-beta.79 --generate-notes --prerelease --verify-tag`.
- routing-33b passes 4/4 (stdin piped into the built harness apphost).
- The grep prints nothing.

### Review

- Round 1. Effort 2. Roster: general. Session `01a1ed4a-a831-71b2-8465-f26eb670ce0b`.
- Counts: bug 0 open / 0 fixed / 0 wontfix, suggestion 0, nit 0.
- Disposition: clean. No wontfix and no escalation. The goal restatement matches the original task.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
- This review read the diff and the recorded gates. It did not re-run `./bin/dev build`, `./bin/dev test`, or `ganda repo audit`.
