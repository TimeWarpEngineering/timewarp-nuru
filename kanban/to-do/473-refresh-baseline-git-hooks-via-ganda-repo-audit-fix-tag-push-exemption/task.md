# Refresh baseline git hooks via ganda repo audit --fix (tag-push exemption)

## Description

ganda 1.0.0-beta.33 (ganda task 307) fixes the baseline pre-push hook so tag-only pushes from master are
allowed. Without it, `dev release` fails: the hook refuses the release tag push while HEAD is master.
The audit now compares installed hooks with the current templates, so `ganda repo audit --fix` replaces
stale copies.

## Requirements

- Confirm `ganda --version` is 1.0.0-beta.33 or later.
- Run `ganda repo audit --fix` in the claim worktree. Commit every change it makes and push.
- Re-run `ganda repo audit`; it must exit 0.
- Confirm `.githooks/pre-push.cs` now exempts `refs/tags/*` (look for `IsTagDest`).
- No product code changes.

## Checklist

- [x] audit --fix applied and committed
- [x] audit clean
- [x] pre-push exempts tags

## Notes

- Implementer: **commit and push your changes before reporting done.**

## Results

- `ganda --version`: `1.0.0-beta.33+4e1dc6d5df9838bb2b55c43b247c4d45e1f07215`
- `ganda repo audit --fix` deployed `.githooks/pre-push.cs` (memsearch-scaffold); no product code touched
- Committed and pushed: `36a7cbd0` — `fix(hooks): refresh pre-push for tag-only release pushes`
- Re-ran `ganda repo audit`: exit 0, 28 passed / 0 failed
- `.githooks/pre-push.cs` includes `IsTagDest` / `IsExemptDest` so tag-only pushes from master/main are allowed; mixed tag+branch batches still refused

### How to validate

- **Smoke:** `ganda --version` ≥ 1.0.0-beta.33; `rg IsTagDest .githooks/pre-push.cs`; `ganda repo audit` (expect exit 0)
- **Expect:** pre-push exempts `refs/tags/*` via `IsTagDest`; audit clean; only hook template refresh on the branch (no product diffs)
