# Round 1 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: changelog.md:18
- Description: The Unreleased **Served apps** bullet still says the socket is `$XDG_RUNTIME_DIR/nuru/<app>.sock` and that "A server from another build of the app is stopped, not used". That is the behavior this change removes. `documentation/user/features/served-apps.md` and `source/timewarp-nuru-daemon/readme.md` describe socket-per-build and refuse-on-a-shared-socket. A release cut from Unreleased would document the bug as the feature, including the old default path.
- Suggestion: Add a Changed or Fixed bullet for 508-017: the default socket is `<app>-<8 hex>.sock`, a client never stops another build's server, a shared socket leaves that server running (Auto local, `--daemon` / `autoStart` one-line error), and the idle timeout still stops a server nobody calls. Revise the 508-005 sentence so it does not still say a different build is stopped.
- Source: general
- Disposition notes: fixed in the implement-fix-2 pass (2026-10-21). `changelog.md` Unreleased gains a Fixed **Served apps** bullet for 508-017 (per-build `<app>-<8 hex>.sock`, a client never stops another build's server, shared socket: Auto local, `--daemon` / `autoStart` one-line error, status shows the build, idle timeout still stops unused servers). The 508-005 Added bullet now gives the per-build default path and says a server of another build is never used and never stopped by the client.

## Duplicates / conflicts

- One finding. Nothing to collapse.
