# 036 Implement Ctrl+D Exit In REPL

## Description

ReplConsoleReader.ReadLine() doesn't handle Ctrl+D (EOF), causing TestTerminal-driven tests to hang indefinitely when input is exhausted. Ctrl+D is the standard Unix way to exit a REPL (Python, Node, bash, etc.) and should be supported.

## Requirements

1. Change `ReplConsoleReader.ReadLine()` return type from `string` to `string?`
2. Add handling for Ctrl+D in the ReadLine switch statement to return `null`
3. The existing code in `ReplSession.ReadCommandInput()` already handles `null` return correctly - sets `Running = false` and exits gracefully

## Checklist

- [ ] Change ReadLine return type to string?
- [ ] Add Ctrl+D case in switch statement
- [ ] Verify ReplSession.ReadCommandInput() handles null correctly (it already does)
- [ ] Update any callers if needed
- [ ] Remove [Skip] attributes from tab completion tests (repl-06, repl-07, repl-12, repl-14)
- [ ] Verify tests pass

## Implementation Notes

In `Source/TimeWarp.Nuru.Repl/Input/ReplConsoleReader.cs`, in the `ReadLine` method's switch statement, add:

```csharp
case ConsoleKey.D when keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control):
  Terminal.WriteLine();
  return null;
```

And change the method signature from:
```csharp
public string ReadLine(string prompt)
```
to:
```csharp
public string? ReadLine(string prompt)
```

## Notes

- This also fixes TestTerminal-driven tests that hang when input is exhausted
- TestTerminal returns Ctrl+D when its key queue is empty, simulating EOF
- Standard Unix REPL behavior - users expect Ctrl+D to exit
