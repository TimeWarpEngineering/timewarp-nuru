# PSReadLine Tab Completion

## Description

Enhance the Nuru REPL tab completion to match PSReadLine behavior. Basic tab completion (Tab/Shift+Tab) and PossibleCompletions (Alt+=) are now implemented.

**Note:** Menu completion (Ctrl+Space) split into separate task 043_003a due to complex multi-line rendering requirements.

## Parent

043_PSReadLine-REPL-Compatibility

## Requirements

- Tab completion should integrate with existing route-based completion
- Menu completion should show interactive selection UI
- Completion should respect word boundaries

## Checklist

### Basic Completion
- [x] Tab: Complete - Complete current word or show completions (IMPLEMENTED - from original REPL)
- [x] Shift+Tab: TabCompletePrevious - Cycle backwards through completions (IMPLEMENTED - from original REPL)

### Completion Display
- [x] Alt+=: PossibleCompletions - Display all possible completions without modifying input (IMPLEMENTED with test)
- [x] Show completions in columns below prompt (IMPLEMENTED)
- [x] Return to editing after display (IMPLEMENTED)

### Completion Behavior (Already Working)
- [x] Double-Tab shows all completions when multiple matches
- [x] Single completion auto-completes without showing menu
- [x] Cycle through completions with Tab/Shift+Tab

### Menu Completion (Moved to 043_003a)
- See task 043_003a for Ctrl+Space interactive menu implementation

### Testing
- [x] Test Alt+= display functionality (1 test in repl-18)

## Notes

### PSReadLine Function Reference
| Function | Description |
|----------|-------------|
| Complete | Complete the input if there is a single completion, else complete to longest common prefix |
| TabCompleteNext | Cycle forward through available completions |
| TabCompletePrevious | Cycle backward through available completions |
| MenuComplete | Show interactive completion menu |
| PossibleCompletions | Display possible completions without completing |

### Menu Completion UX
```
> deploy pro[Ctrl+Space]
┌─────────────────┐
│ production     │  ← highlighted
│ prometheus     │
│ profiling      │
└─────────────────┘
```
- Up/Down arrows navigate
- Left/Right for multi-column layouts
- Typing filters the list
- Tab also cycles through menu items

### Integration Points
- Existing completion provider in `Source/TimeWarp.Nuru.Completion/`
- REPL completion integration needed
- Consider reusing completion UI from shell completion if applicable
