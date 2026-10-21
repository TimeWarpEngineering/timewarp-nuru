# Daemon: a client from another build must not stop a server with calls in flight

## Description

Found by the independent review of Roslynk PR #9 (TimeWarpEngineering/Roslynk, task 005), the first product
on the Nuru served app (TimeWarp.Nuru 4.0.0-beta.4).

`NuruDaemonClient.ForwardAsync` (`source/timewarp-nuru-daemon/nuru-daemon-client.cs` ~line 62-82) compares the
running server's build id with its own and, when they differ, stops the server and starts its own build. The
build id includes the executable path and its modification time. So two builds of the same app sharing one
socket stop each other's server on every alternate call, cancelling in-flight calls and discarding warm state.

Reproduced: a `get_diagnostics` through the packed `roslynk` tool was mid-call when the Release build of the
same app made a `get_solution_status` call. The first exited 130 ("Error: the call was cancelled.") and every
warm solution was lost.

Real setup that hits it: an MCP config running `dnx Roslynk -- stdio` (package cache path) next to a globally
installed `roslynk` (tool path) used from a hook or the shell. Also any developer running a local build beside
the installed tool. For a daemon whose value is warm state, this thrash is the worst case.

## Requirements

- A client never stops a server that has calls in flight.
- Pick and document one model, with tests:
  - **Socket per build** (default socket path includes a short hash of the build id), so different builds run
    side by side, each warm; or
  - **Refuse, do not replace**: a mismatched client gets a clear one-line error naming both builds and how to
    stop the old server, unless `--replace` / an option says otherwise; or
  - **Drain then replace**: the server stops accepting, finishes in-flight calls, then exits.
  Recommend socket-per-build plus an explicit `stop`; record why.
- `ping`/status shows the build id and path so a user can see which build serves.
- Idle stop still cleans up servers nobody uses.
- Docs: daemon page explains what happens with two builds.

## Goal (implementer restatement)

Two builds of the same served app (a `dnx` package-cache copy and a global tool, or a local build beside the
installed one) must never stop each other's server. Today the forwarding client stops any server whose build
id differs, so alternate calls cancel in-flight work (exit 130) and lose warm state.

Model (chosen): **socket per build, refuse on a shared socket.**
- The default socket path carries a short hash of the build id (`<app>-<hash8>.sock`), so each build gets its
  own warm server and they run side by side.
- When two builds still share one socket (explicit `socketPath` or `NURU_DAEMON_SOCKET`), the client never
  stops the other build's server. In Auto mode the call runs locally; with `--daemon` / `autoStart` it fails
  with one line naming both builds and how to stop the old server (`serve --stop`).
- Stopping stays explicit: `serve --stop`. Idle timeout still stops unused servers (old builds fade out).

Acceptance:
- A test holds a call in flight with a blocking handler, a client from another build forwards on the same
  socket, and the in-flight call still completes (not cancelled) and the server keeps running.
- Ping/status carries the build's entry path and write time; `serve --status` prints them and lists other
  builds' servers of the same app in the default socket directory.
- Default socket path differs per build id; tests cover it.
- Docs (served-apps page, package readme) explain what happens with two builds and why.

## Checklist

- [x] Model chosen and recorded
- [x] No in-flight call is ever cancelled by another build's client (test with a blocking handler)
- [x] Status shows build identity
- [x] Docs

## Session

- Created: 2532391 (2026-10-21) by the Roslynk cockpit from the PR #9 review.
- 2026-10-21 implementer-claude (ganda task work, headless): socket per build + refuse on a shared socket;
  tests, docs, smoke with two copies of the 16-daemon sample. CI multi-mode exit 0; `ganda repo audit` clean
  (no fixes needed).

## Results

**Model: socket per build, and refuse (never replace) on a shared socket.** Recorded in the served-apps doc
("Two builds of one app", with the why) and the `#region Design` of `nuru-daemon-client.cs` and
`nuru-daemon-paths.cs`.

- `NuruDaemonPaths.GetDefaultSocketPath(appName, buildId)` (new overload): `<app>-<hash8>.sock`, 8 hex digits
  of SHA-256 of the build id; the long-name fallback keeps the build part (`<hash16>-<hash8>.sock`).
  `NuruDaemonOptions.ResolveSocketPath` uses it for the default. The one-argument overload stays (shared name).
- `NuruDaemonClient.ForwardAsync` no longer stops a server of another build. Auto mode runs locally;
  `--daemon` / `autoStart` returns exit 1 with one line naming both builds and `serve --stop`
  (`OtherBuildMessage`, also used by the launcher).
- Ping carries `entry` and `written` (entry file, UTC write time). `NuruDaemonStatus` gains optional
  `EntryPath` / `WriteTime` and `DescribeBuild()`. An older server reads as empty and is described by its id.
- `serve --status` prints the build ("version V at <entry>, written <time>") and, on the default socket,
  lists other builds' servers of the app in the socket directory, with how to stop each
  (`NURU_DAEMON_SOCKET=<socket> … serve --stop`). Exit code still reflects this build's socket.
- Idle stop is unchanged, so an old build's server stops on its idle timeout after a rebuild.
- Known and documented: each socket leaves a zero-byte `<socket>.lock` (deleting it would race the lock);
  per-build sockets mean one per build in `$XDG_RUNTIME_DIR/nuru` (tmpfs, cleared at logout).
- Not done: draining on explicit `serve --stop` (it still cancels running calls, as the user asked for it);
  no generator change (no `serve --stop --all`).

Tests (`tests/timewarp-nuru-daemon-tests/daemon-01-server.cs`):
`Should_not_cancel_a_call_in_flight_when_another_build_calls` (a blocking `dm-hold` handler via
`CcRendezvous`; another build's Auto call runs locally, its `--daemon` call gets the one-line error, the held
call then finishes with exit 0 and the server still answers. It fails on the old code, which stopped the
server and returned exit 130), `Should_leave_a_server_from_another_build_running` (replaces
`Should_stop_and_not_use_a_server_from_another_build`), `Should_list_servers_of_other_builds_in_status`,
`Should_name_the_build_in_status`, `Should_default_to_one_socket_per_build`, plus build fields on ping.
All daemon (01–05) and McpHost (01–05) files pass; `dotnet run tests/ci-tests/run-ci-tests.cs` exit 0.

### How to validate

Smoke:

```bash
S=samples/endpoints/16-daemon/served-app.cs
dotnet run $S -- --daemon count hello            # build A starts its server
B=$(dirname "$(dotnet run $S -- serve --status | sed -n 's/.* at \(.*\), written.*/\1/p')")
D=$(mktemp -d); cp -r "$B/." "$D/"               # build B: same app, another entry path
"$D/served-app" --daemon count hello             # build B starts its own server
"$D/served-app" serve --status                   # B running + "Also serving served-app: … (A) …"
SOCK=$(dotnet run $S -- serve --status | sed -n '1s/.*socket \(.*\)\.$/\1/p')
NURU_DAEMON_SOCKET=$SOCK "$D/served-app" --daemon count hello; echo "exit=$?"
dotnet run $S -- serve --stop; "$D/served-app" serve --stop; rm -rf "$D"
dotnet run tests/timewarp-nuru-daemon-tests/daemon-01-server.cs
```

Expect:

- Both `--daemon count hello` calls print `5`; A's server stays running when B starts (two
  `served-app-<hash>.sock` files under `$XDG_RUNTIME_DIR/nuru`).
- `serve --status` from B prints `served-app server is running: pid …, version … at <D>/served-app, written …Z,
  socket …/served-app-<hash>.sock.` then an `Also serving served-app:` line for A with its entry, write time,
  socket and the `NURU_DAEMON_SOCKET=… serve --stop` hint.
- B on A's socket prints one line `Error: the served-app server on … is another build (…); this is …. It was left
  running; stop it with 'serve --stop', or give each build its own socket.` and `exit=1`; A still answers.
- Both stops print `served-app server stopped.`; daemon-01 reports 19 passed.

## Notes

- Related: 508-005 (daemon), 508-016 (concurrent calls), Roslynk 005.
