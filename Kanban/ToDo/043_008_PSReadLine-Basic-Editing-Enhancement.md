# PSReadLine Basic Editing Enhancement

## Description

Enhance basic editing operations in the Nuru REPL to fully match PSReadLine behavior, including proper delete character handling and line acceptance variations.

## Parent

043_PSReadLine-REPL-Compatibility

## Requirements

- Ctrl+D should have dual behavior (delete char OR exit)
- All basic editing should integrate with undo system
- Maintain backward compatibility with existing behavior

## Checklist

### Character Deletion (VERIFY/ENHANCE)
- [ ] Backspace: BackwardDeleteChar - Delete character before cursor (VERIFY - likely implemented)
- [ ] Ctrl+H: BackwardDeleteChar - Alternative binding (VERIFY)
- [ ] Delete: DeleteChar - Delete character at cursor (VERIFY)
- [ ] Ctrl+D: DeleteCharOrExit - Delete char at cursor, OR exit if line is empty (IMPLEMENT)

### Line Acceptance (VERIFY/ENHANCE)
- [ ] Enter: AcceptLine - Execute current input (VERIFY - implemented)
- [ ] Ctrl+M: AcceptLine - Alternative binding (VERIFY)
- [ ] Ctrl+J: AcceptLine - Alternative binding (newline character) (VERIFY)

### Line Operations (VERIFY/ENHANCE)
- [ ] Escape: CancelLine / Clear current input (VERIFY - implemented)
- [ ] Ctrl+C: CopyOrCancelLine - Copy selection or cancel line (See 043_006)
- [ ] Ctrl+L: ClearScreen - Clear screen and redraw prompt (IMPLEMENT if missing)

### Insert Mode (IMPLEMENT if applicable)
- [ ] Insert: ToggleInsertMode - Toggle between insert and overwrite mode
- [ ] Visual indicator for overwrite mode

### Testing
- [ ] Test Ctrl+D dual behavior
- [ ] Test all alternative keybindings
- [ ] Test ClearScreen

## Notes

### PSReadLine Function Reference
| Function | Description |
|----------|-------------|
| BackwardDeleteChar | Delete character before cursor |
| DeleteChar | Delete character at cursor |
| DeleteCharOrExit | Delete char at cursor, or exit REPL if line empty |
| AcceptLine | Accept the input and execute |
| CancelLine | Cancel the current input |
| ClearScreen | Clear the screen and redraw prompt |
| ToggleInsertMode | Toggle between insert and overwrite modes |

### DeleteCharOrExit Behavior
This is the Unix/bash-style Ctrl+D behavior:
```
Input: "hello" (cursor in middle)
       "hel|lo"
Ctrl+D: "hel|o"  (deletes 'l')

Input: "" (empty line)
       "|"
Ctrl+D: Exit REPL (like typing 'exit')
```

This dual behavior is expected by Unix users. Windows users may find it surprising.

### ClearScreen Behavior
```
Ctrl+L pressed:
1. Clear entire terminal screen
2. Redraw prompt at top
3. Show current input line
4. Position cursor correctly
```

Use ANSI escape sequence: `\e[2J\e[H` (clear screen, cursor home)

### Overwrite Mode
In overwrite mode, typed characters replace characters at cursor instead of inserting:
```
Insert mode (default):
  "he|llo" + type 'X' = "heX|llo"

Overwrite mode:
  "he|llo" + type 'X' = "heX|lo"
```

### Existing Implementation Status
Based on CLAUDE.md, these are likely implemented:
- Backspace (BackwardDeleteChar)
- Delete key
- Enter (AcceptLine)
- Escape (clear/abort)
- Ctrl+D (exit only - needs enhancement)

### Implementation Complexity
- Low complexity for most items
- DeleteCharOrExit requires state check
- ClearScreen requires terminal control
- Overwrite mode requires input handling changes
