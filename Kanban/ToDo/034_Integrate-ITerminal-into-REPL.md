# 034: Integrate ITerminal into REPL

## Description

Refactor the REPL to use `ITerminal` instead of direct `System.Console` calls, enabling deterministic automated testing of all REPL features including arrow key navigation, tab completion, and history.

Task 033 (IConsole/ITerminal abstractions) is complete. We now have:
- `IConsole` - basic I/O abstraction
- `ITerminal : IConsole` - interactive terminal with `ReadKey`, cursor control, `WindowWidth`, etc.
- `TestTerminal` - test implementation with key queue for deterministic testing
- `NuruTerminal` - production implementation wrapping System.Console

The REPL (`ReplSession.cs`, `ReplConsoleReader.cs`) still uses `System.Console` directly, preventing automated testing of interactive features.

## Requirements

1. Add `ITerminal? Terminal` property to `ReplOptions` (defaults to `NuruTerminal.Default` when null)
2. Refactor `ReplSession` to use injected `ITerminal` for all Console operations:
   - `Console.WriteLine()` -> `terminal.WriteLine()`
   - `Console.Write()` -> `terminal.Write()`
   - `Console.CancelKeyPress` handling
3. Refactor `ReplConsoleReader` to use injected `ITerminal`:
   - `Console.ReadKey()` -> `terminal.ReadKey()`
   - `Console.SetCursorPosition()` -> `terminal.SetCursorPosition()`
   - `Console.WindowWidth` -> `terminal.WindowWidth`
4. Maintain 100% backward compatibility (existing code without ITerminal works unchanged)
5. Build with 0 warnings, 0 errors

## Checklist

### Implementation
- [ ] Add `ITerminal?` property to `ReplOptions`
- [ ] Update `ReplSession` constructor to accept/store `ITerminal`
- [ ] Replace all `Console.*` calls in `ReplSession` with `ITerminal` calls
- [ ] Update `ReplConsoleReader` constructor to accept `ITerminal`
- [ ] Replace all `Console.*` calls in `ReplConsoleReader` with `ITerminal` calls

### Verification
- [ ] Build succeeds with 0 warnings, 0 errors
- [ ] Verify existing REPL samples still work

## Notes

- This replaces Task 032's proposed `IReplIO` interface - `ITerminal` already provides all needed functionality
- Task 032 can be closed/superseded once this task is complete
- After completion, REPL tests can use `TestTerminal` for full automated testing per `repl-test-plan.md`
