# Raw catch-all route that hands the handler every argument as typed

## Description

Add a second, explicit kind of catch-all: every remaining token goes to the handler exactly as
typed, with no option checking.

**Why.** Nuru is meant to let us override existing shell commands fully or partially. Those
commands do not share one option syntax: `tail -40`, `tail +5`, `find . -name x -o -type f`,
`dd if=a of=b`, `tar xzf a.tgz`, `chmod -x file`. Nuru's router cannot learn each one, so a
command override parses its own arguments. It needs a route that gets out of the way.

**What happens today** (measured on 4.0.0-beta.18, 2026-11-27):

- `Map("{*args}")` rejects `-n 40` with `Unknown option '-n'. Use --help for usage.`, exit 1.
- Option-shaped tokens reach a catch-all only after `--`.
- `--key=value` is taken as a configuration override and removed before routing, so
  `--lines=40` on a bare catch-all arrives as nothing.

That is the intended behaviour of `{*args}` since task 515 ("Unknown options fail closed"), and
it stays. `kanban create "Title" --repo x` must keep failing.

## Requirements

- A distinct marker for the raw form, so opting out of option checking is visible in the
  route. Proposal: `{**args}`. A builder call such as `.WithRawArgs()` is the alternative. The
  implementer picks one and says why in Results.
- The handler receives `string[]` with every token after the route's literals, in order and
  unchanged: `-n`, `-40`, `+5`, `--`, `--lines=40`, a lone `-`, empty strings.
- No `--key=value` configuration-override stripping on a raw route.
- `--help` and `--version` are passed through on a raw route, not intercepted. A command
  override answers them the way the real tool does.
- A raw route may have leading literals (`tail {**args}`) or none (a default route, for an
  executable that is itself named `tail`).
- A raw route cannot also declare options or other parameters. That is a build-time
  diagnostic.
- `{*args}` behaviour does not change. The task 515 tests stay green untouched.
- Works under AOT and in the REPL.

## Goal (implementer restatement)

A command override needs a route that hands every remaining token to the handler and stops interpreting them. `{*args}` stays fail-closed on an unknown option, and `--key=value` is still removed before a normal route matches. `{**args}` is the visible opt-out. `[NuruRoute]` accepts only a literal, so the endpoint form is `[Parameter(IsCatchAll = true, IsRaw = true)]` on a `string[]`.

The handler receives those tokens in order and unchanged: `-n`, `-40`, `+5`, `--`, `--lines=40`, a lone `-`, empty strings, `--help`, and `--version`. A raw route may have leading literals or none. It cannot also declare options, other parameters, `--`, or a type constraint (NURU_S011). `--json-args` is still peeled. `--interactive` / `-i` with no other tokens still starts the REPL. A raw route that starts with a literal still leaves `myapp --help` to Nuru. The same generated matcher runs under AOT and in the REPL.

Review round 1 adds three acceptances. Group help must not answer `<group> --help` or `<group> -h` when a raw route's literal prefix is exactly that group's words, which is the `ganda tail` shape (`[NuruRouteGroup("tail")]`, `[NuruRoute("")]`, raw catch-all). Those tokens reach the handler. A raw route with further literals still leaves the shorter group summary in place, and a sibling command under the same group keeps its own help. A runtime test maps that endpoint with `.Map<T>()` and checks the full token set on the property. Another runtime test runs a fluent raw route through `.WithAlias(...)`. routing.md states both configuration facts: an override on a raw route reaches the handler and is still applied to configuration, and an override typed before the leading literals stops that route from matching.

## Checklist

- [x] Route syntax and parser support for the raw marker
- [x] Generator emits a matcher that skips unknown-option checks for a raw route
- [x] Configuration-override stripping skips raw routes
- [x] `--help` / `--version` pass through on a raw route
- [x] Analyzer diagnostic for a raw catch-all combined with options or parameters
- [x] Tests: each token kind above arrives unchanged; default route and literal-prefixed route
- [x] Tests: existing `{*args}` fail-closed behaviour unchanged
- [x] `documentation/user/features/routing.md` and the `tw-nuru` skill describe the raw form
- [x] Changelog entry

## Results

The marker is `{**args}`, not `.WithRawArgs()`. The opt-out has to show up in the pattern, in help, and in `--capabilities`. `[NuruRoute]` only accepts a literal, so the endpoint form is `[Parameter(IsCatchAll = true, IsRaw = true)]`. `IsRaw` without `IsCatchAll` is NURU_S011 and does not become a catch-all.

The generated matcher keeps a second argv, `__rawRouteArgs`: `--json-args` is peeled and configuration overrides stay. Only a raw route reads that array. `{*args}` still fails closed (515). A more specific non-raw route still matches the stripped argv first.

Limits: `--interactive` / `-i` with no other tokens still starts the REPL before user routes when `AddRepl()` is on. `tail -i` reaches the handler. `--json-args` is still peeled; it is transport, not an argument. AOT publishes the same generated `ExecuteRouteAsync`. The REPL test runs that path. This walk does not publish. A raw route combined with another parameter, an option, `--`, or a type constraint is NURU_S011. An invalid fluent pattern is not emitted. An invalid endpoint still emits, with the diagnostic. Specificity stays 10, the same as `{*args}`.

Boy-scout: `package-pin-currency` moved the task 570 proof pin to TimeWarp.Nuru 4.0.0-beta.18. `proof/capture.sh` reran 8 PASS / 0 FAIL and the captures did not change. `source-file-line-cap`: the git, docker, and conditional examples moved to `documentation/user/features/routing-scenarios.md`, so `routing.md` is under 500 lines and its baseline entry is gone. The skill states the raw form in one paragraph and the fluent table; the REPL mode list is one sentence so `SKILL.md` stays at its 557-line baseline. `vscode-window-icon` stays a warning. `mcp-02` still fails 13 lookups for `MCP:endpoint-*` regions that are not in the embedded sample. That failure is not from this diff. `GetSyntax("all")` includes the new raw line and passed.

### How to validate

Smoke: build a one-route app with `Map("tail {**args}")` whose handler joins `string[]` with `|`, then run `tail -n 40 --lines=40`.

Expect: stdout `-n|40|--lines=40`, exit 0. The same app with `Map("{*args}")` and arguments `-n 40` still exits 1 and prints `Unknown option '-n'`.

- `./bin/dev build` — passed, 0 warnings, 0 errors
- `dotnet run --file tests/timewarp-nuru-tests/parser/parser-20-raw-catchall.cs` — 8 passed
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-43-raw-catchall.cs` — 13 passed
- `dotnet run --file tests/timewarp-nuru-tests/generator/generator-81-raw-catchall-diagnostic.cs` — 6 passed
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-37-unknown-options.cs` — 18 passed, file not edited
- `dotnet run --file samples/fluent/03-syntax/fluent-syntax-examples.cs -- --help` — compiled; help lists `fluent-tail`
- `ganda repo audit` — pass, 43 passed, 0 failed, 1 warning (`vscode-window-icon`)

### Review

Round 1 (effort 3, roster: general): 1 bug, 1 suggestion, 1 nit. Fix pass closed all three. M1: group help is skipped when a raw route's literals equal the group prefix, so `raw602-tail --help` and `-h` reach the handler, and a sibling `show --help` still prints its own help. M2: routing-43 runs the endpoint through `.Map<T>()` with the full token set and a fluent `.WithAlias("t")` raw route. `.WithAlias` is the compile-time stub the generator already reads; the public method was missing, so the alias test could not compile. M3: routing.md states that an override on a raw route reaches the handler and configuration, and that an override before the leading literals stops the match. Those two cases are pinned in routing-43. See `review/round-1/merged.md`.

Round 2 (effort 2, roster: general) re-verified the fix delta 1fa4a72e. It read the diff and the recorded results and did not rerun the gates. M1, M2, and M3 are fixed. It raised one new nit, M4: the new public `EndpointBuilder<TBuilder>.WithAlias(string)` turns on fluent route aliases, but the changelog does not name it. M4 is wontfix: the package is beta, the stub is additive, the generator already handled it, and routing-43 covers it. The changelog line goes with the next fluent-alias change.

Final counts: bug 1 fixed; suggestion 1 fixed; nit 1 fixed, 1 wontfix; 0 open. Disposition: **accepted-exceptions**. Files: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.

### Fix pass (review round 1)

### How to validate

Smoke: map a grouped endpoint `[NuruRouteGroup("raw602-tail")]` with `[NuruRoute("")]` and `[Parameter(IsCatchAll = true, IsRaw = true)]`, then run `raw602-tail --help`.

Expect: stdout `--help`, exit 0. The output does not contain `commands:`.

- `./bin/dev build` — passed, 0 warnings, 0 errors
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-43-raw-catchall.cs` — 18 passed
- `ganda repo audit` — pass, 43 passed, 0 failed, 1 warning (`vscode-window-icon`)

## Session

- Created: 549bb8ea-d8c7-4ef7-9b7e-95b0ae556b64 (2026-11-27), from a timewarp-amuru session
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a21d9e-aa2e-7ac2-a734-c29847cd62bf, max-turns 500, Done — 2026-11-27T16:13:46Z
- Review oracle round 1: Claude Opus 5.5 (claude-opus-5-5), session db32ea30-4109-48f3-b176-2a2ff984f511, effort 3, roster general, max-turns 200. Blocked: fix pass needed for open findings M1, M2, M3 (2026-11-27)
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session db32ea30-4109-48f3-b176-2a2ff984f511, max-turns 200, artifacts general.md, merged.md — 2026-11-27T16:17:36Z
- Fix pass (implement-fix-2): closed M1, M2, and M3 — 2026-11-27
- Review oracle round 2: Claude Opus 5.5 (claude-opus-5-5), effort 2, roster general, max-turns 120. Disposition accepted-exceptions (M4 nit wontfix), 0 open, Done (2026-11-27)
- CI red at merge gate (2026-11-27, PR #439, run 46034487255, check `test-multi`): the multi-mode test assembly does not compile. `tests/timewarp-nuru-tests/lexer/lexer-test-helper.cs(17,19)` and `(37,33)`: error CS0433, the types `Lexer` and `Token` exist in both `TimeWarp.Nuru.Analyzers` and `TimeWarp.Nuru`. Master CI is green, so this branch introduced it. Unconfirmed lead: one of the three new test files (`generator-81-raw-catchall-diagnostic.cs`, `parser-20-raw-catchall.cs`, `routing-43-raw-catchall.cs`) brings the analyzers assembly into the CI assembly as a normal reference. Fix pass: reproduce with `ganda runfile cache --clear` then `dotnet run tests/ci-tests/run-ci-tests.cs`, fix the ambiguity (drop the reference, or run that test standalone only behind `#if !JARIBU_MULTI` and add it to `tests/ci-standalone/run-standalone-tests.cs`), and confirm the CI multi-mode run passes before finishing.

## Review

- Round 1: fixed on this branch — M1 group help, M2 endpoint and alias runtime tests, M3 configuration-override docs
- Round 2: Done
## Notes

- First consumer: timewarp-ganda task 731 (`tail`), with the parser in timewarp-amuru task 158.
- Not in scope: a `getopt` mode (attached values `-n40`, bundled flags `-fn 20`, last value
  wins). The raw route makes it unnecessary for command overrides.
- Seen in the same test, not part of this task: a repeated option (`-n 1 -n 2`) puts the second
  `-n` and its value in the catch-all, and a missing option value (`-n` alone) exits 0.
- Background: timewarp-amuru `kanban/done/157-research-head-and-tail-commands-that-save-the-full-stream-for-agents/task.md`.

## Parked

2026-11-27 at merge: PR #439 CI is red; refusing merge. check 'test-multi' (run 46034487255) concluded failure.

No failing command output.
