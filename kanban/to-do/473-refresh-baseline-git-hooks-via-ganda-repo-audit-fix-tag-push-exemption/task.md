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

- [ ] audit --fix applied and committed
- [ ] audit clean
- [ ] pre-push exempts tags

## Notes

- Implementer: **commit and push your changes before reporting done.**
