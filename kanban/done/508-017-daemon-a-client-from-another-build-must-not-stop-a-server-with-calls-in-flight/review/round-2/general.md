# Round 2 — general
**Date:** 2026-10-21
**Scope reviewed:** product delta `9eb1ed8a..8feed8a5` (`changelog.md` Unreleased only). Re-checked M1 against `NuruDaemonClient.ForwardAsync`, `NuruDaemonPaths.GetDefaultSocketPath` / `ShortHash`, and `NuruDaemonServer.StatusAsync`.

## Summary

The fix pass changes only the Unreleased notes. The 508-005 **Served apps** bullet now names `$XDG_RUNTIME_DIR/nuru/<app>-<8 hex>.sock` and says a server from another build is never used and never stopped by the client. A new Fixed **Served apps** bullet (508-017) states the per-build socket (8 hex digits of SHA-256 of the build id), the shared-socket rule (Auto runs locally; `--daemon` or `autoStart` fails with one line naming both builds and `serve --stop`), ping entry path and write time, `serve --status` listing other builds on the default socket, and the idle timeout. That matches the client (a different `BuildId` returns the one-line error or null, and does not call `StopAsync`), `BuildHashLength` 8 with SHA-256, and status listing only when `UsesDefaultSocket`. No new defect on this delta.

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: changelog.md:18 and changelog.md:28
- Description: Round 1: the Unreleased **Served apps** bullet still said `<app>.sock` and that another build's server is stopped. The fix replaces that sentence and adds the Fixed bullet the suggestion asked for.
- Suggestion: none remaining.
- Status: fixed

## Issues

<!-- No new findings. -->
