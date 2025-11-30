# PSReadLine Undo/Redo

## Description

Implement undo/redo functionality in the Nuru REPL. This allows users to reverse editing actions and restore previous states of their input line.

## Parent

043_PSReadLine-REPL-Compatibility

## Requirements

- Undo stack should track all editing operations
- Each "undo unit" should represent a logical edit (not individual keystrokes where possible)
- Undo should restore cursor position as well as text

## Checklist

### Undo Infrastructure (IMPLEMENT)
- [ ] Create UndoStack class to track edit history
- [ ] Define UndoUnit to store text state and cursor position
- [ ] Implement undo grouping for related edits

### Undo Commands (IMPLEMENT)
- [ ] Ctrl+_ (Ctrl+Underscore): Undo - Undo last edit
- [ ] Ctrl+Z: Undo - Alternative binding (common expectation)
- [ ] Multiple undo presses continue undoing

### Redo Commands (IMPLEMENT)
- [ ] Ctrl+Shift+Z: Redo - Redo an undone edit (if available)
- [ ] Alt+Shift+_ : Redo - Alternative binding
- [ ] Note: Ctrl+Y conflicts with Yank, so avoid for Redo

### Revert Line (IMPLEMENT)
- [ ] Alt+R: RevertLine - Undo ALL changes to current line (reset to original)
- [ ] Escape,R: RevertLine - Alternative binding

### Undo Boundaries (IMPLEMENT)
- [ ] Group related edits into single undo unit (e.g., typing a word)
- [ ] Commands like KillLine, Yank are single undo units
- [ ] Movement commands don't create undo entries

### Testing
- [ ] Test undo/redo stack behavior
- [ ] Test undo after various edit types
- [ ] Test RevertLine from different states

## Notes

### PSReadLine Function Reference
| Function | Description |
|----------|-------------|
| Undo | Undo a previous edit |
| Redo | Redo an edit that was undone |
| RevertLine | Undo all edits to the current line |

### Undo Stack Behavior
```
Initial: "hello"
Type " world": stack = [("hello", 5)]
Ctrl+K: stack = [("hello", 5), ("hello world", 11)]
Undo: restores "hello world", cursor at 11
Undo: restores "hello", cursor at 5
Redo: restores "hello world", cursor at 11
```

### Undo Grouping
To avoid tedious character-by-character undo, group edits:
- Consecutive character insertions → single undo unit
- Space or punctuation starts new group
- Any command (kill, yank, etc.) is its own unit
- Movement ends current group but doesn't create new unit

### Redo Stack Behavior
- Redo stack is cleared when a new edit is made
- Undo pushes to redo stack
- Redo pops from redo stack and pushes to undo stack

### State to Track per UndoUnit
```csharp
record UndoUnit(string Text, int CursorPosition);
```

### Implementation Complexity
- Medium complexity
- Requires hooking into all edit operations
- Consider using Command pattern for edits
