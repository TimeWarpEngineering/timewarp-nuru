# Served endpoints: daemon host and forwarding client for Nuru apps

## Description

A framework-level way for a Nuru app to run as a long-lived server and have its CLI forward routes to that server instead of handling them in-process. Working name from the client-server eval: `TimeWarp.Nuru.Daemon`. Today an app that needs warm state (Roslynk's loaded Roslyn workspaces) must write a nested forwarding `Handler` per endpoint and its own client, transport, daemon discovery and lifetime handling.

Backlog until task 506 reports cold-vs-warm timing for Roslynk. If the gap is small the daemon is nice-to-have and this task is archived; if large, it moves to to-do.

## Requirements (when promoted)

- `serve` built-in route (or builder option) that hosts the app's endpoints behind a per-user local transport (Unix domain socket or named pipe preferred over a fixed TCP port; loopback HTTP acceptable as a first transport).
- Client mode: when a server is reachable (or `--daemon` is set), the generated CLI serialises the bound request (508-003's binder output), sends it, and renders the returned typed result exactly as in-process invocation would. Probe-or-start semantics with idle shutdown.
- A route can opt out of forwarding (always local), for example `serve` itself and version/help.
- Behaviors: define which chain runs where (CLI-side telemetry versus server-side validation).
- Works with 508-004 so the MCP host and the CLI share one running server.

## Checklist

- [ ] Promote or archive from task 506 timing results
- [ ] Transport and discovery design note
- [ ] Server host and forwarding client
- [ ] Opt-out and behavior placement
- [ ] Tests and docs

## Notes

- Parent: 508. Reference design: SimCubeLtd/roslynk-cli (System.CommandLine client plus local daemon). Roslynk already has `DaemonLauncher` and an idle-eviction service in its MCP host that do this by hand.
- Related eval: timewarp-intel `evaluations/2026-10-15-nuru-client-server.md`.

## Session

- Created: 1479989 (2026-10-17)
