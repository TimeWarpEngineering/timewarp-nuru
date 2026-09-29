# Round 2 — general
**Date:** 2026-09-29
**Scope reviewed:** post-fix `research/recommendation.md` against round-1 M1–M5, plus `interceptor-emitter.cs` (`IsConfigArg`, `EmitBuiltInFlags`, `EmitNoMatch`, user-routes-before-built-ins), `segment-definition.cs` specificity weights, `diagnostic-descriptors.overlap.cs` NURU_R003, and `option-syntax.cs` (fluent options required unless `?`).

## Summary

M2, M3, M4, and M5 are fixed in the recommendation and in follow-up task 1. M1's suggested replacement (always first-match by `ComputedSpecificity`) is not adopted. Re-check shows the optional-sibling example is NURU_R003 (Error), and a legal pair such as `deploy {env}` and `deploy --env {env}` can both bind `{"env":"prod"}`. Exit 1 on that literal-count tie is the right contract. The recommendation now says so, and says calls without `--json-args` keep today's matcher. No new issues.

## Resolved prior

### M1 — Severity: bug — Status: wontfix
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:107
- Description: Round 1 read the literal-count tie as a break with argv, using `greet {name}` beside `greet {name} {title?}`. That pair shares a required signature. NURU_R003 is an Error (`diagnostic-descriptors.overlap.cs`), so it is not two candidates in a compiling app. `deploy {env}` (1100) and fluent `deploy --env {env}` (1000 + required option 75) do both compile and can both bind `{"env":"prod"}`. First-match would run the positional route and hide the option route. The tie stays exit 1, and the text now says not to break it with `ComputedSpecificity`.
- Suggestion: Not applied. Do not replace the tie with silent first-match.
- Disposition notes: Clarified at recommendation.md:107-115 and in follow-up task 1. Decider: review oracle, round 2.

### M2 — Severity: bug — Status: fixed
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:178
- Description: The reserved-flag sentence now says no diagnostic forbids `--help` or `--capabilities`, user routes are emitted first so they can override those flags, and `json-args` needs a new diagnostic. Peel happens before user-route matching. Follow-up task 1 says the same.
- Suggestion: Applied.
- Disposition notes: Fixed on this id.

### M3 — Severity: suggestion — Status: fixed
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:98
- Description: Attached forms now include `--json-args:`, `/json-args=`, and `/json-args:`, matching `IsConfigArg` (`eqIdx` / `colonIdx` on `--` and on `/` when the second character is a letter). The error table and follow-up task 1 reject those forms. Separate-token `-`, `@path`, and inline JSON stay legal.
- Suggestion: Applied.
- Disposition notes: Fixed on this id.

### M4 — Severity: suggestion — Status: fixed
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:137
- Description: Help wins only when `--help` / `-h` is its own argv element. The next token is the value, so `--json-args --help` is exit 1. If removing the pair leaves a built-in shape from `EmitBuiltInFlags` or `EmitCompletionRoutes`, that built-in runs and stdin is not read. The REPL section matches this.
- Suggestion: Applied.
- Disposition notes: Fixed on this id.

### M5 — Severity: nit — Status: fixed
- File: kanban/to-do/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md:125
- Description: Unknown-command exit 1 is cited on `EmitNoMatch` in `interceptor-emitter.cs`. Invalid-value exit 1 stays on `route-matcher-emitter.cs`.
- Suggestion: Applied.
- Disposition notes: Fixed on this id.

## Issues

No new issues.
