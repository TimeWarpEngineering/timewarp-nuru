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
- [x] Create UndoStack class to track edit history
- [x] Define UndoUnit to store text state and cursor position
- [x] Implement undo grouping for related edits

### Undo Commands (IMPLEMENT)
- [x] Ctrl+_ (Ctrl+Underscore): Undo - Undo last edit
- [x] Ctrl+Z: Undo - Alternative binding (common expectation)
- [x] Multiple undo presses continue undoing

### Redo Commands (IMPLEMENT)
- [x] Ctrl+Shift+Z: Redo - Redo an undone edit (if available)
- [ ] Alt+Shift+_ : Redo - Alternative binding (not implemented)
- [x] Note: Ctrl+Y conflicts with Yank, so avoid for Redo

### Revert Line (IMPLEMENT)
- [x] Alt+R: RevertLine - Undo ALL changes to current line (reset to original)
- [ ] Escape,R: RevertLine - Alternative binding (not implemented)

### Undo Boundaries (IMPLEMENT)
- [x] Group related edits into single undo unit (e.g., typing a word)
- [x] Commands like KillLine, Yank are single undo units
- [x] Movement commands don't create undo entries

### Testing
- [x] Test undo/redo stack behavior
- [x] Test undo after various edit types
- [x] Test RevertLine from different states

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

## Results

### Implementation Summary
- Created `UndoStack` class with separate undo and redo stacks
- Created `UndoUnit` record storing text and cursor position
- Implemented character grouping for consecutive character input (spaces/punctuation end groups)
- Added `repl-console-reader.undo.cs` partial class with HandleUndo, HandleRedo, HandleRevertLine

### Key Bindings Added
| Binding | Action | Profiles |
|---------|--------|----------|
| Ctrl+Z | Undo | All |
| Ctrl+Shift+Z | Redo | Default, Emacs, VSCode |
| Ctrl+_ | Undo | Emacs (canonical) |
| Alt+R | RevertLine | All |

### Files Created/Modified
- NEW: `source/timewarp-nuru-repl/input/undo-stack.cs`
- NEW: `source/timewarp-nuru-repl/input/repl-console-reader.undo.cs`
- NEW: `tests/timewarp-nuru-repl-tests/repl-27-undo-redo.cs` (14 tests)
- MODIFIED: All key binding profiles
- MODIFIED: `repl-console-reader.editing.cs` and `repl-console-reader.kill-ring.cs` (SaveUndoState calls)
- MODIFIED: `repl-console-reader.cursor-movement.cs` (EndUndoCharacterGrouping calls)

### Test Results
- 14 tests in `repl-27-undo-redo.cs` - all pass
- All 30 REPL tests pass

### Deviations from Plan
- Alt+Shift+_ Redo binding not implemented (Ctrl+Shift+Z sufficient)
- Escape,R RevertLine binding not implemented (Alt+R sufficient)

### Commit
`feat(repl): implement PSReadLine undo/redo with character grouping` (30e48a5)
