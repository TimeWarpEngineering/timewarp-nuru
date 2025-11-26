# Fix Clear Command To Use ITerminal

## Description

The `clear` and `cls` REPL commands call `Console.Clear()` directly instead of using the `ITerminal` interface, causing tests to fail. TestTerminal doesn't receive anything because `Console.Clear()` bypasses it entirely.

## Requirements

1. Add `Clear()` method to `ITerminal` interface
2. Implement `Clear()` in TestTerminal to write `[CLEAR]` to output for testing verification
3. Implement `Clear()` in SystemTerminal to call `Console.Clear()` 
4. Change REPL routes to use Terminal instead of `Console.Clear()` directly

## Checklist

- [x] Add `void Clear()` to ITerminal interface (already existed)
- [x] Implement Clear() in TestTerminal - write `[CLEAR]` to OutputWriter (already existed)
- [x] Implement Clear() in SystemTerminal - call Console.Clear() (already existed)
- [x] Update clear/cls routes in NuruAppExtensions to use Terminal.Clear()
- [x] Verify repl-09-builtin-commands.cs tests pass (6/8 -> 8/8)

## Notes

- This is why tests `Should_handle_clear_command` and `Should_handle_cls_command` fail in repl-09-builtin-commands.cs
- The tests expect `[CLEAR]` in output but Console.Clear() bypasses TestTerminal entirely
- Standard pattern: all I/O should go through ITerminal abstraction for testability

## Implementation Notes

Current broken code in `Source/TimeWarp.Nuru.Repl/NuruAppExtensions.cs`:
```csharp
.AddRoute("clear", () => Console.Clear(), "Clear the screen")
.AddRoute("cls", () => Console.Clear(), "Clear the screen")
```

The route handlers need access to the Terminal. Options:
1. Use `ReplSession.CurrentSession?.Terminal.Clear()` if Terminal is accessible
2. Or pass Terminal through a different mechanism
