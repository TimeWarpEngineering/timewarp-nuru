# PSReadLine History Navigation

## Description

Implement PSReadLine-compatible history navigation features in the Nuru REPL. This includes basic up/down navigation (already implemented), plus advanced features like interactive search and prefix-based history search.

## Parent

043_PSReadLine-REPL-Compatibility

## Requirements

- History navigation should preserve cursor position where sensible
- Search features should provide visual feedback
- History search should be case-insensitive by default

## Checklist

### Basic Navigation
- [x] UpArrow: PreviousHistory - Replace input with previous history item (VERIFIED - tested in 043_001)
- [x] Ctrl+P: PreviousHistory - Alternative binding (VERIFIED - tested in 043_001)
- [x] DownArrow: NextHistory - Replace input with next history item (VERIFIED - tested in 043_001)
- [x] Ctrl+N: NextHistory - Alternative binding (VERIFIED - tested in 043_001)

### History Position
- [x] Alt+<: BeginningOfHistory - Jump to first history item (IMPLEMENTED with tests)
- [x] Alt+>: EndOfHistory - Jump to current input (exit history) (IMPLEMENTED with tests)

### Interactive Search (REQUIRES EditMode STATE MACHINE)
- [ ] Ctrl+R: ReverseSearchHistory - Interactive reverse incremental search
- [ ] Ctrl+S: ForwardSearchHistory - Interactive forward incremental search
- [ ] Display search prompt (e.g., "(reverse-i-search)`pattern':")
- [ ] Highlight matching text in results
- [ ] Press Ctrl+R again to find next match
- [ ] Enter to accept, Escape to cancel

### Prefix Search
- [x] F8: HistorySearchBackward - Search history for items starting with current input prefix (IMPLEMENTED with tests)
- [x] Shift+F8: HistorySearchForward - Search forward with prefix (IMPLEMENTED with tests)

### Testing
- [x] Add tests for BeginningOfHistory/EndOfHistory (2 tests in repl-18)
- [x] Add tests for prefix search functionality (4 tests in repl-18)
- [ ] Test edge cases for interactive search (empty history, no matches)

## Notes

### PSReadLine Function Reference
| Function | Description |
|----------|-------------|
| PreviousHistory | Move to previous item in history |
| NextHistory | Move to next item in history |
| ReverseSearchHistory | Interactively search backward through history |
| ForwardSearchHistory | Interactively search forward through history |
| BeginningOfHistory | Move to first item in history |
| EndOfHistory | Move to current (unsaved) input |
| HistorySearchBackward | Search backward for entries starting with current input |
| HistorySearchForward | Search forward for entries starting with current input |

### Interactive Search UX
When Ctrl+R is pressed:
1. Display search prompt at cursor position
2. User types search pattern
3. As they type, show most recent matching history entry
4. Ctrl+R again cycles to older matches
5. Enter accepts the match, Escape cancels
6. Any other key accepts match and processes that key

### Implementation Complexity
- Basic navigation: Low (verify existing)
- Interactive search: Medium-High (requires search UI state machine)
- Prefix search: Medium (filter history by prefix)
