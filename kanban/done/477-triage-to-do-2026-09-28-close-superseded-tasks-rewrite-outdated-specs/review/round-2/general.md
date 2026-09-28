# Round 2 — general
**Date:** 2026-09-28
**Scope reviewed:** post-fix wording on 456 and 069 (M1, M2). No other product delta.

## Summary

M1 now points at the requirements above the triage note, and those requirements name only the NuGet distance (no git-tag strategy, no "both strategies" test). M2 names `AddRepl()` and `AddRepl(Action<ReplOptions>)` and marks `AddReplOptions` obsolete, matching `nuru-app-builder.routes.cs`. No new issues on the fix delta.

## Issues

### M1 — Severity: nit
- File: kanban/to-do/456-check-version-warn-when-source-is-multiple-versions-ahead-of-last-release.md:46
- Description: Prior note pointed below the update for requirements that sit above it.
- Suggestion: (implemented) say the requirements above match the single NuGet methodology.
- Status: fixed

### M2 — Severity: nit
- File: kanban/to-do/069-global-user-key-binding-profiles.md:15
- Description: Prior note listed obsolete `AddReplOptions` as a current enablement API.
- Suggestion: (implemented) name `AddRepl()` / `AddRepl(Action<ReplOptions>)` and mark `AddReplOptions` obsolete.
- Status: fixed
