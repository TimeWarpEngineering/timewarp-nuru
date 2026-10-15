# Roslynk CLI spike on Nuru (pitch for Pete)

## Description

Hand-write a small Roslynk CLI on Nuru to show Pete (Peter Morris, author of Roslynk) what a Roslynk CLI built on Nuru looks like. This is a pitch: it stays Pete's project, and Steven helps. Peter said he is considering Nuru for a Roslynk CLI generated from the `[McpServerTool]` and `[Description]` attributes. This spike proves the route shape by hand before any bridge or generator exists.

Do not frame it to Pete as "Nuru's Code Mode". Contacting SimCube or upstreaming anything is Pete's call; bots don't reach out.

## Context

- timewarp-intel `evaluations/2026-10-15-opencode2-nuru.md` (Busara: GREEN). Recommended order item 1: Roslynk hand-written spike with two commands, `diagnostics` and `refs`. Size S.
- timewarp-intel `evaluations/2026-10-15-nuru-client-server.md` (Busara: daemon (A) GREEN, gated on this spike's cold-vs-warm measurement). Reference design for a warm daemon: SimCubeLtd/roslynk-cli (System.CommandLine client plus a local daemon over a Unix socket or named pipe).

## Requirements

- Two hand-written Nuru routes that call Roslynk's tool methods in-process: `diagnostics` and `refs`, with `[Description]` text carried into help and `--capabilities`.
- Agent contract works end to end: `--capabilities` lists both endpoints; `--json-args` (`-`, `@path`, inline) invokes each.
- Test the `--json-args` to MCP JSON Schema mapping against Roslynk's real tool inputs (nested objects, arrays, enums). Record which inputs map cleanly and which do not. This result decides the shape of task 507 (McpBridge).
- Measure the token size of the full `--capabilities` output, and extrapolate to Roslynk's full tool set (about 40 tools). This decides whether group or prefix filtering beyond the existing `CapabilitiesFilter.Group` is needed.

## Deliverable: cold-vs-warm startup timing

Explicit deliverable, because the optional daemon package (`TimeWarp.Nuru.Daemon`, working name, from the client-server eval) depends on this result:

- Cold: one command in a fresh process, including solution load.
- Warm: a second call to the same command in the same process, with the solution already loaded.
- Report median and spread over repeated runs on the same solution, with the machine and build configuration. Compare with SimCube's `roslynk-cli` daemon as a baseline where possible.
- State the gap. If it is small, the daemon is nice-to-have; if it is large, the daemon is GREEN.

## Checklist

- [ ] Spike app with `diagnostics` and `refs` routes on Nuru
- [ ] `--capabilities` and `--json-args` proof for both routes
- [ ] `--json-args` vs MCP JSON Schema mapping findings
- [ ] `--capabilities` token size, measured and extrapolated
- [ ] Cold-vs-warm timing results written into this task
- [ ] Short pitch summary for Pete (what it looks like, what it costs, what is next)

## Notes

- Nuru's `RunAsync` must be intercepted by the source generator (`source/timewarp-nuru/nuru-app.cs`), so the spike uses normal compile-time routes.
- Follow-ups: task 507 (McpBridge go/no-go) and the daemon package decision both read this task's results.

## Session

- Created: 4064788 (2026-10-15)
- Boy-scout audit fix: ran `ganda repo audit --fix` (root .gitignore porcelain + routine-journal lines; untracked kanban review-round markers) to unblock publish.
