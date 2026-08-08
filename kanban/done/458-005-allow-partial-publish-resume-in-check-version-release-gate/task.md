# Allow partial-publish resume in check-version release gate

Parent: 458 (finding F6 in `458-*/review/findings.md`).

## Description

The push loop pushes packages sequentially with `--skip-duplicate`, so re-running a
failed release should resume idempotently. But `check-version` (nuget-search) fails
when **any** package already has the version, so the re-run aborts at step 1. The
recovery path for a mid-push failure is manual pushes outside the pipeline — which
is how out-of-band artifacts happen (NuGet has `3.0.0-beta.70` with no git tag).

Target (convention.md rule 6): three-state gate —

- none published → proceed;
- **all** published → abort ("already released");
- **some** published → proceed with a loud resume warning; `--skip-duplicate`
  makes the re-push safe.

## Notes

### Implementation plan (Phase 2, 2026-08-08)

1. New pure classifier in DevCli content (testable, same pattern as
   ci-mode/tag-assertion): `PublishState Classify(int totalPackages,
   int alreadyPublishedCount)` → `None | Partial | All` in
   `services/publish-state.cs` (with guard: total <= 0 → error/None with
   message handled by caller; published > total impossible by construction).
2. `check-version-command.cs` `HandleNuGetSearchAsync`: replace
   `alreadyPublished.Count == 0` boolean with the classifier:
   - None → existing "safe to release" success (exit 0).
   - All → existing abort ("already released", exit 1).
   - Partial → **exit 0** with loud warning block: list already-published
     packages and missing packages by name; state "partial publish detected —
     this release run will resume the push; --skip-duplicate makes re-pushing
     published packages a no-op."
3. Single-package repos: 1/1 published == All → abort (test).
   Packages with zero published versions count as not-published (existing
   behavior, preserved).
4. Tests `tests/timewarp-nuru-tests/devcli/check-version-03-publish-state.cs`:
   classifier matrix — (5,0)→None, (5,5)→All, (5,3)→Partial, (1,0)→None,
   (1,1)→All, boundary (0,0) behavior documented.
5. Test props: add publish-state.cs include to both props files.
6. DevCli readme: check-version section documents the three-state gate;
   Migration Notes entry (behavior change: partial no longer aborts).
7. Release pipeline needs no change (partial → exit 0 → pipeline proceeds;
   push already uses --skip-duplicate).
8. Verify: cache-clear, new tests, build, full CI, manual
   `dev check-version` smoke (current repo state: all 5 packages have
   beta.71 → expect All → abort message unchanged).

### Known residual (accepted)

Round-1 review finding #3 (untagged double break-glass): two different commits under one
never-bumped, never-tagged version leaves the release-gate tag-pin check (added in round-1
fix batch, `tools/dev-cli/endpoints/workflow-command.cs`) nothing to pin against — it only
enforces same-commit resume once a tag for the version exists. Full closure arrives once
releases are always cut by tooling that tags first (458-006) or by build-once/promote
(458-002); until then, this narrow case relies on operator discipline. Documented in the
DevCli readme migration-notes tag-pin paragraph too.

## Checklist

- [x] Three-state gate via pure `PublishStateClassifier` (None/Partial/All; fail-loud guards) replacing the boolean
- [x] Partial state: exit 0, loud warning listing published + missing packages, honest `--skip-duplicate` byte-safety wording
- [x] All-published state: abort behavior and message unchanged (verified live against beta.71)
- [x] Zero-package inputs (whitespace-only AND delimiter-only) route to the friendly error via `ParsePackageList` (review HIGH fix)
- [x] **Tag-pin release gate** (review MED fix, beyond original scope): when tag `v{version}` exists, release-mode HEAD must be at that tag's commit — blocks mixed-commit break-glass resumes
- [x] Tests: 15 classifier/parse tests + 11 tag-assertion (from 458-003) + endpoint-level zero-package regression test (standalone-only via whole-file JARIBU_MULTI guard)
- [ ] Releasing guide (458-008): document the resume flow for a failed release run — deferred to 458-008 as planned

## Results

Implemented in commits `30dce371` (three-state gate), `35256b49` (round-1 review
fixes: ParsePackageList guard, honest wording, tag-pin gate), `2f26dda7`
(round-2: endpoint regression test, residual docs).

- **check-version nuget-search is now a three-state gate:** None → safe;
  All → abort (unchanged); Partial → exit 0 with a resume warning naming
  published and missing packages and stating the byte-safety condition (same
  commit as the earlier partial push).
- **Tag-pin gate added to the release pipeline** (workflow-command.cs, between
  tag assertion and ancestor check): if tag `v{propsVersion}` exists, HEAD
  must be at its commit (NoTag/Match/Mismatch/GitError outcomes, fail-closed
  default). Demonstrated live: break-glass from dev with beta.71 tagged
  elsewhere aborts with both short SHAs named.
- **Review (Phase 4b):** 3 rounds, effort 1, sonnet reviewer. 5 findings:
  1 HIGH (crash — live-reproduced, fixed, fault-injection-verified test),
  2 MED (fixed), 1 LOW (fixed after reviewer PoC), 1 INFO wontfix.
  Disposition: **accepted-exceptions** (`review/disposition.md`) — wontfix +
  documented narrow residual (untagged double-break-glass; closes via
  458-006/458-002) + one pre-existing footnote on shipped-warning wording.

### How to validate

Smoke:
1. `dotnet run tests/timewarp-nuru-tests/devcli/check-version-03-publish-state.cs`
   → 15/15. `dotnet run tests/timewarp-nuru-tests/devcli/check-version-04-endpoint-zero-package.cs`
   → 1/1.
2. `dotnet run --file tools/dev-cli/dev.cs -- check-version --package ","`
   → Expect friendly "no packages specified" error, exit 1, NO stack trace.
3. `dotnet run --file tools/dev-cli/dev.cs -- check-version` (all 5 packages
   published at current props version) → Expect All-published abort, exit 1.
4. `dotnet run --file tools/dev-cli/dev.cs -- workflow --mode release` from a
   commit not at the current version's tag → Expect tag-pin Mismatch abort
   naming both short SHAs.

Automated gate: `ganda runfile cache --clear && dotnet run tests/ci-tests/run-ci-tests.cs`
→ Expect 0 failed (last: 1446 total / 1439 passed / 7 skipped).

Depends on / not in scope: releasing-guide docs (458-008); full byte-identity
closure (458-002 promotion / 458-006 tag-first tooling).
