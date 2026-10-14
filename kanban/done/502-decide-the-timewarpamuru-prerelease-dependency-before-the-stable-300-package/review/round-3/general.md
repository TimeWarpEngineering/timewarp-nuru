# Round 3 — general
**Date:** 2026-10-14
**Scope reviewed:** fix pass b3ab7c4c (M3, M4) plus the full `master...HEAD` diff

## Summary

The fix pass removes the beta-by-beta Amuru history from the user-facing docs. `changelog.md:13`, the migration
guide, 487 `release-notes.md`, and 487 `task.md:66` now state the stable 2.0.0 dependency, and the changelog, the guide,
and 487 `task.md:66` add that 3.0.0-beta.80 was the one beta on the 2.0.0-beta.2 prerelease. The nuget.org nuspecs
confirm this: beta.79 depends on Amuru `1.1.1`, beta.80 on `2.0.0-beta.2`, and beta.80 is the latest published Nuru
version, so no earlier beta used a 2.0 prerelease. The wrong "earlier 3.0 betas depend on Amuru 1.x" sentence is gone
from all locations. The full per-beta history stays in the 502 Results and matches round 2's nuspec check. The guide's
Amuru section now wraps at 76–81 columns, which matches the rest of the guide. `Directory.Packages.props:26-27` pins both
packages at `2.0.0`. No `2.0.0-beta.2` Amuru mention remains outside `kanban/` except the intended historical clause.

## Issues

None.

## Resolved prior

- M3 (bug): fixed by b3ab7c4c.
- M4 (nit): fixed by b3ab7c4c.
