# Round 1 — merged findings
**Date:** 2026-10-13
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: documentation/user/guides/migrating-to-3.0.md:22 (also task.md Results, "### Docs updated")
- Description: The guide says betas "through 3.0.0-beta.79" use Mediator 14.0.0-beta.4 and later builds use stable 14.0.0. But the
  published `3.0.0-beta.80` nuspec on nuget.org depends on `TimeWarp.Mediator.Contracts` / `.Generators` `14.0.0-beta.4`.
- Suggestion: Say "through 3.0.0-beta.80" in the guide and in the Results bullet. The first beta after beta.80 carries stable 14.0.0.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/release-notes.md:5-7
- Description: The pre-paste sentence is an unwrapped long line. The header claim "every bullet verified against beta.79"
  no longer covers the new Mediator bullet.
- Suggestion: Rewrap. Say the Mediator bullet is verified against the 14.0.0 pin.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
