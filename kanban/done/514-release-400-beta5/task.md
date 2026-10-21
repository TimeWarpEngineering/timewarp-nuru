# Release 4.0.0-beta.5

## Description

Bump `<Version>` in `source/Directory.Build.props` from 4.0.0-beta.4 to 4.0.0-beta.5, merge,
wait for green master CI, then cut the release with `dev release` from the master worktree
(tw-release convention).

Ships 508-007 (#340), 508-013 (#341), 508-018 (#342) and 508-017 (#343), the Roslynk asks.

Release-note callouts:
- NURU056 now also fires when a singleton reaches a session- or command-scoped service through a Scoped service (508-007).
- A referenced contracts assembly needs `[assembly: TimeWarp.Mediator.MediatorAssembly]` and opts in with `[assembly: NuruEndpointAssembly(typeof(...))]` (508-013).
- Default daemon socket is now per build (`<app>-<8 hex>.sock`); a client never stops another build's server (508-017).

## Checklist

- [x] Bump version to 4.0.0-beta.5
- [ ] Bump PR merged, master CI green
- [ ] `dev release --dry-run` clean
- [ ] `dev release` cut; release pipeline pushed all packages

## Session

- Created: 2026-10-21
- Release steps run from the master worktree after this PR merges; the outcome is reported in chat
  and the release memory (the worktree is collected at merge).

## Notes
