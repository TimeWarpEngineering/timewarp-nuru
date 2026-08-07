# TimeWarp.Nuru.DevCli

Reusable dev-cli endpoints and services for TimeWarp repositories. This package provides source-only files that can be consumed by any TimeWarp repository.

## What's Included

### Endpoints

| Endpoint | Description | Dependencies |
|----------|-------------|--------------|
| `clean` | Clean solution and build artifacts | `IRepoCleanService` (TimeWarp.Amuru) |
| `self-install` | AOT compile dev CLI to ./bin | None (standalone) |
| `check-version` | Verify version is ready to release — three-state gate: none published (proceed), all published (abort), some published (resume with warning) | `NuGetVersionService`, `IRepoConfigService`, `IPackableProjectService` |
| `release` | Cut a release: create tag `v{Version}` (from `source/Directory.Build.props`) and the GitHub Release, gated by working-tree/branch/sync/tag-availability/publish-state/CI-run guards; `--dry-run` runs every guard and previews the exact commands without creating anything | `NuGetVersionService`, `IRepoConfigService`, `IPackableProjectService` |

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
| `CiRunPromotion` | Pure logic for release-mode artifact promotion: orders candidate CI runs for a commit, selects a run's `Packages-*` artifact (including expiry handling), and verifies a downloaded `.nupkg` set against the derived packable set — no process execution |
| `ReleaseGuard` (`GuardVerdict`) | Pure per-guard classifiers for the `release` gate: working tree clean, on master, synced with origin, tag `v{Version}` available (neither local nor remote), and publish state (none/partial/all) — no process execution |
| `AttestationVerifier` (`AttestationNoteDto`, `AttestationEvaluation`, `AttestationVerificationStatus`) | Pure verifier for ganda-audit attestation notes (kanban task 458-010): parses the frozen v1 note JSON, rebuilds the canonical signed payload, decodes the unpadded-base64url signature, resolves `key_id` against a baked-in key registry (or a test-only `keyOverride`), and compares the note's tree against the tree being verified — no process execution; the actual Ed25519 verify (via `openssl pkeyutl -verify -rawin`) runs in `workflow-command.cs`, not here |
| `AttestationConfig` | Config model for the attestation verify step's `mode` (`"warn"`\|`"require"`, default `warn`) |
| `AttestationConfigResolver` (`AttestationMode`, `AttestationModeResolution`) | Pure resolver for `attestation.mode`: blank/absent and "warn"/"require" (case-insensitive) resolve exactly; any other non-blank value resolves to `Warn` but also returns the offending raw value so the caller can warn about a typo (e.g. `"requiree"`) instead of silently falling back |

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

### 3.0.0-beta.72+: attestation verify step (`attestation-verifier.cs`, `attestation-config.cs`)

`dev workflow` now includes an attestation verify step (kanban task 458-010) — the DevCli/public
half of "sign locally in ganda, verify in CI." Ganda (private, operator-only) audits a repo and
signs evidence over `(tree sha, check-set hash, timestamp)` into `refs/notes/ganda-audit`; this
package's `AttestationVerifier` rebuilds that evidence and `workflow-command.cs` shells out to
`openssl pkeyutl -verify -rawin` to check the Ed25519 signature — no BCL Ed25519 verify exists,
and a crypto NuGet dependency was rejected for this source-only package's posture, so `openssl`
must be on PATH (runners and operator machines) or the step reports `VerifierUnavailable`.

Pipeline shape by mode:

- **PR/merge mode**: new Step 1 ("Attestation"), before Clean — Steps renumber from `1/4..4/4` to
  `1/5..5/5`. Governed by `.timewarp/dev.jsonc` `attestation.mode`, resolved by the pure
  `AttestationConfigResolver.ResolveMode`: `"warn"` (default — nothing is attested org-wide yet)
  prints a loud advisory on any non-`Valid` outcome but never fails the pipeline (the advisory
  repeats once more immediately before the `SUCCEEDED` banner so it survives scrollback);
  `"require"` fails the pipeline on any non-`Valid` outcome. An unrecognized non-blank value (a
  typo like `"requiree"`) still resolves to `warn` — it never silently becomes `require` — but
  prints `Warning: unrecognized attestation.mode '<value>' — treating as 'warn'. Valid values:
  warn, require.` so the operator does not believe enforcement is on when it is not.
- **Release mode**: hard gate, always — inserted into the existing Step 1/6 gate block
  immediately after the ancestor-of-master check, and **ignores** `attestation.mode` entirely. A
  release with no verifiable audit evidence must never ship; the runner never signs, so a missing
  or invalid attestation aborts with "pull master locally so ganda can attest."

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
one — default `warn`, nothing to configure until you want to flip to `require`):

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

**Never** point this verifier's tests, or any consuming repo's config, at
`~/.timewarp/ganda/keys/` — that path is the production signing key and is out of scope for
anything outside `ganda repo attest` itself. `AttestationVerifier.KnownKeys` holds only public
key material (hex), and `Evaluate`'s `keyOverride` parameter exists specifically so tests can
inject a throwaway keypair instead.

### 3.0.0-beta.72+: `dev release` cuts tag+Release from props (`release-guard.cs`, `release-command.cs`)

`dev release` cuts tag+Release from props (convention rule 5): reads `<Version>` from
`source/Directory.Build.props`, runs the working-tree/branch/sync/tag-availability/publish-state/
CI-run guards (in that order — see `release-guard.cs`'s Design region), then creates annotated tag
`v{Version}` and the GitHub Release (`gh release create v{Version} --title v{Version}
--generate-notes --verify-tag`) on the verified commit. `--dry-run` runs every guard and prints the
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
builds, tests, and uploads the `.nupkg` set (`Packages-{run_number}`, always-run upload step in
`workflow.yml`); release now locates the successful `workflow.yml` run at the release commit
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
`GH_TOKEN: ${{ github.token }}` and grants the job `actions: read` for CI runners; a local or
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
