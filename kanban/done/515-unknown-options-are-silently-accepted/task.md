# Unknown options are silently accepted

## Description

TimeWarp.Nuru's generated matcher treats an unrecognized `--flag` as another positional. A route that already has options does not check leftover positionals, so the handler runs and the flag disappears (exit 0). Routes with only literals and required parameters still reject the extra token as an unknown command.

That is `route-matcher-emitter.cs`: routes with options skip the exact-length check "because unknown options become extra positionals." `Should_ignore_undefined_options` in `tests/timewarp-nuru-tests/routing/routing-13-negative-numbers.cs` records this as V2 behavior (V1 failed).

It is dangerous for mutating commands. On 2026-10-20 `ganda kanban create "Title" --repo timewarp-architecture` (no such option) reserve-and-claimed in the current repo. Ganda task 439 now rejects unknown options before `RunAsync`. This task is the matcher fix. The ganda guard can stay; it becomes redundant once Nuru fails closed.

## Goal (implementer restatement)

The generated argv matcher must fail closed on option-shaped tokens that the route does not declare.
Today a route with options drops an unknown `--flag` into the positional list, skips the exact-length
check, and runs the handler with exit 0.

Acceptance:

- A token before `--` that starts with `-`, is not `-` alone, is not a number, and is not a declared
  option form (or a value an option consumed) is an unknown option. When a route's literals match
  apart from that token, the route does not run. If no other route matches, the app writes
  `Unknown option '<name>'` (with `Did you mean '<form>'?` when a declared form of a matching route is
  close) and exits 1.
- Negative numbers (`-5`, `-1.5e10`) stay positionals.
- Tokens after `--` stay positionals, even option-shaped ones.
- Catch-all routes still take real positionals, and reject unknown options before `--`.
- An extra non-option token on a route with no options still gives `Unknown command`.
- REPL lines go through the same generated matcher, so they fail the same way.
- `Should_ignore_undefined_options` becomes a failure test.

## Checklist

- [x] Unknown options fail with a non-zero exit and name the option
- [x] Suggest the closest declared form when one is near
- [x] Replace `Should_ignore_undefined_options` with a failure test
- [x] Negative numbers (`-5`, `-1.5e10`) stay positionals
- [x] Tokens after `--` stay positionals, including ones that look like options
- [x] Catch-all routes still accept real positionals and still reject unknown options before `--`
- [x] Routes with no options keep a clear unknown-command error when the extra token is not an option
- [x] REPL dispatch rejects unknown options the same way (ganda's outer guard never sees a REPL line)

## Session

- Created: 3127778 (2026-10-21)
- 2026-10-21 implementer (claude, headless task work): matcher fix, runtime helper, tests, docs.
  `ganda repo audit` passed with no fixes needed.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-21T14:27:16Z
- Review oracle: grok session 01a15edb-f7b0-7011-9e3b-44f7fe830d66 (2026-10-21), effort 2, roster general. Round 1 written. M1 open. Disposition not written.
- Review oracle: review by reviewer-grok (grok, model grok-4.7), session not reported, max-turns 120 — 2026-10-21T14:42:58Z
- 2026-10-21 implementer (claude, headless task work, implement-fix-2): fixed M1 (deferred option
  guards, conversions after the guard skip, alias skip label), added 6 routing-37 tests. Gates green.
  `ganda repo audit` passed with no fixes needed.
- Review oracle round 2: grok session `01a15ef8-d701-70e3-9057-5498bbd97c16` (2026-10-21), effort 2, roster general. M1 fixed. M2 open. Disposition not written.
- Review oracle round 3: grok session `01a15f10-3ffd-7881-8126-addf1b8abb1d` (2026-10-21), effort 2, roster general. M2 fixed. M3 open. Disposition not written.
- 2026-10-21 implementer (claude, headless task work, implement-fix-3): fixed M2 (shape check for
  routes with no leading literal, before the guard skip), added 4 routing-37 tests. Gates green.
  `ganda repo audit` passed with no fixes needed.
- 2026-10-22 implementer (claude, headless task work, implement-fix-4): fixed M3 (only a typo of a
  value-taking option drops the next token), added 4 routing-37 tests. Gates green.
  `ganda repo audit` passed with no fixes needed.
- Review oracle round 4: grok session `01a15f72-4a2a-72c1-84a7-5eaf084c138d` (2026-10-22), effort 1, roster general. M1–M3 fixed. No new findings. Disposition: clean.

## Results

**Generator** (`route-matcher-emitter.cs`): positional construction holds out any token before `--`
that is option-shaped (`NuruUnknownOption.IsOptionLike`), unconsumed, and not a declared form of the
route. A route never runs with such a token. When the route's leading literals match, or the whole route
matches for a route with no leading literal, the token and the route's option forms go into method-level
`__unknownOption` / `__unknownOptionForms`. Matching continues, so another route or a built-in can
still win. The no-match fallback (`interceptor-emitter.cs`) writes the unknown-option line and returns 1
before the generic `Unknown command`. Since unknown options no longer become positionals, routes with
options now get the exact-length check too, so `build stray` on `build --verbose` is `Unknown command`.
Every occurrence of a boolean flag is consumed now, so `--verbose --verbose` still matches. A JSON
member given on argv consumes its value, so the `--X is JSON` error still wins.

**Runtime** (`source/timewarp-nuru/invocation/nuru-unknown-option.cs`): `NuruUnknownOption.IsOptionLike`,
`FormatMessage`, `Suggest`. The suggestion is the closest form by Levenshtein distance. It needs
distance <= max(2, len/3) and less than the typed name's length, so `-x` never suggests `-f`.

**REPL**: the REPL calls the same `ExecuteRouteAsync` (`fromRepl: true`), so the fix covers it. Test
`Should_reject_unknown_option_in_repl` checks this.

**Behavior changes (in changelog)**: catch-all routes take option-shaped args only after `--`. Updated
tests: routing-04 (3 cases), routing-12 colon catch-all, generator-11 catch-all endpoint,
routing-10 unknown option (now exit 1), routing-13 `echo -sometext` (now unknown option, plus a `--` case).
`Should_ignore_undefined_options` became `Should_reject_undefined_options`.

**Not changed**: `--key=value` / `--key:value` are still stripped before routing as configuration
overrides (`IsConfigArg`), so `kanban create T --repo=x` still runs silently. That is a separate
fail-open path. It needs its own decision (only strip when the app uses configuration, or only keys
the configuration knows), so this task does not change it.

**Fix pass for review round 1 (M1)**: Option guards no longer skip the route during option parsing.
A missing required option, a missing required value, or an absent unbound discriminator flag sets
`__optionMissing_N`. The route skips on that flag after the unknown-option check, so
`kanban create T --foldr x` (route `--folder {folder}`) gives `Did you mean '--folder'?` and
`commit --ammend` gives `Did you mean '--amend'?`. Option type conversions now run after that skip,
since a missing value has to skip the route and must not hit a conversion error (for example the
FileInfo "Missing value" error). The guard skip uses the label of the block it is in. Before this
pass, an alias block jumped to `route_skip_N`, which comes before the alias, so a failed guard on an
alias looped forever. The new alias test timed out at 240 s on the old emitter and passes now. The
changelog has a separate Fixed entry for the alias loop. New tests in routing-37: a close typo of a
required value option, a close typo of a discriminator flag, an unknown option next to a required
option that is present, a required option that is present (still runs), an alias with a failed guard
plus an unknown option, and an alias with a value (still runs).

**Fix pass for review round 2 (M2)**: A route with no leading literal recorded an unknown option
only after the whole route matched, which came after the option-guard skip, so `--check-update` on
`Map("--check-updates")` gave `Unknown command`. That route now records the option at the same point
as the prefix check, before the guard skip, when its positional args fit the route's shape (count,
exact count when the route has no optional or catch-all parameter, and literals in place). The check
also tries the positional args without the token right after each unknown option, since that token is
likely its value: in `readme --outpt out` for `{file} --output {output}`, `out` would otherwise be an
extra positional. `EmitUnknownOptionMatchCheck` is gone; `EmitUnknownOptionShapeCheck` replaces it
in `EmitMatch` and in `EmitAliasMatch` (empty alias). An extra positional with no unknown option is
still `Unknown command`, and a missing value still skips the route. The round-2 review said the
optional twin (`{file} --output? {output?}`) already suggested; a probe on the old emitter gave
`Unknown command`, and it suggests now. New tests in routing-37: the two probed cases, the optional
twin, and a missing value on a route with no literal (still `Unknown command`, no conversion error).

**Fix pass for review round 3 (M3)**: The shape check dropped the token after every unknown option,
flags included, so `--check-update extra` on `Map("--check-updates")` suggested `--check-updates`
when the corrected argv would still not match. Now the token is dropped only when the unknown option
has no inline `=value` and its closest declared form (the same `Suggest` the error line uses) is a
value-taking option. The runtime helper is `NuruUnknownOption.TakesValue(token, declaredForms, valueForms)`;
the emitter adds `__valueOptionForms_N` (non-flag forms) next to `__positionalNoValues_N` and sets
`__unknownAt_N` only when `TakesValue` is true. New tests in routing-37: `--check-update extra` on
`--check-updates`, `readme --check-update extra` on `{file} --check-updates`, and `readme --forc extra`
on `{file} --output {output} --force` all exit 1 with `Unknown command` and no suggestion;
`readme --outpt out` on that same route still suggests `--output`.

Gates (rerun after the M3 fix pass): `./bin/dev build` OK; `dotnet run tests/ci-tests/run-ci-tests.cs`
exit 0 (multi-mode plus standalone, 0 failed); `ganda repo audit` passes with no fixes needed.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/routing/routing-37-unknown-options.cs
dotnet run tests/timewarp-nuru-tests/routing/routing-13-negative-numbers.cs
dotnet run tests/ci-tests/run-ci-tests.cs
```

Expect: routing-37 has 30 of 30 passed and routing-13 has 11 of 11 passed. The CI runner exits 0 and
ends with `Standalone finished in ... (0 failed)`. In routing-37,
`Should_reject_unknown_option_on_route_with_options` shows `kanban create Title --repo x` exiting 1
with `Unknown option '--repo'`, and `Should_suggest_closest_declared_option` shows
`Unknown option '--verbos'. Did you mean '--verbose'?`. For M1, `Should_suggest_required_value_option_for_close_typo`
expects `Unknown option '--foldr'. Did you mean '--folder'?` and
`Should_suggest_discriminator_flag_for_close_typo` expects `Unknown option '--ammend'. Did you mean '--amend'?`.
`Should_reject_unknown_option_on_alias_with_failed_option_guard` finishes in well under a second (it hung on the old emitter).
For M2, `Should_suggest_discriminator_flag_on_route_without_literal` expects
`Unknown option '--check-update'. Did you mean '--check-updates'?` and
`Should_suggest_required_value_option_on_route_without_literal` expects
`Unknown option '--outpt'. Did you mean '--output'?`.
For M3, the three `Should_keep_unknown_command_for_flag_typo_*` tests expect `Unknown command` with no
`Did you mean`, and `Should_suggest_value_option_typo_beside_flag` expects
`Unknown option '--outpt'. Did you mean '--output'?`.

### Review

Rounds 1–3 were effort 2. Round 4 was effort 1. Roster general throughout. The implementer restatement matches the original task (no misread). Round 4 counts: 0 open, 3 fixed bugs, 0 wontfix. Disposition: **clean**.

M1 is fixed in `72cf0f08`. A close typo of a required option or unbound flag on a route with a leading literal is still suggested. `routing-37` on `fb21baaf` passed 30 of 30, including those cases.

M2 is fixed in `0ef92207`. `--check-update` on `--check-updates`, and `readme --outpt out` on `{file} --output {output}`, suggest the declared form and do not run the handler.

M3 is fixed in `fb21baaf`. The next token is dropped only when `NuruUnknownOption.TakesValue` finds the closest declared form is a value-taking option. `--check-update extra`, `readme --check-update extra`, and `readme --forc extra` exit 1 with `Unknown command` and no suggestion. `readme --outpt out` on `{file} --output {output} --force` still suggests `--output`. This round re-ran `routing-37` (30 of 30). The full CI runner was not re-run here.

Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/round-3/general.md`, `review/round-3/merged.md`, `review/round-4/general.md`, `review/round-4/merged.md`, `review/disposition.md`.

## Review

- Round 1: Blocked — open M1
- Round 2: Blocked — open M2
- Round 3: Blocked — open M3
- Round 4: Done
## Notes

Filed from timewarp-ganda task 439. Do not add `--repo` to ganda `kanban create` here. Ganda's message for that case points at `kanban reserve --repo` then `kanban claim <id> --repo`.
