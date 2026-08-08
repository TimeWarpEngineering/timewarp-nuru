# Add dev release command to cut tag and GitHub Release from props version

Parent: 458 (finding F7 in `458-*/review/findings.md`).

## Description

Today a release requires two independent human acts that can disagree: merge the
props version bump, then hand-type a tag/Release on GitHub. That dual entry is the
root cause of tag↔package divergence (beta.69 tagged but never on NuGet; beta.70 on
NuGet but never tagged) and the timewarp-architecture burned-version incident
(task 456).

Target (convention.md rule 5): the tag is **derived, not typed**. A `dev release`
DevCli endpoint reads `<Version>` from `source/Directory.Build.props`, runs the
check-version gate, and creates tag `v{Version}` plus the GitHub Release on the
master head commit (via `gh release create`). Publishing then proceeds through the
existing `release: published` pipeline. Humans type a version exactly once — in the
props bump PR. The 458-003 tag==props assertion stays as defense-in-depth.

## Notes

### Implementation plan (Phase 2, 2026-08-08) — key decisions

- **D1 guard order (fail-fast, precise messages):** props version readable →
  gh available/authed → tree clean → on master → synced with origin (fetch
  then ahead/behind counts, distinct messages) → tag v{X} absent local AND
  remote → check-version 3-state (None proceed; All refuse "bump"; **Partial
  refuse** — no tag exists at this point [guard 6], so partial = untagged
  break-glass mess; new tag here would mint the mixed-commit hazard; resume
  from original commit or bump) → **NEW guard 8: successful CI run exists at
  head sha** (else the Release event would fail at locate-run; reuses
  CiRunSummary/OrderCandidateRuns — no new JSON context entries).
- **D2:** endpoint `release-command.cs` in DevCli content (ships to all
  consumers — convention rule 5; DI deps already registered, no dev.cs
  change); pure per-guard classifiers in `release-guard.cs`
  (GuardVerdict; CheckWorkingTree/Branch/Sync/TagAvailability/PublishState).
  workflow-command's private helpers can't be reused (repo-local) — small
  shell sequences re-implemented in the handler, shared pure pieces reused.
- **D3:** annotated local tag at the verified sha → push tag → `gh release
  create v{X} --title v{X} --generate-notes --verify-tag`. Not
  `--target`: gh-created tags are lightweight (verified: existing
  v3.0.0-beta.71 is lightweight) and branch targets resolve server-side
  (race). Push failure → delete local tag + error; release-create failure
  after push → print exact recovery command, no unwinding.
- **D4:** `--dry-run` runs all guards (read-only; fetch is remote-state
  read), prints version/tag/sha/guard checklist/exact commands; exit mirrors
  guard outcome. No notes options — --generate-notes is the convention.
- **D5:** closes the 458-005/458-002 residual for tooling-cut releases (tag
  precedes any publish → tag-pin enforces same-commit resume). No
  endpoint-level test (every guard needs process execution); pure guard
  matrix tests + live dry-run simulations.

## Checklist

- [x] New DevCli content endpoint `release-command.cs` (ships to all consumers via glob; deps already DI-registered)
- [x] Reads props version; refuses on dirty tree, wrong branch, out-of-sync master (distinct ahead/behind/diverged messages)
- [x] check-version 3-state gate: All refuses ("bump"); Partial-untagged refuses (would mint the mixed-commit hazard); None proceeds. PLUS guards beyond spec: gh availability/auth, tag-absent local AND remote, **green CI run exists at head sha** (Release event can't fail at locate-run)
- [x] Annotated tag at the verified sha → push → `gh release create --title --generate-notes --verify-tag`; push-fail deletes local tag with honest cleanup reporting; create-fail prints exact recovery command
- [x] `--dry-run` runs all guards read-only, prints version/tag/sha/guards/exact commands, exit mirrors outcome
- [x] Tests: 22 pure guard-matrix tests (tree/branch/sync/tag/publish-state incl. throw cases); live dry-run simulations (branch guard + tree guard fired correctly on this repo)
- [ ] Releasing guide (458-008): `dev release` becomes the documented way to cut a release — deferred to 458-008 as planned

## Results

Implemented in commits `d7cad702` (endpoint + guards) and `8421a767` (review
fixes). `dev release` is now the single human act of cutting a release:
humans type a version exactly once (the props bump PR); the tag and GitHub
Release derive mechanically from props at a guard-verified master commit,
and publishing flows through the existing release-event promotion pipeline.

- **Eight fail-fast guards** in order: props readable → gh authed → tree
  clean → on master → synced with origin → tag absent (local+remote) →
  check-version 3-state → successful CI run exists at head sha.
- **Residual closure:** for tooling-cut releases the tag now precedes any
  publish, so the tag-pin gate (458-005) enforces same-commit resume —
  the untagged double-break-glass hazard cannot arise via `dev release`.
- **Review (Phase 4b):** 2 rounds, effort 1. 1 MED + 1 LOW resolved, 1 INFO
  wontfix (deliberate thin-orchestration duplication; sanity-checked).
  Disposition: **accepted-exceptions** (`review/disposition.md`) — including
  the explicit note that release:published-fires-immediately rests on
  documented gh semantics, confirmed live by the first real release.
- Reviewer verified: dry-run purity (all mutating calls after the gate;
  live dry-run mutated nothing), NURU050 fail-loud for consumers missing DI
  registrations (reproduced), headSha pinning, guard-8 inheritance of the
  pull_request exclusion.

### How to validate

Smoke:
1. `dotnet run tests/timewarp-nuru-tests/devcli/release-01-guard-matrix.cs`
   → 22/22.
2. `dotnet run --file tools/dev-cli/dev.cs -- release --dry-run` from this
   dev-branch worktree → refusal at the branch guard (or tree guard if
   dirty), exit 1, zero mutation (`git status --short` unchanged).
3. `dotnet run --file tools/dev-cli/dev.cs -- --help` → `release` route listed.

Automated gate: `ganda runfile cache --clear && dotnet run tests/ci-tests/run-ci-tests.cs`
→ 0 failed (last: 1520 total / 1513 passed / 7 skipped).

Depends on / not in scope: releasing guide (458-008); first real
`dev release` run (next version bump) is the live end-to-end confirmation —
expect: all guards green on a clean synced master, tag + Release created,
release event runs the promotion pipeline.
