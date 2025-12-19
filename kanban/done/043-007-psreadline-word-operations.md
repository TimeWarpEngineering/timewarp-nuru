# PSReadLine Word Operations

## Description

Implement word manipulation operations in the Nuru REPL, including case conversion (uppercase, lowercase, capitalize) and character transposition.

## Parent

043_PSReadLine-REPL-Compatibility

## Requirements

- Word operations should work from cursor position to end of word
- Case operations should not move cursor unexpectedly
- Operations should integrate with undo system

## Checklist

### Case Conversion (IMPLEMENT)
- [x] Alt+U: UpcaseWord - Convert from cursor to end of word to UPPERCASE
- [x] Alt+L: DowncaseWord - Convert from cursor to end of word to lowercase
- [x] Alt+C: CapitalizeWord - Uppercase first char, lowercase rest (from cursor)

### Character Operations (IMPLEMENT)
- [x] Ctrl+T: SwapCharacters - Swap character at cursor with previous character
- [x] Move cursor forward after swap (Emacs behavior)

### Word Deletion (IMPLEMENT)
- [x] Alt+D: DeleteWord - Delete from cursor to end of word (same as KillWord but may not use kill ring)
- [x] Ctrl+Backspace: BackwardDeleteWord - Delete from start of word to cursor
- [x] Note: These may be aliases for kill operations

### Testing
- [x] Test case conversion with mixed text
- [x] Test character swap at line boundaries
- [x] Test word deletion

## Notes

### PSReadLine Function Reference
| Function | Description |
|----------|-------------|
| UpcaseWord | Convert characters from cursor to end of word to uppercase |
| DowncaseWord | Convert characters from cursor to end of word to lowercase |
| CapitalizeWord | Capitalize first character after cursor, lowercase rest of word |
| SwapCharacters | Swap current character with the one before it |
| DeleteWord | Delete from cursor to end of word |
| BackwardDeleteWord | Delete from start of current word to cursor |

### Case Conversion Examples
```
Input: "hello WORLD" (cursor at 'e')
       "h|ello WORLD"

Alt+U (UpcaseWord):
       "hELLO| WORLD"  (cursor moves to end of word)

Input: "HELLO world" (cursor at 'E')
       "H|ELLO world"

Alt+L (DowncaseWord):
       "Hello| world"  (cursor moves to end of word)

Input: "hello world" (cursor at 'h')
       "|hello world"

Alt+C (CapitalizeWord):
       "Hello| world"  (cursor moves to end of word)
```

### SwapCharacters Behavior
```
Input: "teh" (cursor after 'e')
       "te|h"

Ctrl+T:
       "the|"  (swapped 'e' and 'h', cursor moved forward)

Edge case - cursor at beginning:
       "|teh"
Ctrl+T:
       "t|eh"  (swaps first two chars, cursor after them)
```

### Delete vs Kill
- Delete operations may not add to kill ring (implementation choice)
- Kill operations always add to kill ring
- PSReadLine blurs this distinction somewhat
- Consider making DeleteWord an alias for KillWord for simplicity

### Implementation Complexity
- Low-Medium complexity
- String manipulation is straightforward
- Main complexity is cursor positioning after operations
- Integrate with undo stack

## Results

### Implementation Summary
- Created `repl-console-reader.word-operations.cs` partial class
- Implemented case conversion (upcase, downcase, capitalize) 
- Implemented character transposition (swap)
- Implemented backward word deletion
- All operations integrate with undo system

### Key Bindings Added
| Binding | Action | Profiles |
|---------|--------|----------|
| Alt+U | UpcaseWord | Default, Emacs |
| Alt+L | DowncaseWord | Default, Emacs |
| Alt+C | CapitalizeWord | Default, Emacs |
| Ctrl+T | SwapCharacters | Default, Emacs |
| Ctrl+Backspace | BackwardDeleteWord | Default, Emacs, VSCode |

### Files Created/Modified
- NEW: `source/timewarp-nuru-repl/input/repl-console-reader.word-operations.cs`
- NEW: `tests/timewarp-nuru-repl-tests/repl-29-word-operations.cs` (16 tests)
- MODIFIED: `default-key-binding-profile.cs`
- MODIFIED: `emacs-key-binding-profile.cs`

### Test Results
- 16 tests in `repl-29-word-operations.cs` - all pass

### Commit
`feat(repl): implement PSReadLine word operations` (5a82219)
