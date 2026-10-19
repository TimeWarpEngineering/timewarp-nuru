# Round 2 — merged findings
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
- File: source/timewarp-nuru-daemon/nuru-daemon-server.cs:99
- Description: `RunAsync` used to delete the socket when ping failed, then bind, with no lock. A second start could unlink a live server.
- Suggestion: Hold an exclusive lock across the stale check, delete, and bind.
- Source: general
- Disposition notes: Verified in round 2 against `1e474f0d`. The lock is `FileShare.None` on `<socket>.lock`, held from before the stale check until after this server deletes its socket. A busy lock does not touch the path. `daemon-01` `Should_not_touch_the_socket_while_another_server_holds_the_lock` passed (15/15 in this round).

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-daemon/nuru-daemon-client.cs:64
- Description: After a build mismatch, the client cleared status even when the old server still answered, and the launcher treated any ping as success. The call carried no build id.
- Suggestion: Re-ping after stop and after start. Use the server only when the build id matches. Send the build id on the call.
- Source: general
- Disposition notes: Verified in round 2. Re-ping after stop, launcher waits for this build, server build id is fixed at start, and forwarded `run` / `invoke` carry `build` for the server to refuse. `Should_refuse_a_call_that_names_another_build` and `Should_stop_and_not_use_a_server_from_another_build` passed.

### M3 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-daemon/nuru-daemon-options.cs:71
- Description: A relative `socketPath` was resolved with `Path.GetFullPath` in each process's current directory, so the child bound a different path than the client pinged.
- Suggestion: Resolve a relative path against one stable directory, and make the child listen on the path the client resolved.
- Source: general
- Disposition notes: Verified in round 2. Relative paths resolve under `GetDefaultSocketDirectory()`. `NURU_DAEMON_LAUNCH_SOCKET` carries the client's absolute path and wins in the child. `Should_resolve_a_relative_socket_path_under_the_socket_directory` passed.

### M4 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-daemon/nuru-daemon-paths.cs:74
- Description: An existing socket directory was trusted with no owner, mode, or symlink check, and the socket node stayed at the process umask.
- Suggestion: Create the directory mode 0700, reject a symlink or any group/other access, and set the socket to owner read/write.
- Source: general
- Disposition notes: Verified in round 2. Create is 0700, symlink and group/other bits are refused by the server and the client, the default directory is tightened when this user owns it, and the socket is 0600 after bind. `Should_require_an_owner_only_socket_directory` passed, and the ping test checks 0600.

## Duplicates / conflicts

- One reviewer (general). M1–M4 carried forward from round 1 with status `fixed`. No new findings.
