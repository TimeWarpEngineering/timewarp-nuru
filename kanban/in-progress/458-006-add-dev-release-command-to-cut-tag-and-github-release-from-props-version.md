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

- [ ] New DevCli content endpoint `release-command.cs` (ships to all consumers)
- [ ] Reads props version; refuses if working tree dirty, not on master, or master not in sync with origin
- [ ] Runs check-version gate first; refuses on already-fully-published
- [ ] Creates annotated tag `v{Version}` and GitHub Release (`gh release create v{Version} --title v{Version} --generate-notes`)
- [ ] `--dry-run` flag printing what would be created
- [ ] Tests for the guard conditions (dirty tree, wrong branch, existing tag)
- [ ] Releasing guide (458-008): `dev release` becomes the documented way to cut a release
