# Publish only tested artifacts: promote from master CI, or retest

Parent: 458 (finding F2 in `458-*/review/findings.md`, revised 2026-08-07).

## Description

The release run today rebuilds from source with a floating SDK
(`setup-dotnet: '10.0.x'` resolves at run time) and pushes those fresh binaries
without running tests. So the published packages are **never** the bits any test
run exercised — release does not ship "what is in master," it ships a new build of
the same source. Verified enablers: master branch protection has no
`required_status_checks` (PR CI is advisory), the release event never reads the
commit's CI status, and the workflow path filters omit `*.slnx` / `assets/**`.

Two coherent designs (convention.md rule 7); current rebuild-without-retest is the
only incoherent one:

- **Design B — build-once / promote (preferred).** Master merge CI builds, tests,
  uploads the `.nupkg` set (already uploads `Packages-{run_number}`; version is
  final because the props bump merged first). Release job: no rebuild — locate the
  successful CI run for the tag's commit, download its artifacts, run
  check-version gate, push those exact bytes. Fail loudly if no green run /
  artifact exists for the commit.
- **Design A — rebuild + retest (fallback).** Keep the rebuild, insert
  `verify-samples → test` between build and pack.

## Checklist

- [x] Decided 2026-08-08 (operator): **Design B — build-once / promote**. Design A not chosen.
- [ ] Enable required status checks on master (green CI enforced at merge) — **operator
      post-merge step, not run by this implementation session**: PUT
      `repos/TimeWarpEngineering/timewarp-nuru/branches/master/protection` with
      `required_status_checks` context `ci`, `strict: false`, preserving
      `enforce_admins` and the existing 0-approval review requirement (D8).
- [x] Release job resolves tag commit → successful CI run → downloads `Packages-*` artifact; hard-fail with re-run guidance when absent; confirm artifact retention covers the bump→release window
- [x] **Drop workflow path filters entirely** (decided 2026-08-08 — CI runs on every PR/push; closes the `*.slnx`/`assets/**` gap; kanban/docs PRs cost minutes, accepted)
- [x] Update mode comments in `workflow-command.cs` (and `tools/dev-cli/dev.cs`'s
      pipeline description line) — done here. The releasing guide itself is
      tracked separately in 458-008 (not yet written; still `to-do`), so that
      half of this item is deliberately left for 458-008 rather than
      pre-empted here.

## Results

Implemented in commits `02cbcf4f` (promotion pipeline) and `bd79b9bc` (review
fixes). Release mode is now build-once/promote:

- **Pipeline:** tag-gate → check-version → locate-run → download-artifact →
  verify → push. No Clean/Build/Pack in release mode; `PackProjectsAsync`
  deleted. Every pushed byte comes from a successful CI run at the exact HEAD
  sha (push-event runs preferred; **pull_request runs excluded by
  construction** — they build from synthetic merge refs); candidate walk is
  expiry-aware with `gh run rerun` guidance; downloaded set verified
  bidirectionally against the derived packable set at the props version.
- **workflow.yml:** path filters removed (CI on every PR/push — closes the
  slnx/assets gap and makes the `ci` check always report); `actions: read` +
  `GH_TOKEN` added; artifact upload skipped on release runs.
- **Failure paths:** gh-missing vs gh-failed distinguished with stderr
  surfaced; no-green-run, expired-artifact, and set-mismatch each abort
  non-zero with precise guidance before push.
- **458-005 interplay (stated honestly):** the untagged double-break-glass
  residual is improved (every package now CI-tested for a specific commit),
  not closed — cross-attempt commit mixing under one unbumped version remains
  until 458-006's tag-first cutting.
- **Review (Phase 4b):** 2 rounds, effort 1. 5 findings: 3 MED + 1 LOW
  resolved, 1 LOW/INFO wontfix. Disposition: **accepted-exceptions**
  (`review/disposition.md`). Reviewer live-verified merge-ref checkout
  behavior, 100-run collision audit, real gh auth failure, artifact
  extraction layout, and the upload condition across all 5 trigger shapes.

### How to validate

Smoke:
1. `dotnet run tests/timewarp-nuru-tests/devcli/promotion-01-run-selection.cs`
   → 11/11; `promotion-02` → 8/8; `promotion-03` → 5/5; `promotion-04` → 6/6
   (fixtures are verbatim live gh payloads).
2. `gh run list --workflow workflow.yml --commit b2eea2c9acdd5f1a0cd3f1a07af36ed1658409b1 --status success --json databaseId,event` → the beta.71 push+release runs; `gh run download 27553073284 --name Packages-42 --dir <scratch>` → 5 nupkgs, flat.
3. `dotnet run --file tools/dev-cli/dev.cs -- workflow --mode release` from a
   non-tag commit → aborts at Step 1/6 tag-pin before any locate/download.
4. Read workflow.yml: no `paths:` blocks; `actions: read`; upload step's `if`
   excludes release + confirmed break-glass dispatch.

Automated gate: `ganda runfile cache --clear && dotnet run tests/ci-tests/run-ci-tests.cs`
→ 0 failed (last: 1498 total / 1491 passed / 7 skipped).

**Depends on (operator, post-merge):** the branch-protection PUT enabling
required status check `ci` — exact command in Notes (D8); run it right after
this branch merges to master. Full end-to-end promotion is exercised by the
next real release (beta.72): its log must show locate → download → verify →
push and no build steps.

## Notes

### Implementation plan (Phase 2, 2026-08-08) — key decisions (all live-verified)

- Release pipeline becomes: tag-gate → check-version → **locate-run →
  download-artifact → verify → push**. No Clean/Build/Pack in release mode;
  merge CI already produces+uploads the nupkg set (GeneratePackageOnBuild +
  existing `Packages-{run_number}` upload; live artifact from the beta.71 run
  verified non-expired at 53 days).
- D2: run selection = successful workflow.yml runs at the tag's HEAD sha,
  push-event first then newest; walk candidates until a non-expired
  `Packages-*` artifact is found (release-event runs at the same sha won't
  have one post-change).
- D3: pure logic (run ordering, artifact selection incl. expiry, bidirectional
  version-pinned set verification, JSON DTOs) in shared content
  `ci-run-promotion.cs`; gh orchestration stays repo-local in
  workflow-command.cs. AOT: DTOs added to DevCliJsonContext.
- D5: `PackProjectsAsync` deleted (unused private method fails build anyway;
  a pack fallback would invite pushing untested bytes).
- D6: workflow.yml gains `actions: read` permission + `GH_TOKEN` env (gh on
  runners needs both); path filters deleted from push+pull_request.
- D8: sequencing — workflow changes merge to master FIRST, then the
  branch-protection PUT (exact command in plan; context `ci`, strict false,
  preserves enforce_admins + 0-approval review requirement). PUT is an
  operator/post-merge step.
- D9: break-glass/local release uses the same promotion path (gh user auth;
  clear failure if gh missing). **Improves (does not fully close) the 458-005
  untagged double-break-glass residual**: release mode never builds locally,
  so every pushed package is now a genuine CI-tested artifact for a specific
  commit — but two break-glass attempts from two DIFFERENT commits can still
  mix under one never-bumped, never-tagged version (`check-version`'s Partial
  state + `--skip-duplicate` do not verify cross-attempt commit consistency).
  Full closure lands with 458-006 tag-first tooling (round-1 review finding
  #3, corrected from the original overstated "fully closed" phrasing).
- D10: artifact expiry/missing → hard fail with `gh run rerun <id>` guidance
  (rerun executes the same sha — tested-bytes property preserved).
- Tests: promotion-01..04 (run selection, artifact selection, set
  verification, real-payload JSON round-trip through source-gen context).

Raised by operator during 458 review: "it should only release what is in master,
not something else" — that is Design B's property, byte-identical promotion. A
retest is only needed when a rebuild exists.

### Session — implementation (2026-08-07)

Implemented per the Phase 2 plan above. Changes left uncommitted in the working tree:

- `.github/workflows/workflow.yml`: deleted both `paths:` blocks; added
  `actions: read` permission; added `env: GH_TOKEN: ${{ github.token }}` to the
  "Run CI Pipeline" step.
- New shared content `source/timewarp-nuru-devcli/content/any/services/ci-run-promotion.cs`:
  `CiRunSummary`/`RunArtifact`/`RunArtifactListResponse` DTOs,
  `PackagesArtifactStatus` enum, `PackagesArtifactOutcome`/`PackageSetVerification`
  records, static `CiRunPromotion` with `OrderCandidateRuns`,
  `SelectPackagesArtifact`, `VerifyPackageSet` — pure logic, no process
  execution. `dev-cli-json-context.cs` gained `List<CiRunSummary>` and
  `RunArtifactListResponse` to the AOT source-gen context.
- `tools/dev-cli/endpoints/workflow-command.cs`: release pipeline reshaped to
  tag-gate → check-version → locate-run → download-artifact → verify → push
  (6 steps). New `LocateCiRunAsync` (git rev-parse HEAD + `gh run list`,
  `LocateRunStatus` outcome: Found/GhUnavailable/NoMatchingRun) and
  `DownloadPackagesArtifactAsync` (walks candidates via `gh api .../artifacts`
  + `gh run download`, clears/recreates `artifacts/packages` before download,
  `DownloadArtifactStatus` outcome: Downloaded/Exhausted, tracks
  `ExpiredArtifactEncounter`s for the abort message). `PackProjectsAsync`
  deleted entirely. Header pipeline comment updated here and in
  `tools/dev-cli/dev.cs`.
- Tests: `tests/timewarp-nuru-tests/devcli/promotion-01-run-selection.cs` (8
  tests), `promotion-02-artifact-selection.cs` (8),
  `promotion-03-package-set-verification.cs` (5),
  `promotion-04-json-parse.cs` (6, real live-verified gh payloads for sha
  `b2eea2c9acdd5f1a0cd3f1a07af36ed1658409b1` / runs 27553776617 (release) and
  27553073284 (push) / artifact `Packages-42`) — 27 new tests total. Added
  `ci-run-promotion.cs` `Compile Include` to both
  `tests/timewarp-nuru-tests/devcli/Directory.Build.props` and
  `tests/ci-tests/Directory.Build.props`.
- Docs: `source/timewarp-nuru-devcli/readme.md` — new Services table row for
  `CiRunPromotion`, new Migration Notes section "3.0.0-beta.72+: release mode
  promotes CI artifacts (ci-run-promotion.cs)" with the CS0101 note for the
  six new public types and the 458-005 residual-closure note.

**Verification (all green):**
- `dotnet run tests/timewarp-nuru-tests/devcli/promotion-0{1,2,3,4}-*.cs` — 8+8+5+6 = 27/27 passed.
- `dotnet build timewarp-nuru.slnx` — 0 warnings, 0 errors.
- `dotnet run tests/ci-tests/run-ci-tests.cs` — 0 failed (7 pre-existing skips, unrelated to this task; new promotion tests ran and passed inside the multi-mode assembly).
- `gh run list --workflow workflow.yml --commit b2eea2c9... --status success --json ...` — returns the same 2 live runs the plan cites.
- `dotnet run --file tools/dev-cli/dev.cs -- workflow --mode release` from dev HEAD — aborts at Step 1/6 tag-pin Mismatch, confirming gate order is preserved (locate/download/verify never run when an earlier gate fails).
- `dotnet run --file tools/dev-cli/dev.cs -- --help` — compiles/runs clean (sanity check on the whole dev-cli after the workflow-command.cs rewrite).

**Deviations / notes for reviewer:**
- `LocateCiRunAsync`'s `git rev-parse HEAD` failure path is not explicitly
  specced; on nonzero exit it throws `InvalidOperationException` (fail-loud,
  consistent with `PackableProjectService`'s MSBuild-eval-failure precedent)
  rather than adding a third `LocateRunStatus` — HEAD is already resolved
  successfully by the ancestor check earlier in the same pipeline run, so this
  path is not expected to be reachable in practice.
- ~~gh missing vs. unauthenticated are not distinguished~~ — superseded by
  round-1 review finding #2 below: gh-could-not-launch (`GhUnavailable`) and
  gh-launched-but-exited-nonzero (`GhFailed`, carrying stderr) are now
  distinguished.
- Branch-protection PUT intentionally NOT run (explicit instruction) — left as
  the sole unchecked checklist item, an operator post-merge step per D8.
- No commit made (explicit instruction) — all changes are in the working tree.

### Session — round-1 review fixes (2026-08-07)

Round-1 review of commit `02cbcf4f` (full record:
`review/round-1/merged.md`) returned 4 fix findings + 1 wontfix. All 4 fixed
in the working tree (still uncommitted, per instruction):

- **Fix 1 (MED)** — `CiRunPromotion.OrderCandidateRuns` now excludes
  `event == "pull_request"` runs from candidacy outright (filtered alongside
  the foreign-sha rows, single combined `Where` to avoid an RCS1112 chain
  warning). Design region explains why: a `pull_request` run checks out
  GitHub's synthetic merge ref (`refs/pull/N/merge`), so its artifacts are
  built from a DIFFERENT tree than the commit named by its own reported
  headSha — dormant today under this repo's merge-commit-only +
  `enforce_admins` policy, but candidacy must not depend on that external,
  unstated config. Added 3 tests to `promotion-01-run-selection.cs`: a lone
  `pull_request` run at the matching sha excluded (empty result even as the
  only candidate), mixed pull_request+push at the same sha → only push
  survives, and a three-way pull_request+release+push mix → push first,
  release second, pull_request excluded. 11/11 pass (was 8).
- **Fix 2 (MED)** — `LocateRunOutcome` gained a `Detail` (`string?`) field.
  `LocateRunStatus` split `GhUnavailable` into two: `GhUnavailable` (gh could
  not even be launched — `Win32Exception` from a missing binary, unchanged
  "install gh / gh auth login" message) and new `GhFailed` (gh launched and
  ran but exited nonzero — `Detail = stderr.Trim()`, message: "gh run list
  failed — {stderr}. If this is transient (network/rate limit), retry; for
  auth issues run 'gh auth login'."). Mirrors the existing
  `TagPinOutcome`/`AncestorCheckOutcome` GitError precedent of surfacing real
  stderr instead of a generic message.
- **Fix 3 (MED)** — softened the 458-005 "fully closed" overclaim in both
  `source/timewarp-nuru-devcli/readme.md` and this file's D9 note above (see
  the corrected D9 bullet): release mode no longer ships untested local
  builds (genuine improvement, every push is CI-tested for a specific
  commit), but the untagged double-break-glass case can still mix
  CI-tested-but-different-commit packages under one version — full closure
  needs 458-006 tag-first tooling.
- **Fix 4 (LOW)** — `workflow.yml`'s "Upload Artifacts" step now skips in
  release mode (release event, or confirmed break-glass dispatch) to avoid
  re-uploading the just-downloaded set: `if: always() && github.event_name
  != 'release' && !(github.event_name == 'workflow_dispatch' && inputs.mode
  == 'release' && inputs.confirm == 'release')`.
- **Wontfix (finding #5, LOW/INFO, decided by orchestrator)** — partial-download
  leftover state in `artifacts/packages`: self-heals (next attempt clears
  unconditionally) and cannot cause an unsafe push (throw aborts before push).
  No code change.

**Verification (round-1 fixes):**
- `promotion-01-run-selection.cs`: 11/11 passed (3 new pull_request tests).
- `promotion-02/03/04`: unaffected, still passing (re-run to confirm no
  regression from the shared `ci-run-promotion.cs` edits).
- `dotnet build timewarp-nuru.slnx`: 0 warnings, 0 errors (the RCS1112
  chained-`Where` warning surfaced once and was fixed by combining the two
  predicates into one `Where`).
- `dotnet run tests/ci-tests/run-ci-tests.cs`: 0 failed.
- `dotnet run --file tools/dev-cli/dev.cs -- workflow --mode release` from
  dev HEAD: still aborts at Step 1/6 tag-pin Mismatch — gate order intact,
  locate/download/verify never reached.
- Manual `gh` failure-path simulation: ran the exact command shape
  `LocateCiRunAsync` uses with a deliberately bad token —
  `GH_TOKEN=bad_token_xyz gh run list --workflow workflow.yml --commit
  9e7f900dd8660b516c70b8ab76909f90edaa9e86 --status success --json
  databaseId,event,headSha,createdAt` — exit code 1, stderr:
  `HTTP 401: Bad credentials (https://api.github.com/repos/TimeWarpEngineering/timewarp-nuru/actions/workflows/workflow.yml)`
  + `Try authenticating with:  gh auth login -h github.com`. Confirms the new
  `GhFailed` branch renders a real, human-readable, auth-actionable message —
  "Release gate failed: gh run list failed — HTTP 401: Bad credentials
  (...)\nTry authenticating with: gh auth login -h github.com. If this is
  transient (network/rate limit), retry; for auth issues run 'gh auth
  login'." — clearly distinct from the generic "install gh" `GhUnavailable`
  message.
