# Round 1 — general
**Date:** 2026-10-12
**Scope reviewed:** same as framework

## Summary

The change routes redirected stdin to `Terminal.ReadLine()` line mode, catches the `ReadKey`
`InvalidOperationException` in the key reader path (stderr line, exit 1), and threads the session
exit code through `ReplSession.RunAsync`, `NuruApp.RunReplAsync`, the generated `RunReplAsync`, its
interceptor and the `--interactive` / `AutoStartWhenEmpty` branches. Exit code is sticky across EOF,
`exit`, blank lines and built-ins. The `Task` → `Task<int>` change is source-compatible for
`await app.RunReplAsync();` call sites in docs/samples. `TestTerminal` reports not-redirected, so the
existing key-binding REPL tests keep their coverage. Risk is low; no bugs found.

## Issues

### Issue 1 — Severity: suggestion
- File: source/timewarp-nuru/repl/repl-session.cs:286
- Description: `catch (InvalidOperationException)` wraps the whole `ReplConsoleReader.ReadLineAsync`, so an
  `InvalidOperationException` from a key-binding/completion bug inside the reader would be reported as
  "interactive input is unavailable" and the original exception is dropped.
- Suggestion: log the caught exception through `LoggerFactory`, or narrow to the `ReadKey` call.
- Status: open

### Issue 2 — Severity: nit
- File: source/timewarp-nuru/repl/repl-session.cs:296
- Description: in line mode the prompt is still written for each piped line, so piped output contains
  `demo> ` noise.
- Suggestion: optionally suppress the prompt when input is redirected.
- Status: open
