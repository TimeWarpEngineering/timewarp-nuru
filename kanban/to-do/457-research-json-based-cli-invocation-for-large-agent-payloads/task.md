# Research JSON-based CLI invocation for large agent payloads

## Description

Feature-request **research**: evaluate first-class support in TimeWarp.Nuru for **structured invocation** (JSON args and/or stdin/`@file` payloads) so AI agents can call Nuru CLIs with large free-text arguments without hitting shell `ARG_MAX`, quoting pain, or mandatory temp files.

**Driving use case:** [Roslynk](https://github.com/mrpmorris/Roslynk) (and similar tools) want to expose a full semantic API as a **Nuru CLI** over a **background HTTP daemon** (warm process), with agents discovering ops via **`--capabilities`** instead of (or in addition to) MCP. The remaining gap vs MCP is **large tool arguments** — e.g. `apply_patch` with a multi-file unified diff. Schema discovery is already solved by `--capabilities`; payload transport is not.

This task is research + design recommendation + follow-up implementation task(s). **Do not implement the feature in this task** unless a tiny prototype is needed to validate feasibility.

## Requirements

### Research outcomes (Definition of Done for this task)

1. Document how agents today should pass large payloads to Nuru CLIs (workarounds only).
2. Compare design options (below) for framework-level support vs app-only conventions.
3. Recommend a **canonical agent invocation shape** aligned with `--capabilities` (same parameter/option names).
4. Specify stdin vs file semantics (`-`, `@path`, heredoc) and whether a **file is ever required** (expected answer: no).
5. Call out AOT / source-gen implications (bind via endpoint metadata, not free reflection).
6. Note relationship to MCP: what JSON-invoke closes vs what remains host-integration-only.
7. Related work: task **142** (WASI/MCP/capabilities alignment), existing `--capabilities` catalog, any prior stdin patterns in Nuru/REPL.
8. Produce a short **design recommendation** under this folder (`research/recommendation.md`) and create follow-up kanban task(s) for implementation if the recommendation is “build it.”

### Non-goals for this research task

- Replacing argv for human-friendly short commands.
- Turning the CLI into a long-lived JSON-RPC server on stdin (that reinvents MCP stdio).
- Removing or redesigning `--capabilities` (it is the discovery half of the pair).

## Checklist

### Context & prior art

- [ ] Re-read `--capabilities` model (`CapabilitiesResponse`, `EndpointCapability`, `EndpointKind`, `--group-filter` / `--search`)
- [ ] Survey existing Nuru/MCP packages and samples for stdin or structured invoke patterns
- [ ] Survey industry conventions: `-` = stdin, `@file` = path (`kubectl apply -f -`, `gh api --input -`, etc.)
- [ ] Note OS limits: `ARG_MAX`, shell quoting, agent host shell nesting

### Design options (evaluate and recommend)

- [ ] **A. Fat-field only:** route via argv; one large option from stdin/file (e.g. `--patch -`)
- [ ] **B. `--json-args`:** route via argv; params/options filled from JSON object on stdin/`@file`
- [ ] **C. `--invoke-json`:** full call `{ "endpoint"|"pattern", "args": { ... } }` on stdin (CLI `tools/call`)
- [ ] **D. App-only convention:** document pattern; no framework change (Roslynk implements `-`/`@file` itself)
- [ ] Compare merge rules (argv vs JSON key conflicts), error UX, exit codes
- [ ] Capabilities surface: advertise support? always-on? per-endpoint?

### Feasibility

- [ ] Source-gen / AOT binding path from JSON keys → endpoint properties
- [ ] Interaction with REPL, help, and pipeline behaviors
- [ ] Windows/Unix stdin behavior (redirect, non-TTY, binary-safe text)
- [ ] Security notes (local CLI trust model; no new network surface)

### Deliverables

- [ ] Write `research/recommendation.md` (chosen option, UX examples, non-goals, open questions)
- [ ] Optionally write `research/roslynk-use-case.md` (or keep use case in Notes — already captured below)
- [ ] Create follow-up implementation task(s) via `ganda kanban create` if building
- [ ] Update `## Results` with How to validate (reviewers re-read recommendation + checklist)

## Notes

### Origin

Captured from architecture discussion on Roslynk + Nuru (TimeWarp agents / Grok session, 2026-08-06):

- Keep a **background daemon** (plain HTTP is fine); do **not** require MCP for the server.
- Agents in the TimeWarp ecosystem already use **`cli --capabilities`** then shell invoke (`ganda`, `dev`).
- MCP remains optional for MCP-native IDE hosts; it is not needed for discovery if capabilities are used.
- **Largest remaining CLI gap vs MCP:** large free-text args (`apply_patch`, similar).

### Roslynk use case (concrete)

Roslynk today: MCP tool server + detached HTTP daemon (`DaemonLauncher` / Gradle-style warm host). Tools return compact text outlines. Example painful tool:

| Tool | Large field | Why argv fails |
|------|-------------|----------------|
| `apply_patch` | unified diff (multi-file, multi-KB) | `ARG_MAX`, shell quoting, nested agent escaping |
| (future) any bulk JSON | structured body | same |

Desired agent loop (no MCP):

```bash
roslynk --capabilities
roslynk --capabilities --group-filter diagnostics   # optional scope
roslynk open_solution /abs/path/to.sln
roslynk get_diagnostics --solution /abs/path/to.sln --include-errors

# Large payload — must NOT require stuffing diff into argv
roslynk apply_patch --solution /abs/path/to.sln --patch - <<'EOF'
--- a/Foo.cs
+++ b/Foo.cs
@@
-old
+new
EOF
```

Or structured:

```bash
roslynk apply_patch --json-args - <<'EOF'
{
  "solutionId": "/abs/path/to.sln",
  "patch": "--- a/Foo.cs\n+++ b/Foo.cs\n...",
  "checkOnly": true
}
EOF
```

Or full invoke:

```bash
roslynk --invoke-json - <<'EOF'
{
  "endpoint": "apply_patch",
  "args": {
    "solutionId": "/abs/path/to.sln",
    "patch": "--- a/Foo.cs\n...",
    "checkOnly": false
  }
}
EOF
```

### Stdin vs file — analysis already agreed in discussion

| Mechanism | Required? | Notes |
|-----------|-----------|--------|
| **stdin pipe / heredoc** | Preferred | No `ARG_MAX`; no temp file; agent-friendly |
| **`-` means stdin** | Convention | Widely understood |
| **`@path` means file** | Optional convenience | Retries, debugging — not mandatory |
| **Temp file only** | Not required | Avoid forcing agents to write then read |

**Answer to “must one write a file?”: No.** Pipe JSON or raw body on stdin. Offer `@file` as an alternative.

### Why `--capabilities` matters (pair with invoke)

`--capabilities` already exposes agent-facing schema:

- `pattern`, `groupPath`, `description`
- `kind`: `query` | `command` | `idempotentCommand` (≈ MCP readOnly / destructive / idempotent)
- `parameters[]` / `options[]` with `name`, `type`, `required`, `description`, defaults

| MCP | Nuru today / proposed |
|-----|------------------------|
| `tools/list` | `--capabilities` |
| `tools/call` { name, arguments } | **missing** → JSON/stdin invoke research target |
| Host tool panel / `mcp add` | Still MCP-only (out of scope to replace) |

JSON invoke should use **the same names** as capabilities so agents map catalog → call without a second schema.

### What JSON/stdin invoke would close vs leave open

**Closes (for shell-native agents):**

- Large payloads without ARG_MAX/quoting failure
- Symmetric discovery + invoke story (`capabilities` + structured call)
- Stronger “CLI-only product surface” for apps like Roslynk

**Does not replace:**

- MCP host auto-wiring (Claude/Cursor tool panels)
- Per-tool host approval without unrestricted shell
- Long-lived in-host tool session (process-per-CLI-call remains; daemon still holds warm state)

### Suggested design principles (for researchers)

1. **Human argv stays** for short commands; structured invoke is additive.
2. **One-shot** JSON body / stdin payload — not a persistent JSON-RPC server on stdin.
3. **`-` = stdin, `@path` = file** for any body-bearing flag.
4. **SSOT:** JSON keys = capability parameter/option names.
5. **Merge policy:** document argv vs JSON precedence (pick one).
6. **AOT-safe:** use generated endpoint metadata.
7. Prefer framework support so every Nuru CLI (ganda, dev, roslynk, …) inherits the same agent contract; app-only `-`/`@file` is a valid interim for Roslynk.

### Example interim (app-level, no Nuru change)

```csharp
[Option("patch", Description = "Unified diff; use - for stdin or @path for file")]
public string Patch { get; set; } = "";

// Handler resolves -, @file, or literal
```

Researchers should decide whether this interim is enough as the *recommended* story or only a stopgap until framework support.

### Related

- Kanban **142** — Investigate WASI/MCP/Nuru capabilities alignment (schema/interop; complementary)
- Nuru skill / docs: `--capabilities` “for AI tools”
- `TimeWarp.Nuru.Search` indexes CLIs via `--capabilities`
- Consumer context: Roslynk architecture (daemon + optional CLI clients; MCP as optional adapter)

### Folder layout for artifacts

```
457-research-json-based-cli-invocation-for-large-agent-payloads/
  task.md                      # this file
  research/
    recommendation.md          # required deliverable
    notes.md                   # optional working notes
```

## Session

- Created: grok session (2026-08-06) — feature-request research task from Roslynk + Nuru architecture discussion; use case and analysis captured for agent handoff
