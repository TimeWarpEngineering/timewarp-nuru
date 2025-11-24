# PSReadLine Basic Cursor Movement

## Description

Implement and verify PSReadLine-compatible cursor movement keybindings in the Nuru REPL. This includes character-by-character movement, word-based movement, and line position jumps.

**Prerequisites:** Refactor `ReplConsoleReader` to use a KeyBindings dictionary instead of switch statement.

## Parent

043_PSReadLine-REPL-Compatibility

## Requirements

- All cursor movement should respect line boundaries
- Word movement should handle various word delimiters (spaces, punctuation)
- ForwardWord should move to the END of the current/next word (PSReadLine behavior)

## Checklist

### Infrastructure
- [ ] Add `Dictionary<(ConsoleKey, ConsoleModifiers), Action>` KeyBindings field to ReplConsoleReader
- [ ] Replace switch statement with KeyBindings lookup
- [ ] Initialize default keybindings in constructor

### Character Movement
- [ ] LeftArrow: BackwardChar - Move cursor back one character (IMPLEMENTED)
- [ ] Ctrl+B: BackwardChar - Alternative binding (ADD)
- [ ] RightArrow: ForwardChar - Move cursor forward one character (IMPLEMENTED)
- [ ] Ctrl+F: ForwardChar - Alternative binding (ADD)

### Line Position
- [ ] Home: BeginningOfLine - Move to beginning of line (IMPLEMENTED)
- [ ] Ctrl+A: BeginningOfLine - Alternative binding (ADD)
- [ ] End: EndOfLine - Move to end of line (IMPLEMENTED)
- [ ] Ctrl+E: EndOfLine - Alternative binding (ADD)

### Word Movement
- [ ] Ctrl+LeftArrow: BackwardWord - Move to beginning of previous word (IMPLEMENTED)
- [ ] Alt+B: BackwardWord - Alternative binding (ADD)
- [ ] Ctrl+RightArrow: ForwardWord - Move to END of current/next word (FIX - goes to START)
- [ ] Alt+F: ForwardWord - Alternative binding (ADD after fix)

### Testing
- [ ] Add tests for each keybinding
- [ ] Verify behavior matches PSReadLine

## Notes

### KeyBindings Dictionary Pattern

Replace the current switch statement:
```csharp
// Before (switch statement)
switch (keyInfo.Key)
{
  case ConsoleKey.LeftArrow:
    HandleLeftArrow(keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control));
    break;
  // ... 15+ more cases
}

// After (dictionary lookup)
private readonly Dictionary<(ConsoleKey Key, ConsoleModifiers Mods), Action> KeyBindings = new();

// In constructor:
KeyBindings[(ConsoleKey.LeftArrow, ConsoleModifiers.None)] = () => HandleBackwardChar();
KeyBindings[(ConsoleKey.LeftArrow, ConsoleModifiers.Control)] = () => HandleBackwardWord();
KeyBindings[(ConsoleKey.B, ConsoleModifiers.Control)] = () => HandleBackwardChar();
// etc.

// In ReadLine loop:
var key = (keyInfo.Key, keyInfo.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt | ConsoleModifiers.Shift));
if (KeyBindings.TryGetValue(key, out var handler))
  handler();
else if (!char.IsControl(keyInfo.KeyChar))
  HandleCharacter(keyInfo.KeyChar);
```

### PSReadLine Function Reference

| Function | Description |
|----------|-------------|
| BackwardChar | Move cursor one character to the left |
| ForwardChar | Move cursor one character to the right |
| BeginningOfLine | Move cursor to the beginning of the line |
| EndOfLine | Move cursor to the end of the line |
| BackwardWord | Move cursor to the start of the previous word |
| ForwardWord | Move cursor to the end of the current/next word |

### Known Issue: ForwardWord Behavior

The ForwardWord (Ctrl+RightArrow) goes to the START of the next word instead of the END.

PSReadLine's ForwardWord behavior:
- If cursor is in a word, move to the END of that word
- If cursor is at the end of a word (or in whitespace), move to the END of the next word

Current implementation moves to START of next word (like some editors, but not PSReadLine).

### Implementation Location

- Input handling: `Source/TimeWarp.Nuru.Repl/Input/ReplConsoleReader.cs`
- Tests: `Tests/TimeWarp.Nuru.Repl.Tests/`
