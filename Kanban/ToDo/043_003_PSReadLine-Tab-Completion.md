# PSReadLine Tab Completion

## Description

Enhance the Nuru REPL tab completion to match PSReadLine behavior, including menu completion and completion listing features.

## Parent

043_PSReadLine-REPL-Compatibility

## Requirements

- Tab completion should integrate with existing route-based completion
- Menu completion should show interactive selection UI
- Completion should respect word boundaries

## Checklist

### Basic Completion (VERIFY EXISTING)
- [ ] Tab: Complete - Complete current word or show completions (VERIFY - partially implemented)
- [ ] Shift+Tab: TabCompletePrevious - Cycle backwards through completions (VERIFY - implemented)

### Menu Completion (IMPLEMENT)
- [ ] Ctrl+Space: MenuComplete - Show interactive menu of completions
- [ ] Display completions in columnar format
- [ ] Arrow keys to navigate menu
- [ ] Enter to select, Escape to cancel
- [ ] Type to filter completions

### Completion Display (IMPLEMENT)
- [ ] Alt+=: PossibleCompletions - Display all possible completions without modifying input
- [ ] Show completions in columns below prompt
- [ ] Return to editing after display

### Completion Behavior
- [ ] Double-Tab shows all completions when multiple matches (like bash)
- [ ] Single completion auto-completes without showing menu
- [ ] Partial completion to common prefix when multiple matches

### Testing
- [ ] Test menu navigation
- [ ] Test with many completions (scrolling)
- [ ] Test completion filtering

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
