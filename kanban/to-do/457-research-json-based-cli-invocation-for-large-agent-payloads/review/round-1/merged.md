# Round 1 — merged findings
**Date:** 2026-09-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 2 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:107
- Description: Literal-count tie then exit 1 disagrees with first-match by `ComputedSpecificity`. `greet {name} {title?}` wins over `greet {name}` for `greet Ada`, but `greet --json-args '{"name":"Ada"}'` would exit 1. Specificity order plus unknown-key rejection already stops a zero-parameter `deploy` from stealing `{"env":"prod"}`.
- Suggestion: First satisfied route in existing `ComputedSpecificity` order wins. No new tie error. No `--json-args` means today's matcher. Literal-matched failures name the key, type, or missing required value.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:170
- Description: No analyzer forbids user routes from taking `--help` or `--capabilities`. User routes are emitted first so they can override those flags. The `json-args` diagnostic cannot be a copy of that check.
- Suggestion: Document a new diagnostic, and peel `--json-args` before user routes so a colliding option cannot override it.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:98
- Description: `IsConfigArg` also drops `--json-args:…`, `/json-args=…`, and `/json-args:…`. The spec and the follow-up task reject only the `=` form on `--`.
- Suggestion: Reject every attached form that filter would swallow. Separate-token forms stay legal.
- Source: general
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:131
- Description: Only `--help` is specified to skip stdin. Other no-body built-ins would parse `-` first. `tool --json-args --help` is both "value is `--help`" (exit 1) and "help wins".
- Suggestion: Dispatch a no-body built-in without reading stdin. The next token is always the value; help wins only outside that slot.
- Source: general
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:119
- Description: `EmitNoMatch` is on `interceptor-emitter.cs`, not `route-matcher-emitter.cs`.
- Suggestion: Cite each file for the method that actually lives there.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer. No overlaps.
