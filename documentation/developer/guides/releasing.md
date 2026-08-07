# Releasing TimeWarp.Nuru

How this repo cuts and publishes a release: the version SSOT, the normal flow from
version bump to NuGet, every gate the pipeline enforces and what to do when one
refuses, the break-glass path, and the trusted-publishing setup. A cold reader
should be able to execute a release from this document alone.

This guide documents **this repo's instantiation** of an org-wide TimeWarp
versioning/release convention (developed under kanban task 458). It does not
attempt to restate the convention itself — only what is actually implemented here.

## Overview

- **Version SSOT:** `<Version>` in `source/Directory.Build.props`. There is no
  MinVer, no CI-injected version, and no per-project version overrides.
- **One version, lockstep.** Every packable project in the repo shares that single
  version and releases together. The packable set is never hand-maintained: it is
  derived by evaluating `IsPackable` via real MSBuild evaluation
  (`dotnet msbuild -getProperty:IsPackable,PackageId`) over every `*.csproj` under
  `source/` (excluding `obj/`/`bin/`). Adding, renaming, or removing a packable
  project changes what ships automatically — no list to update.
- **Humans type a version exactly once** — in the props-bump PR. Every tag, every
  NuGet push, and every gate check derives from that one value; nothing downstream
  re-types it.
- **Prerelease versions go through the same pipeline.** A `-beta.N` version is
  released with the identical guards and promotion steps as a stable one — the
  machinery does not distinguish them. When a line stops being prerelease is a
  maintainer judgment call (committing to the current API surface), not something
  this pipeline enforces or tracks.

## Normal release flow

### 1. Bump the version and merge to master

Open a PR that changes `<Version>` in `source/Directory.Build.props` (it may ride a
feature PR or stand alone). Once it merges to master, the `push` event runs
`workflow.yml` in **merge** mode: `dev workflow` executes
`clean → build → verify-samples → test`; the `Packages-{run_number}` `.nupkg`
artifact is uploaded whenever nupkgs exist (the upload step runs on non-release
events with `if-no-files-found: ignore`, and packages exist exactly when the
build succeeded — `GeneratePackageOnBuild` produces them during Build). This is
the CI-tested artifact a later release will promote — nothing is published yet.

### 2. Cut the release with `dev release`

`dev release` (from the `TimeWarp.Nuru.DevCli` package, `release-command.cs`) reads
`<Version>` from `source/Directory.Build.props` and runs eight guards, in order,
before creating anything. Guards 2–8 each print a `✓` line on success (guard 1,
reading the props version, confirms itself implicitly by proceeding); the first
failure aborts with an operator-facing reason and a nonzero exit code:

1. **Props version readable** — `<Version>` parses out of
   `source/Directory.Build.props`.
2. **`gh` available and authenticated** — `gh auth status` succeeds.
3. **Working tree clean** — `git status --porcelain` is empty.
4. **On master** — current branch is `master` (not a feature branch, not detached
   `HEAD`).
5. **In sync with `origin/master`** — `git fetch origin master`, then zero commits
   ahead and zero behind. Ahead-only means push first; behind-only means pull
   first; both nonzero means a genuine divergence to reconcile by hand.
6. **Tag `v{Version}` available** — the tag must not already exist locally or on
   origin. If it does, resume from that commit via break-glass, or bump the
   version.
7. **Publish-state gate** — package set resolves from `.timewarp/dev.jsonc`
   `checkVersionConfig.packages` when configured, else the derived `IsPackable`
   set (`dev release` has no `--package` override — that flag exists only on
   the standalone `check-version` command), and applies a **stricter**
   verdict than `check-version` itself: **None** published passes; **All**
   published aborts ("bump the version"); **Partial** also **aborts** here — at
   this point in `dev release` guard 6 has already confirmed no `v{Version}` tag
   exists yet, so a `Partial` result can only mean an *untagged* prior push (an old
   manual release, or a break-glass run from before this tag existed). Tagging now
   could pin a different commit than that earlier partial push used, so `dev
   release` refuses and asks you to resume via break-glass from the *original*
   commit, or bump the version. (This differs from the promotion pipeline's own
   check-version step below, which *does* let `Partial` proceed — by the time that
   step runs, the tag already exists and pins the commit, closing the same hazard
   a different way. See [Partial-publish resume](#partial-publish-resume).)
8. **A successful CI run of `workflow.yml` exists at `HEAD`** — otherwise the
   `release: published` event's own pipeline would fail later at its
   "locate CI run" step anyway; this guard catches that up front.

Run `dev release --dry-run` first to see every guard evaluated (all read-only, even
the `git fetch origin master` step) plus the exact tag/push/`gh release create`
commands it would run, without creating anything.

With guards passed and not `--dry-run`, `dev release`:

1. Creates an **annotated** tag `v{Version}` at the verified `HEAD` commit.
2. Pushes the tag to `origin`. If the push fails, the local tag is deleted and the
   command refuses — nothing was made public, so it is safe to unwind.
3. Runs `gh release create v{Version} --title v{Version} --generate-notes --verify-tag`.
   If this fails *after* the tag was already pushed, the tag is **not** rolled
   back — it is already public, and re-running `dev release` will now refuse at
   guard 6 (tag availability) by design. The command instead prints the exact
   `gh release create ...` invocation to retry by hand.

### 3. `release: published` runs the promotion pipeline

Publishing the GitHub Release fires the `release` event, which `workflow.yml` maps
to **release** mode (`dev workflow`, no explicit `--mode` needed — mode is
auto-detected from `GITHUB_EVENT_NAME`). This pipeline does **not** rebuild
anything; it promotes the exact bytes CI already built and tested in step 1:

`tag-gate → check-version → locate-run → download-artifact → verify → push`

**Step 1/6 — Release Gate (tag assertions).** Three checks, each a distinct
failure class:

- *Tag assertion* (only on a real `release` event): `GITHUB_REF_NAME` must equal
  `v{Version}` from props exactly. Refusal message: **"release tag does not match
  source version"** — the release tag was typo'd or props drifted after tagging.
- *Tag pin*: if tag `v{Version}` already exists as a local ref, `HEAD` must be at
  that tag's exact commit. Refusal: **"tag pin mismatch"** — a resume attempt is
  running from a different commit than the one that was tagged; either check out
  the tag's commit or bump the version if source has genuinely changed.
- *Ancestor-of-master*: `HEAD` must be reachable from `origin/master` (or `master`
  locally) via `git merge-base --is-ancestor`. Refusal: **"commit not on master"**
  (a real non-ancestor) or **"master ref unresolvable"** (neither
  `origin/master` nor `master` could be resolved — usually a shallow checkout;
  this is exactly why `workflow.yml` uses `fetch-depth: 0`).

**Step 2/6 — Check Version.** Runs the standalone `check-version` gate directly
(not the stricter `ReleaseGuard.CheckPublishState` guard 7 uses) — here `None` and
`Partial` both proceed, only `All` aborts, **"version already released."** `Partial`
is safe to let through at this point because the tag was already created and
pushed back in `dev release` step 3, so the tag-pin check in Step 1/6 above has
already pinned this run to the same commit the earlier partial push used.
Immediately after, the packable project set is derived
(`IPackableProjectService`); an empty set aborts **"no packable projects found
under source/."**

**Step 3/6 — Locate CI Run.** Resolves `HEAD`'s sha and asks
`gh run list --workflow workflow.yml --commit <sha> --status success` for
candidates. `pull_request`-event runs are excluded outright (they build from
GitHub's synthetic merge ref, not the named commit's real tree); among the rest, a
`push`-event run is preferred over a `release`-event run at the same sha, then
newest first. Refusal classes:

- **"gh CLI unavailable"** — `gh` could not even be launched (install it; on
  runners `GH_TOKEN` is already wired).
- **"gh run list failed"** — `gh` ran but exited nonzero; the real stderr is
  surfaced (auth failure, rate limit, network — retry or `gh auth login`).
- **"no successful CI run ... exists for commit"** — this commit never went
  through the merge pipeline. Fix and re-run CI (`gh run rerun <id>`) first.

**Step 4/6 — Download Artifact.** Walks the candidate runs looking for a
non-expired `Packages-*` artifact (the same one `workflow.yml` uploads on `push`
events). Refusals:

- **"every candidate CI run's Packages-* artifact has expired"** — regenerate with
  `gh run rerun <run-id>` (reruns the same commit, so tested-bytes identity holds).
- **"no candidate CI run ... uploaded a Packages-* artifact"** — no run ever
  produced one; same `gh run rerun` remedy.

**Step 5/6 — Verify Package Set.** Compares the downloaded `.nupkg` file names
against `{PackageId}.{version}.nupkg` for every project in the derived packable
set. Any mismatch (missing or unexpected file) aborts **"downloaded package set
does not match derived packable set"** — typically the located CI run predates the
version bump; re-run CI on the target commit and retry.

**Step 6/6 — Push.** Pushes each verified `.nupkg` to `nuget.org` with
`dotnet nuget push --skip-duplicate`, using the short-lived API key from OIDC
trusted publishing (below). `--skip-duplicate` makes a resumed push idempotent at
the HTTP layer for packages already published; it does not verify byte identity —
that guarantee comes from the tag-pin check in Step 1, not from this flag.

Success prints `Pipeline SUCCEEDED - Packages published to NuGet.org`.

## Why promotion, not rebuild

The published bytes are **byte-identical** to the bytes the merge-CI run already
built, ran `verify-samples` and `test` against, and uploaded. There is no rebuild
at release time and therefore no chance of SDK/Roslyn drift between merge time and
release time changing generated code — a real risk for a source-generator product,
since `actions/setup-dotnet` resolves `10.0.x` freshly on every run. "Published"
and "tested" are the same artifact, by construction, not by convention.

## Break-glass

A bare `workflow_dispatch` (default inputs) runs in **merge** mode — `dev
workflow` never publishes on it. One input combination neither merges nor
releases: `mode: release` with a missing/wrong `confirm` fails the run
immediately at the confirmation guard. Publishing via dispatch
requires two explicit inputs together: `mode: release` **and** `confirm: release`
(typed exactly). `workflow.yml`'s "Validate break-glass confirmation" step fails
the run immediately if `mode` is `release` but `confirm` doesn't match. Only when
both are set does the OIDC NuGet login run and does the workflow invoke
`dev workflow --mode release --api-key ...` — the explicit `--mode` bypasses the
`GITHUB_EVENT_NAME`-based auto-detection that would otherwise map
`workflow_dispatch` to `merge`.

**When to use it:** recovering or resuming a release that didn't complete —
for example the `release: published` webhook never fired, or the promotion
pipeline aborted partway (an expired artifact after a fix, a transient `gh`
failure, or a partial NuGet push).

**Tag-pin forces a same-commit resume.** Because `dev release` creates and pushes
the tag *before* anything is published, any resume — break-glass or otherwise —
must run from the commit the tag already points at. The release gate's tag-pin
check (Step 1/6 above) enforces this: a break-glass dispatch running from a
different commit than the existing `v{Version}` tag aborts with "tag pin
mismatch," not a silent mixed-commit publish.

### Partial-publish resume

If an earlier attempt pushed some but not all packages under one version (`check-
version`'s `Partial` state — some packages already on NuGet.org, some not), the gate
passes with a warning and the run resumes: `--skip-duplicate` no-ops the
already-published packages and pushes only the rest. This is safe only when the
resuming run is at the **same commit** as the original partial push — which the
tag-pin check guarantees whenever the release was cut through `dev release` (the
tag exists before any push happens, so any resume is pinned to it). If the source
has genuinely changed since the partial push, bump the version instead of
resuming.

## Trusted publishing

NuGet credentials are never stored as a repository secret. `workflow.yml` grants
the job `id-token: write` and, only when the release condition is true (`release`
event, or confirmed break-glass dispatch), runs `nuget/login@v1` with
`user: TimeWarp.Enterprises`. This exchanges the job's OIDC token for a short-lived
NuGet API key (`steps.nuget-login.outputs.NUGET_API_KEY`), passed straight into
`dev workflow --api-key ...` for the push step. The policy authorizing this
exchange (which repo, which workflow file, which NuGet package) is configured per
package on NuGet.org's Trusted Publishing settings, not in this repo.

## Operator / maintenance appendix

- **Outstanding post-merge step — required status check.** Master branch
  protection does not yet require the `ci` check to pass before merge (PR CI is
  currently advisory). Once the 458 program's changes are on master, run once:

  ```
  PUT repos/TimeWarpEngineering/timewarp-nuru/branches/master/protection
  ```

  setting `required_status_checks` context `ci` with `strict: false`, while
  preserving the existing `enforce_admins` setting and the existing 0-approval
  review requirement. (Read the current protection settings first — a PUT to this
  endpoint replaces the whole configuration, so untouched fields must be copied
  forward explicitly.) This is a one-time operator action, not something `dev`
  tooling runs.

- **Artifact retention.** The `Packages-{run_number}` upload step in `workflow.yml`
  sets no `retention-days`, so GitHub's default retention applies (90 days unless
  the repo's Settings → Actions → General overrides it). If the artifact for the
  commit you need has expired, regenerate it with `gh run rerun <run-id>` — reruns
  execute the same commit, so the tested-bytes property is preserved.

- **Distance warning.** A planned `check-version` enhancement (kanban task 456, not
  yet implemented) will warn when the source `<Version>` has drifted more than one
  increment ahead of the last released version — a signal that bumps are being
  merged faster than releases are being cut. Not yet present in this repo's
  `check-version` output.
