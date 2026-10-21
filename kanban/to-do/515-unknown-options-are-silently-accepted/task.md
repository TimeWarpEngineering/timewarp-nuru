# Unknown options are silently accepted

## Description

TimeWarp.Nuru's generated matcher treats an unrecognized `--flag` as another positional. A route that already has options does not check leftover positionals, so the handler runs and the flag disappears (exit 0). Routes with only literals and required parameters still reject the extra token as an unknown command.

That is `route-matcher-emitter.cs`: routes with options skip the exact-length check "because unknown options become extra positionals." `Should_ignore_undefined_options` in `tests/timewarp-nuru-tests/routing/routing-13-negative-numbers.cs` records this as V2 behavior (V1 failed).

It is dangerous for mutating commands. On 2026-10-20 `ganda kanban create "Title" --repo timewarp-architecture` (no such option) reserve-and-claimed in the current repo. Ganda task 439 now rejects unknown options before `RunAsync`. This task is the matcher fix. The ganda guard can stay; it becomes redundant once Nuru fails closed.

## Checklist

- [ ] Unknown options fail with a non-zero exit and name the option
- [ ] Suggest the closest declared form when one is near
- [ ] Replace `Should_ignore_undefined_options` with a failure test
- [ ] Negative numbers (`-5`, `-1.5e10`) stay positionals
- [ ] Tokens after `--` stay positionals, including ones that look like options
- [ ] Catch-all routes still accept real positionals and still reject unknown options before `--`
- [ ] Routes with no options keep a clear unknown-command error when the extra token is not an option
- [ ] REPL dispatch rejects unknown options the same way (ganda's outer guard never sees a REPL line)

## Session

- Created: 3127778 (2026-10-21)

## Notes

Filed from timewarp-ganda task 439. Do not add `--repo` to ganda `kanban create` here. Ganda's message for that case points at `kanban reserve --repo` then `kanban claim <id> --repo`.
