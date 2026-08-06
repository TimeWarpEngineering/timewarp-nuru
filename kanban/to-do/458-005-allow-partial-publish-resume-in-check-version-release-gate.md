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

## Checklist

- [ ] `HandleNuGetSearchAsync`: distinguish none / some / all published instead of `alreadyPublished.Count == 0`
- [ ] Partial state: exit 0, print which packages are missing and that this run resumes the push
- [ ] All-published state: keep current abort behavior and message
- [ ] Tests: all three states, including single-package repos (some == all)
- [ ] Releasing guide (458-008): document the resume flow for a failed release run
