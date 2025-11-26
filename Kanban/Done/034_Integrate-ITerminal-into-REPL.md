# 034: Integrate ITerminal into REPL

## Description

Refactor the REPL to use `ITerminal` instead of direct `System.Console` calls, enabling deterministic automated testing of all REPL features including arrow key navigation, tab completion, and history.

Task 033 (IConsole/ITerminal abstractions) is complete. We now have:
- `IConsole` - basic I/O abstraction
- `ITerminal : IConsole` - interactive terminal with `ReadKey`, cursor control, `WindowWidth`, etc.
- `TestTerminal` - test implementation with key queue for deterministic testing
- `NuruTerminal` - production implementation wrapping System.Console

## Requirements

1. ~~Add `ITerminal? Terminal` property to `ReplOptions`~~ **Changed:** Use builder DI pattern instead
2. Refactor `ReplSession` to use injected `ITerminal` for all Console operations
3. Refactor `ReplConsoleReader` to use injected `ITerminal`
4. Maintain 100% backward compatibility (existing code without ITerminal works unchanged)
5. Build with 0 warnings, 0 errors

## Checklist

### Implementation
- [x] Add `ITerminal? Terminal` field to `NuruAppBuilder`
- [x] Add `UseTerminal(ITerminal)` method to `NuruAppBuilder`
- [x] Add `ITerminal Terminal` property to `NuruApp`
- [x] Make `NuruApp` direct constructor `internal` (only builder should construct)
- [x] Update `ReplSession` to get `ITerminal` from `NuruApp.Terminal`
- [x] Replace all `Console.*` calls in `ReplSession` with `Terminal.*` calls
- [x] Update `ReplConsoleReader` constructor to accept `ITerminal`
- [x] Replace all `Console.*` calls in `ReplConsoleReader` with `Terminal.*` calls

### Verification
- [x] Build succeeds with 0 warnings, 0 errors
- [x] Automated REPL tests pass (52 total tests)
  - Session Lifecycle: 11/11
  - History Management: 8/8
  - History Security: 14/14
  - CommandLineParser Basic: 8/8
  - CommandLineParser Quotes: 11/11
- [ ] Verify existing REPL samples still work (requires manual interactive testing)

## Implementation Notes

**Design Decision:** ITerminal is injected via builder pattern, not ReplOptions.
- Options/configuration classes should contain data/settings, not service implementations
- `NuruAppBuilder.UseTerminal(ITerminal)` provides the terminal
- `NuruApp.Terminal` property exposes it to REPL components
- Defaults to `NuruTerminal.Default` when not specified

**Usage for testing:**
```csharp
var terminal = new TestTerminal();
terminal.QueueLine("help");
terminal.QueueLine("exit");

var app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .AddReplSupport()
    .Build();

await app.RunReplAsync();
Assert.Contains("REPL Commands", terminal.Output);
```

**Files Modified:**
- `NuruAppBuilder.cs` - Added Terminal field, UseTerminal() method, DI registration
- `NuruApp.cs` - Added Terminal property, made direct constructor internal
- `ReplSession.cs` - Uses NuruApp.Terminal instead of System.Console
- `ReplConsoleReader.cs` - Accepts ITerminal in constructor
- `ReplOptions.cs` - Added `clear-history` to default HistoryIgnorePatterns

**Additional Fix:**
- `clear-history` command no longer adds itself to history (added to default ignore patterns)
- This prevents confusing behavior where `history` shows `clear-history` after clearing

**Test Files Created:**
- `Tests/TimeWarp.Nuru.Repl.Tests/repl-01-session-lifecycle.cs`
- `Tests/TimeWarp.Nuru.Repl.Tests/repl-03-history-management.cs`
- `Tests/TimeWarp.Nuru.Repl.Tests/repl-03b-history-security.cs`
- `Tests/TimeWarp.Nuru.Repl.Tests/CommandLineParser/parser-01-basic-parsing.cs`
- `Tests/TimeWarp.Nuru.Repl.Tests/CommandLineParser/parser-02-quoted-strings.cs`

## Notes

- This replaces Task 032's proposed `IReplIO` interface - `ITerminal` already provides all needed functionality
- Task 032 can be closed/superseded once this task is complete
- After completion, REPL tests can use `TestTerminal` for full automated testing per `repl-test-plan.md`
