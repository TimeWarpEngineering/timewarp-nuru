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
the configuration knows), so this task does not change it. A required option that is missing
(`--folder {f?}` without `?` on the option) skips the route before unknown-option detection, so
that route adds no suggestion.

Gates: `./bin/dev build` OK; `dotnet run tests/ci-tests/run-ci-tests.cs` exit 0 (multi-mode plus
standalone, 0 failed); `./bin/dev verify-samples` 68/68; `ganda repo audit` passes.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/routing/routing-37-unknown-options.cs
dotnet run tests/timewarp-nuru-tests/routing/routing-13-negative-numbers.cs
dotnet run tests/ci-tests/run-ci-tests.cs
```

Expect: routing-37 has 16 of 16 passed and routing-13 has 11 of 11 passed. The CI runner exits 0 and
ends with `Standalone finished in ... (0 failed)`. In routing-37,
`Should_reject_unknown_option_on_route_with_options` shows `kanban create Title --repo x` exiting 1
with `Unknown option '--repo'`, and `Should_suggest_closest_declared_option` shows
`Unknown option '--verbos'. Did you mean '--verbose'?`.

## Notes

Filed from timewarp-ganda task 439. Do not add `--repo` to ganda `kanban create` here. Ganda's message for that case points at `kanban reserve --repo` then `kanban claim <id> --repo`.
