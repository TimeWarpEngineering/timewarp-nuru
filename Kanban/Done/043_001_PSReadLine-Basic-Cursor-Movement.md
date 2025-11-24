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
- [x] Add `Dictionary<(ConsoleKey, ConsoleModifiers), Func<bool>>` KeyBindings field to ReplConsoleReader
- [x] Replace switch statement with KeyBindings lookup
- [x] Initialize default keybindings in constructor

### Character Movement
- [x] LeftArrow: BackwardChar - Move cursor back one character
- [x] Ctrl+B: BackwardChar - Alternative binding
- [x] RightArrow: ForwardChar - Move cursor forward one character
- [x] Ctrl+F: ForwardChar - Alternative binding

### Line Position
- [x] Home: BeginningOfLine - Move to beginning of line
- [x] Ctrl+A: BeginningOfLine - Alternative binding
- [x] End: EndOfLine - Move to end of line
- [x] Ctrl+E: EndOfLine - Alternative binding

### Word Movement
- [x] Ctrl+LeftArrow: BackwardWord - Move to beginning of previous word
- [x] Alt+B: BackwardWord - Alternative binding
- [x] Ctrl+RightArrow: ForwardWord - Move to END of current/next word (FIXED)
- [x] Alt+F: ForwardWord - Alternative binding

### History Navigation
- [x] Ctrl+P: PreviousHistory - Alternative binding
- [x] Ctrl+N: NextHistory - Alternative binding

### Testing
- [x] Add tests for each keybinding (repl-18-psreadline-keybindings.cs - 18 tests)
- [x] Verify behavior matches PSReadLine

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
