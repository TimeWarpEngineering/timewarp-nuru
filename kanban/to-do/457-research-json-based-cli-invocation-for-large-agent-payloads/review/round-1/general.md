# Round 1 — general
**Date:** 2026-09-29
**Scope reviewed:** branch `task/457-research-json-based-cli-invocation-for-large-agent` vs `origin/master` — `research/recommendation.md` and the Results on `task.md`. Claims checked against `interceptor-emitter.cs`, `route-matcher-emitter.cs`, `segment-definition.cs`, `built-in-flags.cs`, `endpoint-extractor.cs`, `parameter-attribute.cs`, `capabilities-response.cs`, `capabilities-json-serializer-context.cs`, `nuru-app.cs`, `repl-session.cs`, `capabilities-client.cs`, and archived task 142.

## Summary

The recommendation chooses `--json-args`, keeps the route on argv, and correctly rejects `--invoke-json`. Argv-overrides-JSON, exit code 1, stdin `-`, optional `@path`, AOT binding via generated metadata, and the task 142 split all match the repo. Two specified behaviors do not: candidate tie-break would reject JSON calls that argv already accepts, and the reserved-flag diagnostic is described as a copy of a check that does not exist. Three smaller gaps are a colon/`/` config-arg hole, stdin vs no-body built-ins, and a wrong `EmitNoMatch` file.

## Issues

### Issue 1 — Severity: bug
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:107
- Description: "Several candidates → prefer the route with more matched literals. If still tied, exit 1" is not how the matcher chooses a route. `interceptor-emitter.cs` walks `ComputedSpecificity` descending and takes the first match. Weights in `segment-definition.cs` are literal 1000, required parameter 100, optional parameter 50, required option 75, optional option 25, catch-all 10. `greet {name} {title?}` (1150) is tried before `greet {name}` (1100), so `greet Ada` succeeds on the optional-title route. The same values as `greet --json-args '{"name":"Ada"}'` share one literal and the spec exits 1. That breaks the "same names, same call" contract. Unknown-key rejection plus specificity order already stops a zero-parameter `deploy` from stealing `{"env":"prod"}` from `deploy {env}` (1100 before 1000). A new literal-count tie error is not required for that case.
- Suggestion: Keep today's walk: first route in `ComputedSpecificity` order that matches literals and is fully satisfied by argv ∪ JSON wins. Equal specificity stays stable registration order. Do not exit 1 because two routes share a literal count. Calls without `--json-args` keep today's matcher. When nothing matches but some route matched literals, name the unknown key, type mismatch, or missing required value instead of "Unknown command".
- Status: open

### Issue 2 — Severity: bug
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:170
- Description: The text says an analyzer should reject a user option long form `json-args` "the same way user routes must not take `--help` or `--capabilities`." No such diagnostic exists under `source/timewarp-nuru-analyzers/diagnostics`. `interceptor-emitter.cs` emits user routes before built-ins "so users can override --help, --version, etc." An implementer looking for that diagnostic to copy will not find it, and might allow a user option named `json-args` to override the built-in the way `--help` can.
- Suggestion: Say the diagnostic is new. Peel `--json-args` before user-route matching so a colliding option cannot swallow it, and diagnose long form `json-args` at compile time. Do not treat `--help` override as the precedent.
- Status: open

### Issue 3 — Severity: suggestion
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:98
- Description: `IsConfigArg` drops `--key=value` and any `--` token with `colonIdx > 2`, and the same for `/key=value` and `/key:value` when the second character is a letter. The spec rejects only `--json-args=…`. `--json-args:…`, `/json-args=…`, and `/json-args:…` still vanish from `routeArgs` and the command runs with no payload — the failure mode the `=` rule was written to prevent. The follow-up task repeats "reject the `=` form" only.
- Suggestion: Reject every attached form `IsConfigArg` would swallow. The separate-token forms (`--json-args -`, `--json-args @path`, `--json-args '{…}'`) stay the only legal ones.
- Status: open

### Issue 4 — Severity: suggestion
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:131
- Description: Help is specified to win without reading stdin. Peel-and-parse of `-` happens before matching. `tool --version --json-args -` (and `--capabilities` with its `--search` / `--group-filter` forms, `--check-updates`, and the completion flags) would otherwise block or fail the TTY check before the built-in runs. Those built-ins are exact `routeArgs` matches in `EmitBuiltInFlags`, so they do not take a body. Separately, `--json-args` consumes the next token, so `tool --json-args --help` makes `--help` the value ("anything else" → exit 1) while the help row says help wins whenever `--help` is present. Those two rules disagree.
- Suggestion: If the argv aside from `--json-args` and its value is only a no-body built-in, dispatch that built-in and do not read stdin. The token immediately after `--json-args` is always the value. Help wins only when `--help` or `-h` is its own element outside that value slot.
- Status: open

### Issue 5 — Severity: nit
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:119
- Description: `EmitNoMatch` is `interceptor-emitter.cs` (writes "Unknown command. Use --help for usage." and returns 1). `route-matcher-emitter.cs` returns 1 inline for invalid values; it has no `EmitNoMatch` method.
- Suggestion: Cite `interceptor-emitter.cs` for `EmitNoMatch` and `route-matcher-emitter.cs` for invalid-value exit 1.
- Status: open
