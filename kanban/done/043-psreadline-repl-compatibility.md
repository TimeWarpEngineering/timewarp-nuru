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
- [x] 043-002a - Interactive History Search (Ctrl+R/S with EditMode state machine)
- [x] 043-003 - Tab Completion (Tab/Shift+Tab cycling, Alt+= possible completions)
- [x] 043-004 - Kill Ring / Cut-Paste
- [x] 043-005 - Undo/Redo
- [x] 043-006 - Text Selection
- [x] 043-007 - Word Operations
- [x] 043-008 - Basic Editing Enhancement
- [x] 043-009 - Multiline Editing (Shift+Enter, continuation prompts)
- [x] 043-010 - Yank Arguments (Alt+. YankLastArg, Alt+Ctrl+Y YankNthArg)

### Documentation
- [x] Update REPL documentation with supported keybindings
- [x] Create keybinding reference table

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

## Results

**Completed:** 2025-12-05

### Summary

All 11 PSReadLine compatibility subtasks have been implemented, providing comprehensive PowerShell-style editing capabilities in the Nuru REPL.

### Implemented Features

| Task | Feature | Key Bindings |
|------|---------|--------------|
| 043-001 | Basic Cursor Movement | Home/End, Ctrl+A/E, Ctrl+Left/Right, etc. |
| 043-002 | History Navigation | Up/Down, Ctrl+R (prefix), F8 |
| 043-002a | Interactive History Search | Ctrl+R/S with modal search UI |
| 043-003 | Tab Completion | Tab/Shift+Tab cycling, menu complete |
| 043-004 | Kill Ring | Ctrl+K/U/W, Ctrl+Y, Alt+Y cycling |
| 043-005 | Undo/Redo | Ctrl+Z/Ctrl+Shift+Z with character grouping |
| 043-006 | Text Selection | Shift+arrows, Ctrl+Shift+arrows, clipboard |
| 043-007 | Word Operations | Alt+U/L/C case, Ctrl+T transpose, Ctrl+Backspace |
| 043-008 | Basic Editing | Ctrl+D exit, Ctrl+L clear, Insert overwrite |
| 043-009 | Multiline Editing | Shift+Enter, continuation prompts |
| 043-010 | Yank Arguments | Alt+. last arg, Alt+Ctrl+Y Nth arg |

### Architecture

- **EditMode state machine**: Normal, Search, MenuComplete modes
- **4 key binding profiles**: Default, Emacs, Vi, VSCode
- **Partial class structure**: ReplConsoleReader split across 10+ files
- **Abstraction**: IReplIO for testability

### Test Coverage

- 200+ tests across all features
- Jaribu runfile-based test framework
- Integration tests with simulated console input

### Files Added/Modified

**Core input handling** (`source/timewarp-nuru-repl/input/`):
- `repl-console-reader.cs` (main partial)
- `repl-console-reader.cursor.cs`
- `repl-console-reader.history.cs`
- `repl-console-reader.search.cs`
- `repl-console-reader.kill-ring.cs`
- `repl-console-reader.undo.cs`
- `repl-console-reader.selection.cs`
- `repl-console-reader.word-ops.cs`
- `repl-console-reader.multiline.cs`
- `repl-console-reader.yank-arg.cs`
- `multiline-buffer.cs`
- `kill-ring.cs`
- `selection.cs`
- `undo-stack.cs`

**Key bindings** (`source/timewarp-nuru-repl/key-bindings/`):
- `default-key-binding-profile.cs`
- `emacs-key-binding-profile.cs`
- `vi-key-binding-profile.cs`
- `vscode-key-binding-profile.cs`
