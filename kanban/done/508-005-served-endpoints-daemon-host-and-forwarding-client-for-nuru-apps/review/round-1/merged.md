# Round 1 — merged findings
**Date:** 2026-10-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 4 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-daemon/nuru-daemon-server.cs:65
- Description: `RunAsync` deletes the socket file when ping does not answer, then binds. That check and the bind are not atomic. A second start can unlink a live server's socket and bind the path, so probe-or-start talks to a different process while the first server is still running. A transient connect failure against a live server takes the same delete path. The refuse-second-server test covers only a ping that already succeeds.
- Suggestion: Hold an exclusive lock across the stale check, delete, and bind. On address-in-use, ping and adopt the server that is actually listening. Delete a socket file only when nothing is listening and the lock is held.
- Source: general
- Disposition notes: Fixed (implement-fix-2). `RunAsync` takes an exclusive lock on `<socket>.lock` (FileShare.None, flock on Unix) before the stale check and holds it until after it deletes its socket; only the holder deletes or binds the path. Lock busy: report the running server if it answers, else fail after `StartTimeout` without touching the socket. Bind `AddressInUse`: ping and report. The listener closes as soon as the server stops. Test: `Should_not_touch_the_socket_while_another_server_holds_the_lock`.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-daemon/nuru-daemon-client.cs:55
- Description: After a build mismatch, `ForwardAsync` clears the probed status even when `StopAsync` leaves the old server answering (`StopAsync` returns after 10 seconds either way). `NuruDaemonLauncher.StartAsync` (`nuru-daemon-launcher.cs:56`) treats any later ping as success. The call is sent with no second build-id check, so a different build can still run it. The acceptance criterion says a server from a different build is stopped and is not used.
- Suggestion: Re-ping after stop and after start. Use the server only when `BuildId` matches `NuruDaemonPaths.GetBuildId`. If `--daemon` or `autoStart` cannot replace a mismatched server, return an error result.
- Source: general
- Disposition notes: Fixed (implement-fix-2). After stopping a mismatched server the client pings again; if a different build still answers, Auto runs locally and `--daemon`/`autoStart` return an error. The launcher waits for a ping that reports the client build id. Each run/invoke carries `build`, and the server refuses another build. The server build id is fixed at server start. Ping has a 5 s timeout. Tests: `Should_refuse_a_call_that_names_another_build`, `Should_stop_and_not_use_a_server_from_another_build`.

### M3 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-daemon/nuru-daemon-options.cs:56
- Description: A relative `socketPath` is resolved with `Path.GetFullPath` in each process's current directory. The launcher starts the child in `AppContext.BaseDirectory` and passes the client's absolute path in `NURU_DAEMON_SOCKET`, but a set `SocketPath` wins over that variable. The child binds a different path than the client pings, so `--daemon` times out. Foreground `serve` splits the same way when a later call uses another directory. Null and absolute paths agree.
- Suggestion: Resolve a relative `socketPath` against one stable directory on both sides. When the launcher has resolved an absolute path, the child should listen on that path.
- Source: general
- Disposition notes: Fixed (implement-fix-2). A relative `socketPath` or `NURU_DAEMON_SOCKET` resolves under `NuruDaemonPaths.GetDefaultSocketDirectory()`, never the cwd. The launcher passes its absolute path in `NURU_DAEMON_LAUNCH_SOCKET`, which wins in the child and is cleared after it is read. The launcher no longer sets `NURU_DAEMON_SOCKET`, so a handler child process does not inherit it. Test: `Should_resolve_a_relative_socket_path_under_the_socket_directory`, plus a manual smoke from two directories.

### M4 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-daemon/nuru-daemon-paths.cs:63
- Description: `EnsureSocketDirectory` does not check owner, mode, or symlink when the directory already exists, and the socket node stays at the process umask. The temp fallback `<temp>/nuru-<user>` is predictable. Another local user can pre-create that directory and then send `run`, `invoke`, and `stop` to the victim's server. Mode 0700 is applied only when this process creates the directory.
- Suggestion: Require an owner-only directory that is not a symlink, create it 0700, and set the socket node to owner read/write after bind.
- Source: general
- Disposition notes: Fixed (implement-fix-2). `EnsureSocketDirectory` creates a missing directory 0700. It refuses an existing directory that is a symlink or has any group/other bits, and tightens the default directory to 0700 when the user owns it. The client (`CheckSocketDirectory` in connect, `ForwardAsync`, `serve --status`) refuses the same directories. The socket file is chmod 0600 after bind. Test: `Should_require_an_owner_only_socket_directory`, and the ping test checks 0600.

## Duplicates / conflicts

- One reviewer (general). No overlaps to collapse. M1 and M2 both sit on probe-or-start; they stay separate because fixing the unlink does not add the post-start build-id check.
