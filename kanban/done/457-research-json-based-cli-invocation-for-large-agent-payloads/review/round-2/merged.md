# Round 2 — merged findings
**Date:** 2026-09-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 1 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: wontfix
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:107
- Description: Literal-count tie then exit 1. Round 1 asked to replace it with first-match by `ComputedSpecificity`.
- Suggestion: Not applied.
- Source: general
- Disposition notes: NURU_R003 (Error) already drops the optional-sibling example. `deploy {env}` and `deploy --env {env}` are a real tie for `{"env":"prod"}`; guessing the positional route would hide the option route. Clarified at recommendation.md:107-115. Calls without `--json-args` keep today's matcher. Decider: review oracle, round 2.

### M2 — Severity: bug — Status: fixed
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:178
- Description: `json-args` diagnostic described as a copy of a `--help` / `--capabilities` ban that does not exist.
- Suggestion: Document a new diagnostic and peel before user routes.
- Source: general
- Disposition notes: Fixed on this id. recommendation.md:178 and follow-up task 1.

### M3 — Severity: suggestion — Status: fixed
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:98
- Description: `IsConfigArg` also drops colon and `/` attached forms.
- Suggestion: Reject every attached form that filter would swallow.
- Source: general
- Disposition notes: Fixed on this id. recommendation.md:98, error table line 130, follow-up task 1.

### M4 — Severity: suggestion — Status: fixed
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:137
- Description: Stdin parse versus no-body built-ins, and `--json-args --help` versus "help wins".
- Suggestion: Value slot is always the next token. Dispatch a remaining built-in without reading stdin.
- Source: general
- Disposition notes: Fixed on this id. recommendation.md:137-139.

### M5 — Severity: nit — Status: fixed
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:125
- Description: `EmitNoMatch` cited on the wrong file.
- Suggestion: Cite `interceptor-emitter.cs`.
- Source: general
- Disposition notes: Fixed on this id.

## Duplicates / conflicts

- Single reviewer. Round 1 IDs carried forward. No new findings.
