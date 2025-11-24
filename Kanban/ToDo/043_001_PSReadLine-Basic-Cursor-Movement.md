# PSReadLine Basic Cursor Movement

## Description

Implement and verify PSReadLine-compatible cursor movement keybindings in the Nuru REPL. This includes character-by-character movement, word-based movement, and line position jumps.

## Parent

043_PSReadLine-REPL-Compatibility

## Requirements

- All cursor movement should respect line boundaries
- Word movement should handle various word delimiters (spaces, punctuation)
- ForwardWord should move to the END of the current/next word (PSReadLine behavior)

## Checklist

### Character Movement
- [ ] LeftArrow: BackwardChar - Move cursor back one character (VERIFY - likely implemented)
- [ ] Ctrl+B: BackwardChar - Alternative binding (VERIFY)
- [ ] RightArrow: ForwardChar - Move cursor forward one character (VERIFY - likely implemented)
- [ ] Ctrl+F: ForwardChar - Alternative binding (VERIFY)

### Line Position
- [ ] Home: BeginningOfLine - Move to beginning of line (VERIFY - likely implemented)
- [ ] Ctrl+A: BeginningOfLine - Alternative binding (VERIFY)
- [ ] End: EndOfLine - Move to end of line (VERIFY - likely implemented)
- [ ] Ctrl+E: EndOfLine - Alternative binding (VERIFY)

### Word Movement
- [ ] Ctrl+LeftArrow: BackwardWord - Move to beginning of previous word (VERIFY)
- [ ] Alt+B: BackwardWord - Alternative binding (IMPLEMENT if missing)
- [ ] Ctrl+RightArrow: ForwardWord - Move to END of current/next word (FIX - goes to START)
- [ ] Alt+F: ForwardWord - Alternative binding (IMPLEMENT/FIX if needed)

### Testing
- [ ] Add tests for each keybinding
- [ ] Verify behavior matches PSReadLine

## Notes

### PSReadLine Function Reference
| Function | Description |
|----------|-------------|
| BackwardChar | Move cursor one character to the left |
| ForwardChar | Move cursor one character to the right |
| BeginningOfLine | Move cursor to the beginning of the line |
| EndOfLine | Move cursor to the end of the line |
| BackwardWord | Move cursor to the start of the previous word |
| ForwardWord | Move cursor to the end of the current/next word |

### Known Issue
The user noted that ForwardWord (Ctrl+RightArrow) goes to the START of the next word instead of the END. PSReadLine's ForwardWord moves to the END of the current word, or if at the end of a word, to the END of the next word.

### Implementation Location
- Input handling: `Source/TimeWarp.Nuru.Repl/Input/`
- Key processing likely in a ConsoleKeyInfo handler
