# Round 2 — general
**Date:** 2026-10-14
**Scope reviewed:** fix pass 06acf71a (round-1 M1/M2), plus a re-check of the full branch diff `master...HEAD`

## Summary

The fix pass removes the claim that every 3.0 beta through beta.80 used Amuru 2.0.0-beta.2 (M1). It also adds the
note that Amuru is a new dependency for 2.x users (M2), which the 2.0.0 and 2.1.0-beta.32 nuspecs confirm. The
pin bump itself (`Directory.Packages.props` 2.0.0) is unchanged and correct.

The replacement sentence "earlier 3.0 betas depend on Amuru 1.x" is still wrong. I checked every published 3.0 beta
nuspec on nuget.org:

- beta.2–57: no Amuru dependency
- beta.58–71: `1.0.0-beta.17` through `1.0.0-beta.32`
- beta.72–76: `1.0.0`
- beta.79: `1.1.1`
- beta.80: `2.0.0-beta.2`

The parenthetical "REPL clipboard and the generated `--capabilities` search call" matches the source:
`timewarp-nuru/global-usings.cs:28`, `repl-console-reader.clipboard.cs`, and `capabilities-emitter.cs:67-70`.

## Issues

### Issue 1 — Severity: bug
- File: changelog.md:13; documentation/user/guides/migrating-to-3.0.md:51-52; kanban/to-do/487-*/release-notes.md:54-55;
  kanban/to-do/487-*/task.md:66; 502 task.md:38, 88, 129
- Description: "Earlier 3.0 betas depend on Amuru 1.x" is false for beta.2–57, which have no Amuru dependency. Round 1's
  suggested wording caused this, so it is a regression from M1's fix.
- Suggestion: "3.0.0-beta.58 through beta.79 depend on Amuru 1.x; earlier betas have no Amuru dependency". Or drop the
  beta history from the user-facing guide and the release notes.
- Status: open

### Issue 2 — Severity: nit
- File: documentation/user/guides/migrating-to-3.0.md:53
- Description: The line is 130 characters. The rest of the paragraph and the guide wrap at about 80 columns.
- Suggestion: Rewrap it in the same edit.
- Status: open
