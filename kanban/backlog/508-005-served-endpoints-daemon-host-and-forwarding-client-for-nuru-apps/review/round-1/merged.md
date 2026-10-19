# Round 1 — merged findings
**Date:** 2026-10-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 4 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-daemon/nuru-daemon-server.cs:65
- Description: `RunAsync` deletes the socket file when ping does not answer, then binds. That check and the bind are not atomic. A second start can unlink a live server's socket and bind the path, so probe-or-start talks to a different process while the first server is still running. A transient connect failure against a live server takes the same delete path. The refuse-second-server test covers only a ping that already succeeds.
- Suggestion: Hold an exclusive lock across the stale check, delete, and bind. On address-in-use, ping and adopt the server that is actually listening. Delete a socket file only when nothing is listening and the lock is held.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/timewarp-nuru-daemon/nuru-daemon-client.cs:55
- Description: After a build mismatch, `ForwardAsync` clears the probed status even when `StopAsync` leaves the old server answering (`StopAsync` returns after 10 seconds either way). `NuruDaemonLauncher.StartAsync` (`nuru-daemon-launcher.cs:56`) treats any later ping as success. The call is sent with no second build-id check, so a different build can still run it. The acceptance criterion says a server from a different build is stopped and is not used.
- Suggestion: Re-ping after stop and after start. Use the server only when `BuildId` matches `NuruDaemonPaths.GetBuildId`. If `--daemon` or `autoStart` cannot replace a mismatched server, return an error result.
- Source: general
- Disposition notes:

### M3 — Severity: bug — Status: open
- File: source/timewarp-nuru-daemon/nuru-daemon-options.cs:56
- Description: A relative `socketPath` is resolved with `Path.GetFullPath` in each process's current directory. The launcher starts the child in `AppContext.BaseDirectory` and passes the client's absolute path in `NURU_DAEMON_SOCKET`, but a set `SocketPath` wins over that variable. The child binds a different path than the client pings, so `--daemon` times out. Foreground `serve` splits the same way when a later call uses another directory. Null and absolute paths agree.
- Suggestion: Resolve a relative `socketPath` against one stable directory on both sides. When the launcher has resolved an absolute path, the child should listen on that path.
- Source: general
- Disposition notes:

### M4 — Severity: bug — Status: open
- File: source/timewarp-nuru-daemon/nuru-daemon-paths.cs:63
- Description: `EnsureSocketDirectory` does not check owner, mode, or symlink when the directory already exists, and the socket node stays at the process umask. The temp fallback `<temp>/nuru-<user>` is predictable. Another local user can pre-create that directory and then send `run`, `invoke`, and `stop` to the victim's server. Mode 0700 is applied only when this process creates the directory.
- Suggestion: Require an owner-only directory that is not a symlink, create it 0700, and set the socket node to owner read/write after bind.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- One reviewer (general). No overlaps to collapse. M1 and M2 both sit on probe-or-start; they stay separate because fixing the unlink does not add the post-start build-id check.
