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
- [ ] Alt+U: UpcaseWord - Convert from cursor to end of word to UPPERCASE
- [ ] Alt+L: DowncaseWord - Convert from cursor to end of word to lowercase
- [ ] Alt+C: CapitalizeWord - Uppercase first char, lowercase rest (from cursor)

### Character Operations (IMPLEMENT)
- [ ] Ctrl+T: SwapCharacters - Swap character at cursor with previous character
- [ ] Move cursor forward after swap (Emacs behavior)

### Word Deletion (IMPLEMENT)
- [ ] Alt+D: DeleteWord - Delete from cursor to end of word (same as KillWord but may not use kill ring)
- [ ] Ctrl+Backspace: BackwardDeleteWord - Delete from start of word to cursor
- [ ] Note: These may be aliases for kill operations

### Testing
- [ ] Test case conversion with mixed text
- [ ] Test character swap at line boundaries
- [ ] Test word deletion

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
