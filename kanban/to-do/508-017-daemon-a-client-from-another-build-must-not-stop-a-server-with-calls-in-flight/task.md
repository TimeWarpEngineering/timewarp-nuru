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

## Checklist

- [ ] Model chosen and recorded
- [ ] No in-flight call is ever cancelled by another build's client (test with a blocking handler)
- [ ] Status shows build identity
- [ ] Docs

## Session

- Created: 2532391 (2026-10-21) by the Roslynk cockpit from the PR #9 review.

## Notes

- Related: 508-005 (daemon), 508-016 (concurrent calls), Roslynk 005.
