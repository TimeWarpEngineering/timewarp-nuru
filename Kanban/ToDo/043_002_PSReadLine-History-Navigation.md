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

### Basic Navigation (VERIFY EXISTING)
- [ ] UpArrow: PreviousHistory - Replace input with previous history item (VERIFY)
- [ ] Ctrl+P: PreviousHistory - Alternative binding (VERIFY)
- [ ] DownArrow: NextHistory - Replace input with next history item (VERIFY)
- [ ] Ctrl+N: NextHistory - Alternative binding (VERIFY)

### Interactive Search (IMPLEMENT)
- [ ] Ctrl+R: ReverseSearchHistory - Interactive reverse incremental search
- [ ] Ctrl+S: ForwardSearchHistory - Interactive forward incremental search
- [ ] Display search prompt (e.g., "(reverse-i-search)`pattern':")
- [ ] Highlight matching text in results
- [ ] Press Ctrl+R again to find next match
- [ ] Enter to accept, Escape to cancel

### History Position (IMPLEMENT)
- [ ] Alt+<: BeginningOfHistory - Jump to first history item
- [ ] Alt+>: EndOfHistory - Jump to current input (exit history)

### Prefix Search (IMPLEMENT)
- [ ] F8 or custom: HistorySearchBackward - Search history for items starting with current input prefix
- [ ] Shift+F8 or custom: HistorySearchForward - Search forward with prefix

### Testing
- [ ] Add tests for search functionality
- [ ] Test edge cases (empty history, no matches)

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
