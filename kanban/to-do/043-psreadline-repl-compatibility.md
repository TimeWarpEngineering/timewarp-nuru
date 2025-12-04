# PSReadLine REPL Compatibility

## Description

Implement PSReadLine-like editing capabilities in the Nuru REPL to provide a familiar PowerShell-style editing experience. This is a parent task that tracks the implementation of comprehensive keyboard-based line editing features matching PSReadLine's functionality.

PSReadLine is the PowerShell line-editing module that provides advanced command-line editing features. By implementing compatible keybindings and behaviors, Nuru REPL users familiar with PowerShell will have a seamless experience.

## Requirements

- Match PSReadLine keybinding conventions where possible
- Support both Emacs-style (default) and common alternative bindings
- Maintain backward compatibility with existing REPL functionality
- Ensure all features work correctly with the IReplIO abstraction

## Checklist

### Child Tasks
- [x] 043-001 - Basic Cursor Movement (keybinding dictionary, character/word/line movement)
- [x] 043-002 - History Navigation (Up/Down, Beginning/End of history, F8 prefix search)
- [ ] 043-002a - Interactive History Search (Ctrl+R/S - requires EditMode state machine)
- [x] 043-003 - Tab Completion (Tab/Shift+Tab cycling, Alt+= possible completions)
- [ ] 043-004 - Kill Ring / Cut-Paste
- [ ] 043-005 - Undo/Redo
- [ ] 043-006 - Text Selection
- [ ] 043-007 - Word Operations
- [ ] 043-008 - Basic Editing Enhancement
- [ ] 043-009 - Multiline Editing (Optional/Future)
- [ ] 043-010 - Yank Arguments (Optional/Future)

### Documentation
- [ ] Update REPL documentation with supported keybindings
- [ ] Create keybinding reference table

## Notes

### Architecture Prerequisite: EditMode State Machine

Tasks 043-002a (Interactive History Search) and future menu completion require an `EditMode` state machine:

```csharp
private enum EditMode 
{ 
  Normal,      // Standard editing
  Search,      // Interactive search (Ctrl+R/S)
  MenuComplete // Menu completion (Ctrl+Space) - future
}
```

This enables modal behavior where the same keys have different meanings based on current mode.

### PSReadLine Reference
- Official docs: https://docs.microsoft.com/en-us/powershell/module/psreadline/
- Default edit mode is Emacs-style
- Key functions are named consistently (e.g., BackwardChar, ForwardWord, KillLine)

### Implementation Strategy
1. ~~Start with basic cursor movement (043-001)~~ DONE
2. ~~Add history navigation (043-002)~~ DONE
3. ~~Enhance tab completion (043-003)~~ DONE
4. Add basic editing enhancements (043-008)
5. Add word operations (043-007)
6. Implement kill ring for Emacs-style editing (043-004)
7. Add undo/redo support (043-005)
8. Implement interactive history search (043-002a) - requires EditMode
9. Implement text selection (043-006)
10. Optional: multiline and yank arguments (043-009, 043-010)

### Existing REPL Location
- Core REPL: `source/timewarp-nuru-repl/`
- Input handling: `source/timewarp-nuru-repl/input/`
- Key bindings: `source/timewarp-nuru-repl/key-bindings/`
- Tests: `tests/timewarp-nuru-repl-tests/`
