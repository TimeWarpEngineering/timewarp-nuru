# Bind --json-args in the Nuru source generator

## Description

Follow-up 1 from research task **457** (merged 2026-09-29, PR #271). Read the full recommendation first:
`kanban/done/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md`. It is the design of record: canonical shape, match and merge rules, errors and exit codes,
JSON type mapping, REPL/help/pipeline interaction, stdin handling, security, and AOT binding.

Agents need to pass large argument values (e.g. a multi-KB unified diff for Roslynk's `apply_patch`) without
hitting argv limits or shell quoting. The chosen contract is `--json-args` with `-` (stdin), `@path`, or an
inline object, keyed by the `name` strings `--capabilities` already emits.

## Scope (verbatim from the recommendation)


Reserve `--json-args` as a built-in with no short form. Add a new diagnostic when a user option's long form is `json-args` (user routes may still override `--help`; do not copy a diagnostic that is not there). On the original `args` array, before `IsConfigArg` and before user routes, reject every attached form that filter would drop (`--json-args=`, `--json-args:`, `/json-args=`, `/json-args:`) and peel a single separate-token `-`, `@path`, or inline object. The next token is always the value, even when it looks like `--help`. If removing that pair leaves argv that already matches a built-in in `EmitBuiltInFlags` or `EmitCompletionRoutes`, dispatch it and do not read stdin. Parse with `JsonDocument`. Extend route selection so literal segments still match argv while parameter slots may be satisfied by JSON keys. A candidate must satisfy every key and every required slot. Prefer more matched literals; if still tied, exit 1 and list the patterns. Do not use `ComputedSpecificity` to break that tie (`deploy {env}` and `deploy --env {env}` can both bind `{"env":"prod"}`). Calls without `--json-args` keep today's matcher. NURU_R003 already rejects a same-required-signature sibling, so that pair is not the tie. Argv overrides JSON. Unknown keys and type mismatches exit 1. TTY or empty stdin with `-` exits 1. Assign the same generated locals and command properties the matcher already assigns, via `TypeConversionMap` and `EnumTypeConverter`, with no reflection. Reject `-` inside the REPL; allow `@path` and quoted inline JSON there. Add a root-help row. Tests: merge precedence, unknown key, type mismatch, enum, repeated option, catch-all, `@file`, stdin larger than `MAX_ARG_STRLEN`, empty stdin, TTY stdin, each attached form, equal-literal-count tie lists both patterns and exits 1, a built-in that remains after the pair is removed does not read stdin, REPL rejection, and a route that matches only because required positionals are in the JSON object.

## Requirements

- Implement exactly the recommendation's rules; any deviation is recorded in Results with the reason.
- Build, full CI test gate, samples, and `ganda repo audit` pass (warnings are errors in tests and samples).
- Out of scope: the `--capabilities` `invocation` object and docs (task **479**), per-option `-`/`@path`.

## Checklist

- [ ] `--json-args` reserved; diagnostic for a user option named `json-args`
- [ ] Peel-off before config-arg filter and user routes; attached forms rejected
- [ ] Route selection with JSON-satisfied slots; tie rule; argv overrides JSON
- [ ] Generated, reflection-free binding via existing converters
- [ ] Error cases and exit codes as specified; REPL rejects `-`
- [ ] Root help row
- [ ] Tests from the scope list

## Notes

- If a rule proves unworkable, return `ORACLE_RESULT: Blocked — <which rule and why>` rather than inventing a variant.
- Commit and push your changes before reporting done. Run the build and test gate in the foreground.
