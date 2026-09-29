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

- [x] `--json-args` reserved; diagnostic for a user option named `json-args`
- [x] Peel-off before config-arg filter and user routes; attached forms rejected
- [x] Route selection with JSON-satisfied slots; tie rule; argv overrides JSON
- [x] Generated, reflection-free binding via existing converters
- [x] Error cases and exit codes as specified; REPL rejects `-`
- [x] Root help row
- [x] Tests from the scope list

## Notes

- If a rule proves unworkable, return `ORACLE_RESULT: Blocked — <which rule and why>` rather than inventing a variant.
- Commit and push your changes before reporting done. Run the build and test gate in the foreground.
- 2026-09-29: review oracle (ganda task work, tw-implementation-review effort 1, reviewer `general`). Session: review-oracle (Cursor implementer-cursor profile, headless). Rounds: 2. Final counts: bug 2 fixed, suggestion 1 fixed, open 0. Disposition: clean. Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`. Next host nodes: open-pr / done (no apply-review sibling).

## Results

`--json-args` is reserved (no short form) and peeled from the original `args` array before `IsConfigArg` and before user routes. Attached forms (`--json-args=`, `--json-args:`, `/json-args=`, `/json-args:`) exit 1. The next token is the value (`-`, `@path`, or an inline object), including when that token is `--help`. A remaining argv that already matches a no-body built-in or a completion route is dispatched and does not read stdin. Otherwise the object is parsed with `JsonDocument` and route selection lets parameter slots come from JSON keys. More matched literals win. An equal literal count exits 1 and lists the patterns. `ComputedSpecificity` is not used to break that tie. Argv overrides JSON. Unknown keys, type mismatches, empty stdin, and a TTY stdin exit 1. Binding assigns the same generated locals through `TypeConversionMap`, `EnumTypeConverter`, and registered custom converters, with no reflection. NURU_R005 rejects a user option whose long form is `json-args`. The REPL rejects `-`. Root help lists `--json-args`, and per-route help says values may come from it.

No rule from the recommendation was changed.

Review found two gaps against that contract. An absent key now keeps an option's property default instead of assigning `default!`. A missing required positional is named when the route's later literals still match, instead of collapsing to "Unknown command". An argv conversion failure names the key and the expected type and is not described as a JSON type error.

`generator-45` now expects 12 `FileInfo` and `DirectoryInfo` constructions (was 4). Each route emits the matcher conversion plus the `--json-args` argv branch and the JSON branch. Every construction is still inside `catch (Exception)`.

Verification:

- `dotnet tests/ci-tests/run-ci-tests.cs`: exit 0. Multi-mode total 1789, passed 1783, skipped 6, failed 0, including `JsonArgs` 19/19. Standalone phase passed, including `generator-50` (NURU_R005) and `generator-45`.
- `dotnet run tools/dev-cli/dev.cs -- verify-samples`: `64/64 samples built successfully`.
- `ganda repo audit`: "Repository passes all audit checks." (`bin/dev` is gitignored; `ganda repo audit --fix` ran `self-install`).

### How to validate

Smoke:

```bash
dotnet tests/ci-tests/run-ci-tests.cs
dotnet run tools/dev-cli/dev.cs -- verify-samples
ganda repo audit
dotnet run tests/timewarp-nuru-tests/routing/routing-33-json-args.cs
```

Expect:

- The CI runner exits 0. Multi-mode total 1789, passed 1783, skipped 6, failed 0. `JsonArgs` is 19 passed. Standalone tests pass, including `generator-50-json-args-reserved.cs`.
- `verify-samples` prints `64/64 samples built successfully`.
- Audit prints "Repository passes all audit checks."
- `routing-33-json-args.cs` exits 0 with 19 passed. That covers merge precedence, unknown key, type mismatch, enum, repeated option, catch-all, `@file`, stdin larger than `MAX_ARG_STRLEN`, empty stdin, TTY stdin, each attached form, the equal-literal-count tie, a built-in that does not read stdin, REPL rejection of `-`, a route that matches only because required positionals are in the JSON object, option defaults kept when a JSON key is absent, a missing positional named when later literals match, and an argv conversion failure that is not labeled a JSON type error.

Implementation review (effort 1, reviewer `general`): rounds 2. Final counts: bug 2 fixed, suggestion 1 fixed, nit 0, open 0. Disposition: `clean`. Artifacts: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.
