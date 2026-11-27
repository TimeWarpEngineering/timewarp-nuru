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

A command override needs a route that stops interpreting arguments. `{*args}` stays fail-closed: an option-shaped token such as `-n` is still an unknown option, and `--key=value` is still stripped before routing. A second marker, `{**args}`, is the opt-out. It is written in the pattern so help and capabilities show it. The endpoint form is `[Parameter(IsCatchAll = true, IsRaw = true)]` because `[NuruRoute]` only accepts a literal.

The handler gets a `string[]` of every token after the route's leading literals, in order and unchanged. That includes `-n`, `-40`, `+5`, `--`, `--lines=40`, a lone `-`, and empty strings. `--help` and `--version` are arguments on a raw route, not Nuru's own commands. A raw route with a leading literal still leaves `myapp --help` to Nuru. A raw route may have leading literals or none. It cannot also declare options, other parameters, `--`, or a type constraint; that is build error NURU_S011. The same generated matcher runs under AOT and in the REPL. `--interactive` / `-i` still starts the REPL before user routes, and `--json-args` is still peeled as transport.

Done when the parser accepts `{**args}`, the generated matcher skips unknown-option checks and configuration stripping for that route only, `--help` and `--version` reach the handler, NURU_S011 fires for a raw catch-all that is not alone, the task 515 `{*args}` tests stay green, and routing.md, the tw-nuru skill, and the changelog describe the raw form.

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

## Session

- Created: 549bb8ea-d8c7-4ef7-9b7e-95b0ae556b64 (2026-11-27), from a timewarp-amuru session

## Notes

- First consumer: timewarp-ganda task 731 (`tail`), with the parser in timewarp-amuru task 158.
- Not in scope: a `getopt` mode (attached values `-n40`, bundled flags `-fn 20`, last value
  wins). The raw route makes it unnecessary for command overrides.
- Seen in the same test, not part of this task: a repeated option (`-n 1 -n 2`) puts the second
  `-n` and its value in the catch-all, and a missing option value (`-n` alone) exits 0.
- Background: timewarp-amuru `kanban/done/157-research-head-and-tail-commands-that-save-the-full-stream-for-agents/task.md`.
