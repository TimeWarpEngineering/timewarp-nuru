# Round 1 — general
**Date:** 2026-10-21
**Scope reviewed:** commit `583cbbbf` vs `master` (`5f916790`). Daemon client, launcher, options, paths, protocol, server, package readme, `documentation/user/features/served-apps.md`, `tests/timewarp-nuru-daemon-tests/daemon-01-server.cs`, and the Unreleased daemon notes in `changelog.md`.

## Summary

The change follows the recommended model: the default socket name carries an 8-hex hash of the build id, so two builds of one app each keep a warm server, and `serve --stop` plus the idle timeout are how a server goes away. On a fixed socket (`socketPath` or `NURU_DAEMON_SOCKET`), `ForwardAsync` no longer calls `StopAsync`. Auto runs locally; `--daemon` and `autoStart` return one line that names both builds and `serve --stop`. Ping carries `entry` and `written`. `serve --status` prints them and, on the default socket, lists other servers of the same app. `dotnet run tests/timewarp-nuru-daemon-tests/daemon-01-server.cs` reported Total 19, Passed 19, including the in-flight hold and the idle-timeout case. The implementer restatement matches the original task (see Learnings on `task.md`). The release notes still describe the old stop-and-replace behavior.

## Issues

### Issue 1 — Severity: suggestion
- File: changelog.md:18
- Description: The Unreleased **Served apps** bullet still says the socket is `$XDG_RUNTIME_DIR/nuru/<app>.sock` and that "A server from another build of the app is stopped, not used". That is the behavior this change removes. `documentation/user/features/served-apps.md` and `source/timewarp-nuru-daemon/readme.md` describe socket-per-build and refuse-on-a-shared-socket. A release cut from Unreleased would document the bug as the feature, including the old default path.
- Suggestion: Add a Changed or Fixed bullet for 508-017: the default socket is `<app>-<8 hex>.sock`, a client never stops another build's server, a shared socket leaves that server running (Auto local, `--daemon` / `autoStart` one-line error), and the idle timeout still stops a server nobody calls. Revise the 508-005 sentence so it does not still say a different build is stopped.
- Status: open
