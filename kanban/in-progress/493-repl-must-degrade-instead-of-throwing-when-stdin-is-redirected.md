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

- [x] Line-mode fallback when input is redirected
- [x] No unhandled exception path out of `RunReplAsync`
- [x] REPL test for piped input
- [x] `changelog.md` Unreleased: Fixed entry
- [ ] PR merged

## Notes

- Found by task 487 (2026-10-09) while probing the launch demo. The REPL test files under `tests/timewarp-nuru-tests/repl/` already use `TestTerminal`; follow that pattern.

## Session

- Created: claude 2412bd45 (2026-10-09)
- Implemented: claude sub-agent impl-493 (2026-10-11)

## Results

**Fix** (`source/timewarp-nuru/repl/repl-session.cs`): `ReadCommandInputAsync` uses the key-driven reader only when `EnableArrowHistory && !Terminal.IsInputRedirected`; otherwise prompt + `Terminal.ReadLine()`. `null` (EOF) goes through the existing EOF path (newline, stop loop, Goodbye prints). An `InvalidOperationException` from the key reader writes one stderr line (`REPL: interactive input is unavailable (console input is redirected or absent).`), sets `Running = false`, and the session result is 1.

**Exit-code propagation:** `ReplSession.RunAsync`, `NuruApp.RunReplAsync()`, the generated `RunReplAsync` and its interceptor now return `Task<int>`. The `--interactive` and `AutoStartWhenEmpty` branches return that value instead of 0. The value is the last command's exit code (0 after clean EOF or `exit`), or 1 when the console is unusable. Tests assert 1 for both `app.RunAsync(["--interactive"])` and `app.RunReplAsync()` in the unusable-console case.

**Test** `tests/timewarp-nuru-tests/repl/repl-49-redirected-input.cs` (renumbered from repl-29, picked up by the `../timewarp-nuru-tests/**/*.cs` glob in `tests/ci-tests/Directory.Build.props`, line 94; it ran in CI). `TestTerminal.ReadKey` never throws (it falls back to ReadLine, then Ctrl+D), so against plain `TestTerminal` the tests pass even without the fix. The tests therefore use a small `ITerminal` wrapper whose `ReadKey` throws `InvalidOperationException` like `Console.ReadKey`. Added two tests for the unusable-console guard (stderr line and exit code 1 via RunAsync and RunReplAsync).

```
dotnet run tests/timewarp-nuru-tests/repl/repl-49-redirected-input.cs
```
- Before fix (source reverted): Total 4, Passed 1, Failed 3, `InvalidOperationException: Cannot read keys when console input has been redirected.`
- After fix: Total 4, Passed 4.

**Real process** (`samples/fluent/10-repl/fluent-repl-basic.cs`):
```
$ printf 'greet x
exit
' | dotnet run samples/fluent/10-repl/fluent-repl-basic.cs -- --interactive     # BEFORE
... unhandled InvalidOperationException stack trace ...   exit=134
$ printf 'greet x
exit
' | dotnet run <sample> -- --interactive                                       # AFTER
Welcome to Interactive Mode!
demo> Hello, x!
demo> Goodbye!                                                                                         exit=0
$ printf 'greet x
' | dotnet run <sample> -- --interactive
demo> Hello, x!
demo>
Goodbye!                                                                                               exit=0
$ dotnet run <sample> -- --interactive < /dev/null
demo>
Goodbye!                                                                                               exit=0
```

**REPL tests** (after `ganda runfile cache --clear`, run individually): repl-01 11/11, repl-05 10/10, repl-09 8/8, repl-10 8/8, repl-13 8/8, repl-15 8/8, repl-49-redirected-input 4/4.

**CI** `dotnet run tests/ci-tests/run-ci-tests.cs` (cache cleared first), summed per-file tables: Total 3864, Passed 3852, Skipped 12, Failed 0 (re-run after exit-code propagation), no FAILED lines.

**Build** `bin/dev build`: 0 Warning(s), 0 Error(s).

**Exit-code re-verification** (sample `fluent-repl-basic.cs`, piped): `greet x`+`exit`, `greet x` only, and `< /dev/null` all exit 0. `printf 'nope\n' | ... --interactive` prints `Unknown command. Use --help for usage.` and exits 0, because the final EOF read resets the last code to 0. `repl-18` 25/25, `repl-42` 6/6, `repl-48` 1/1.

### Exit code is sticky across EOF and built-ins (2026-10-11, reviewer)

Only executed route commands (and an unusable console) set the session exit code. EOF, `exit`/`quit`,
blank lines, and built-ins leave it alone, so a piped script whose last command failed exits non-zero.

```
$ printf 'greet x\nnope\n' | dotnet run samples/fluent/10-repl/fluent-repl-basic.cs -- --interactive; echo exit=$?
... Hello, x! ... Unknown command. Use --help for usage. ... Goodbye!
exit=1
$ printf 'greet x\nexit\n' | dotnet run samples/fluent/10-repl/fluent-repl-basic.cs -- --interactive; echo exit=$?
... Hello, x! ... Goodbye!
exit=0
```

Test: `Should_keep_last_command_exit_code_when_eof_follows_a_failure` in `repl-49-redirected-input.cs` (6/6).


- Reviewed + exit-code follow-up: claude (2026-10-11)

