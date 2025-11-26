# PSReadLine Interactive History Search

## Description

Implement Ctrl+R and Ctrl+S interactive incremental history search. This provides a real-time search experience where users type a search pattern and see matching history entries as they type.

**Prerequisites:** Requires EditMode state machine implementation (see 043_002 implementation notes).

## Parent

043_PSReadLine-REPL-Compatibility

## Requirements

- Search should be case-insensitive by default
- Display search prompt showing current pattern
- Update results in real-time as user types
- Support cycling through multiple matches
- Escape cancels, Enter accepts, other keys accept and process

## Checklist

### Architecture (MUST IMPLEMENT FIRST)
- [ ] Add EditMode enum (Normal, Search, MenuComplete)
- [ ] Add CurrentMode field to track active mode
- [ ] Modify ReadLine loop to check CurrentMode
- [ ] Create mode-specific key handlers

### Interactive Reverse Search (Ctrl+R)
- [ ] Add Ctrl+R keybinding (conditional on Normal mode)
- [ ] Implement HandleReverseSearchHistory() to enter search mode
- [ ] Display search prompt: "(reverse-i-search)`pattern': "
- [ ] Search backward through history for entries containing pattern
- [ ] Show matched entry with highlighted search term
- [ ] Press Ctrl+R again to find next (older) match
- [ ] Enter accepts current match and returns to Normal mode
- [ ] Escape cancels and returns to Normal mode
- [ ] Other keys accept match and process the key

### Interactive Forward Search (Ctrl+S)
- [ ] Add Ctrl+S keybinding (conditional on Normal mode)
- [ ] Implement HandleForwardSearchHistory() to enter search mode
- [ ] Display search prompt: "(forward-i-search)`pattern': "
- [ ] Search forward through history for entries containing pattern
- [ ] Show matched entry with highlighted search term
- [ ] Press Ctrl+S again to find next (newer) match
- [ ] Enter accepts current match and returns to Normal mode
- [ ] Escape cancels and returns to Normal mode

### Testing
- [ ] Add tests for Ctrl+R search mode entry
- [ ] Add tests for incremental search (pattern updates)
- [ ] Add tests for cycling through matches
- [ ] Add tests for Enter/Escape handling
- [ ] Test edge cases (empty history, no matches, empty pattern)

## Notes

### PSReadLine Function Reference

| Function | Description |
|----------|-------------|
| ReverseSearchHistory | Interactively search backward through history |
| ForwardSearchHistory | Interactively search forward through history |

### Interactive Search UX Flow

When Ctrl+R is pressed:
```
1. User is at: demo> greet
2. Press Ctrl+R:
   (reverse-i-search)`': 
3. User types 'dep':
   (reverse-i-search)`dep': deploy prod
4. Press Ctrl+R again:
   (reverse-i-search)`dep': deploy staging
5. Press Enter - accepts "deploy staging" and exits search mode
   demo> deploy staging
```

### EditMode State Machine

```csharp
private enum EditMode 
{ 
  Normal,      // Standard editing
  Search,      // Interactive search (Ctrl+R/S)
  MenuComplete // Menu completion (Ctrl+Space) - future
}

private EditMode CurrentMode = EditMode.Normal;
private string SearchPattern = string.Empty;
private int SearchMatchIndex = -1;
```

### Key Handling in Search Mode

When in Search mode:
- **Character keys** - Append to search pattern, update results
- **Backspace** - Remove from search pattern, update results
- **Ctrl+R** - Find next older match
- **Ctrl+S** - Find next newer match
- **Enter** - Accept current match, exit to Normal mode
- **Escape** - Cancel search, restore original input, exit to Normal mode
- **Other keys** - Accept current match, process the key, exit to Normal mode

### Implementation Strategy

1. **Phase 1:** Add EditMode enum and mode checking
2. **Phase 2:** Implement Ctrl+R search mode
3. **Phase 3:** Implement Ctrl+S forward search
4. **Phase 4:** Add comprehensive tests

### Related Tasks

- **043_002** - Completed basic history navigation
- **043_003** - Tab completion (Ctrl+Space menu also needs EditMode)

### Implementation Location

- Input handling: `Source/TimeWarp.Nuru.Repl/Input/ReplConsoleReader.cs`
- Tests: `Tests/TimeWarp.Nuru.Repl.Tests/repl-18-psreadline-keybindings.cs`

## Implementation Notes

This task is blocked until we implement the EditMode state machine. Without it, we cannot cleanly implement the modal behavior required for interactive search.

The EditMode architecture will also be reused for:
- Task 043_003: Ctrl+Space menu completion
- Future: Vi mode (if desired)
