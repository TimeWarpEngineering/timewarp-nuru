# REPL must degrade instead of throwing when stdin is redirected

## Description

**3.0.0 launch gate #6.** With input piped, the REPL crashes with an unhandled exception:

```
$ printf 'greet x\nexit\n' | app --interactive
TimeWarp.Nuru REPL Mode. Type 'help' for commands, 'exit' to quit.
> Unhandled exception. System.InvalidOperationException: Cannot read keys when either application does not
  have a console or when console input has been redirected. Try Console.Read.
   at TimeWarp.Nuru.ReplConsoleReader.<ReadLineAsync>d__48.MoveNext()
```

`source/timewarp-nuru/repl/input/repl-console-reader.cs:137` calls `Terminal.ReadKey(true)` unconditionally.
Agents, CI, and `expect`-style tests pipe input; the 3.0 story is "CLIs your agents can call", so the REPL
must not be the one surface that throws on a pipe.

## Requirements

- Detect `Console.IsInputRedirected` (via `ITerminal`, so `TestTerminal` can simulate it) before entering the
  key-driven reader. When redirected: read whole lines with `ReadLine`, execute them in order, no key bindings,
  no history navigation, exit cleanly at EOF with exit code 0 (or the last command's code).
- The exception never escapes `RunReplAsync`; if a terminal is genuinely unusable, print one line to stderr and
  return a non-zero exit code.
- Test with `TestTerminal` driving redirected input: two commands then EOF; assert outputs and exit code.

## Checklist

- [ ] Line-mode fallback when input is redirected
- [ ] No unhandled exception path out of `RunReplAsync`
- [ ] REPL test for piped input
- [ ] `changelog.md` Unreleased: Fixed entry
- [ ] PR merged

## Notes

- Found by task 487 (2026-10-09) while probing the launch demo. The REPL test files under `tests/timewarp-nuru-tests/repl/` already use `TestTerminal`; follow that pattern.

## Session

- Created: claude 2412bd45 (2026-10-09)
