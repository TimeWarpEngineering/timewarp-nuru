# Round 1 — general
**Date:** 2026-10-19
**Scope reviewed:** branch `task/508-005-served-endpoints-daemon-host-and-forwarding-client` vs `master` (commit `e7a03a6a`). Daemon package, forwarding contract, generator hooks, McpHost `tools/call`, sample, daemon tests, served-app docs. Checked against the original requirements and against `## Goal (implementer restatement)`.

## Summary

`AddServeRoute()` adds a real per-user Unix-socket server, a forwarding client, local-only opt-out, and an MCP `invoke` path that shares that server. The generator forwards after argv matching and type conversion and before behaviors, built-ins stay local, and the in-process server tests plus `daemon-03` cover the happy path (same pid, cwd, stderr, exit code, `--json-args` stdin, MCP). The holes are in how a process claims the socket: a start can unlink a live socket, a start treats any answering process as the right build, a relative `socketPath` resolves differently in the client and the child, and the temp socket directory is trusted when it already exists.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-daemon/nuru-daemon-server.cs:65
- Description: `RunAsync` treats "ping failed" as "the socket file is stale" and deletes it before `Bind`. Ping and bind are not one critical section. Two `--daemon` starts (the CLI and `myapp mcp`, or two terminals) can both observe no listener. The later `File.Delete` unlinks a socket the earlier process has already bound. On Linux that leaves the first server listening on a deleted inode while the second binds the path. New clients then talk to the second process, and the first keeps running. `Should_refuse_a_second_server_on_the_same_socket` only covers a second start whose ping already succeeds. A connect failure while a server is up (full listen backlog of 16, or a transient `SocketException` that `PingAsync` maps to null) takes the same delete path.
- Suggestion: Claim the socket under an exclusive lock file held across the stale-file check, delete, and bind. On `AddressAlreadyInUse`, ping the existing server and return the "already running" result. Delete the socket file only when connect fails because nothing is listening, and only while the lock is held.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-nuru-daemon/nuru-daemon-client.cs:55
- Description: A build mismatch calls `StopAsync` and then forces `status` to null even when the old server is still answering. `StopAsync` returns true after 10 seconds whether or not ping has gone quiet. `Require` and `autoStart` then call `NuruDaemonLauncher.StartAsync`, which returns success for any ping on that path (`nuru-daemon-launcher.cs:56`). `ForwardAsync` sends the `run` or `invoke` request with no second build-id check. A different build that survived `serve --stop`, or that won the start race, still runs the call. The acceptance criterion is that a server from a different build is stopped and is not used.
- Suggestion: After stop and after start, ping again and compare `BuildId` to `NuruDaemonPaths.GetBuildId`. If the survivor's build id differs, fail the `--daemon` / `autoStart` call with an error result. In `Auto` with `autoStart` false, run locally only after the mismatched server has actually stopped answering. Have `StartAsync` treat "a server answered" as success only when that status build id matches.
- Status: open

### Issue 3 — Severity: bug
- File: source/timewarp-nuru-daemon/nuru-daemon-options.cs:56
- Description: A relative `socketPath` is turned into an absolute path with `Path.GetFullPath`, which uses each process's current directory. The client resolves it in the caller's directory. The launcher starts the child with `WorkingDirectory = AppContext.BaseDirectory` (`nuru-daemon-launcher.cs:82`) and puts the client's absolute path in `NURU_DAEMON_SOCKET`. `ResolveSocketPath` ignores that variable whenever `SocketPath` is set, so the child binds `BaseDirectory` + the relative path and the client pings a different path. `--daemon` then waits until `StartTimeout` and reports that the server did not answer. The same split happens for a foreground `serve` when a later CLI call uses another directory. Null and absolute paths agree; the sample and the daemon tests use those.
- Suggestion: Resolve a relative `socketPath` against one stable directory on both sides (the app base directory, or the entry file's directory), so the current directory cannot move the socket. When the launcher has already resolved an absolute path, the child should listen on that path.
- Status: open

### Issue 4 — Severity: bug
- File: source/timewarp-nuru-daemon/nuru-daemon-paths.cs:63
- Description: `EnsureSocketDirectory` returns immediately when the directory exists. It does not check that the directory is owned by the current user, is mode 0700, or is not a symlink. The fallback location is predictable: `<temp>/nuru-<user>` (`GetDefaultSocketDirectory`). On a shared machine without `XDG_RUNTIME_DIR`, another local user can create that directory (or a symlink to a directory they control) before the victim runs. The victim then binds the daemon socket there. The socket node itself is left at the process umask. A peer who can reach the socket can send `run`, `invoke`, and `stop`. The design note says the directory is created mode 0700. That mode is applied only on the create path.
- Suggestion: Create the directory mode 0700, reject an existing directory that is a symlink or has any mode bit outside owner rwx or a different owner, and set the socket node to owner read/write after bind. Keep the `XDG_RUNTIME_DIR/nuru` location when that runtime directory exists and is usable.
- Status: open
