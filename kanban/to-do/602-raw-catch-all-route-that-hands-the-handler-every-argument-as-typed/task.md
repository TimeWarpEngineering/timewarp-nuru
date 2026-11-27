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

## Checklist

- [ ] Route syntax and parser support for the raw marker
- [ ] Generator emits a matcher that skips unknown-option checks for a raw route
- [ ] Configuration-override stripping skips raw routes
- [ ] `--help` / `--version` pass through on a raw route
- [ ] Analyzer diagnostic for a raw catch-all combined with options or parameters
- [ ] Tests: each token kind above arrives unchanged; default route and literal-prefixed route
- [ ] Tests: existing `{*args}` fail-closed behaviour unchanged
- [ ] `documentation/user/features/routing.md` and the `tw-nuru` skill describe the raw form
- [ ] Changelog entry

## Session

- Created: 549bb8ea-d8c7-4ef7-9b7e-95b0ae556b64 (2026-11-27), from a timewarp-amuru session

## Notes

- First consumer: timewarp-ganda task 731 (`tail`), with the parser in timewarp-amuru task 158.
- Not in scope: a `getopt` mode (attached values `-n40`, bundled flags `-fn 20`, last value
  wins). The raw route makes it unnecessary for command overrides.
- Seen in the same test, not part of this task: a repeated option (`-n 1 -n 2`) puts the second
  `-n` and its value in the catch-all, and a missing option value (`-n` alone) exits 0.
- Background: timewarp-amuru `kanban/done/157-research-head-and-tail-commands-that-save-the-full-stream-for-agents/task.md`.
