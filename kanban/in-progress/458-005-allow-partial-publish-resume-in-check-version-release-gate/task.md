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

## Checklist

- [ ] `HandleNuGetSearchAsync`: distinguish none / some / all published instead of `alreadyPublished.Count == 0`
- [ ] Partial state: exit 0, print which packages are missing and that this run resumes the push
- [ ] All-published state: keep current abort behavior and message
- [ ] Tests: all three states, including single-package repos (some == all)
- [ ] Releasing guide (458-008): document the resume flow for a failed release run
