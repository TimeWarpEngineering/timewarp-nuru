# Round 2 — general
**Date:** 2026-10-19
**Scope reviewed:** fix commit `1e474f0d` since round-1 base `9c80497e`. Daemon client, launcher, options, paths, protocol, server, served-app docs, changelog, and `daemon-01` / `daemon-02` / `daemon-03` test updates. Checked against the original requirements and against `## Goal (implementer restatement)`. The restatement mismatch recorded under `## Learnings` is unchanged.

## Summary

The fix pass closes the four round-1 bugs. `serve` holds an exclusive lock across the stale-socket check, delete, and bind. A mismatched build is stopped and then pinged again, a started server is used only when its build id matches, and each forwarded `run` / `invoke` carries the build id for the server to refuse. A relative socket path resolves under the per-user socket directory, and a launched server listens on the client's absolute path. The socket directory must be owner-only, and the socket file is mode 0600. `daemon-01-server.cs` passed 15/15 here, including the five new cases. No new defect on this delta.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-daemon/nuru-daemon-server.cs:99
- Description: Re-checked. `RunAsync` calls `AcquireServerLockAsync` before ping, delete, or bind, and holds that `FileStream` (`FileShare.None`) until after `TryDelete`. A busy lock pings and either reports the running server or fails after `StartTimeout` without deleting the path. `AddressAlreadyInUse` pings and reports; it does not unlink the winner. Same-process `FileShare.None` throws `IOException` when the file is already open, which is what the wait loop catches. `Should_not_touch_the_socket_while_another_server_holds_the_lock` passed: a regular file left in place while the lock was held, then replaced once the lock was released.
- Suggestion: none
- Status: fixed

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-daemon/nuru-daemon-client.cs:64
- Description: Re-checked. After `StopAsync`, `ForwardAsync` pings again. A different build that still answers is not used: `--daemon` and `autoStart` return an error, and Auto returns null so the call runs locally. `NuruDaemonLauncher.StartAsync` returns success only when a ping's build id equals the client's, and returns an error when a different build answers. `ForwardAsync` sends `build` on `run` and `invoke`. The server stores its build id at start and refuses a call whose `build` differs. The direct `RunAsync` / `InvokeAsync` helpers still omit `build`; the generated CLI and the MCP host go through `ForwardAsync`, which is the path the acceptance criterion names. `Should_refuse_a_call_that_names_another_build` and `Should_stop_and_not_use_a_server_from_another_build` passed.
- Suggestion: none
- Status: fixed

### M3 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-daemon/nuru-daemon-options.cs:71
- Description: Re-checked. `Resolve` uses `Path.GetFullPath(path, NuruDaemonPaths.GetDefaultSocketDirectory())` for a relative override, so the current directory is not part of the path. The launcher puts that absolute path in `NURU_DAEMON_LAUNCH_SOCKET` and does not set `NURU_DAEMON_SOCKET`. `TakeLaunchedSocketPath` uses a fully qualified launch path and then clears the variable. `Should_resolve_a_relative_socket_path_under_the_socket_directory` passed from two working directories.
- Suggestion: none
- Status: fixed

### M4 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-daemon/nuru-daemon-paths.cs:74
- Description: Re-checked. A missing directory is created with mode 0700 (the leaf directory only; this was probed). A symlink, or any group or other mode bit, is refused. The default directory is tightened to 0700 when this user can change it. The client refuses the same directories in `TryConnectAsync`, `ForwardAsync`, and `serve --status`, so it does not talk to a socket it will not trust. `RestrictSocketFile` sets the socket to 0600 after bind. The predictable `<temp>/nuru-<user>` pre-create (mode other than 0700, or a symlink) no longer receives the victim's socket. A 0700 directory this process cannot write fails at the lock or bind with an error, rather than binding inside it. `Should_require_an_owner_only_socket_directory` passed, and the ping test checks mode 0600.
- Suggestion: none
- Status: fixed

## Issues

<!-- No new issues. -->
