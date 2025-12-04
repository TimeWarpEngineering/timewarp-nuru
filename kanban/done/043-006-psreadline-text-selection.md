# PSReadLine Text Selection

## Description

Implement text selection functionality in the Nuru REPL, allowing users to select text using Shift+movement keys and perform operations on the selection (copy, cut, delete).

## Parent

043_PSReadLine-REPL-Compatibility

## Requirements

- Selection should be visually indicated (highlight/inverse colors)
- Selection state should be cleared on non-selection commands
- Copy should work with system clipboard where possible

## Checklist

### Selection Infrastructure (IMPLEMENT)
- [ ] Track selection state (anchor position, active position)
- [ ] Implement selection rendering (visual highlight)
- [ ] Selection model: anchor (start) and cursor (end) positions

### Character Selection (IMPLEMENT)
- [ ] Shift+LeftArrow: SelectBackwardChar - Extend selection one character left
- [ ] Shift+RightArrow: SelectForwardChar - Extend selection one character right

### Word Selection (IMPLEMENT)
- [ ] Shift+Ctrl+LeftArrow: SelectBackwardWord - Extend selection to previous word
- [ ] Shift+Ctrl+RightArrow: SelectNextWord - Extend selection to next word
- [ ] Shift+Alt+B: SelectBackwardWord - Alternative binding
- [ ] Shift+Alt+F: SelectForwardWord - Alternative binding

### Line Selection (IMPLEMENT)
- [ ] Shift+Home: SelectBackwardsLine - Extend selection to beginning of line
- [ ] Shift+End: SelectLine - Extend selection to end of line
- [ ] Ctrl+Shift+A: SelectAll - Select entire input

### Selection Actions (IMPLEMENT)
- [ ] Ctrl+C: CopyOrCancelLine - Copy selection to clipboard, or cancel if no selection
- [ ] Ctrl+X: Cut - Cut selection to clipboard (and kill ring)
- [ ] Delete/Backspace with selection: Delete selected text
- [ ] Typing with selection: Replace selected text

### Clipboard Integration (IMPLEMENT)
- [ ] Ctrl+V: Paste - Paste from system clipboard
- [ ] Integrate with kill ring (Ctrl+X adds to kill ring)

### Selection Clearing
- [ ] Clear selection on cursor movement without Shift
- [ ] Clear selection on text modification
- [ ] Escape clears selection

### Testing
- [ ] Test selection rendering
- [ ] Test selection with copy/cut/paste
- [ ] Test selection replacement on typing

## Notes

### PSReadLine Function Reference
| Function | Description |
|----------|-------------|
| SelectBackwardChar | Extend selection one character backward |
| SelectForwardChar | Extend selection one character forward |
| SelectBackwardWord | Extend selection to start of previous word |
| SelectNextWord | Extend selection to end of next word |
| SelectLine | Extend selection to end of line |
| SelectAll | Select entire input |
| CopyOrCancelLine | Copy selection or cancel line if no selection |
| Cut | Cut selection to clipboard |
| Paste | Paste from clipboard |

### Selection Model
```csharp
class Selection
{
    public int Anchor { get; set; }  // Where selection started
    public int Cursor { get; set; }  // Current cursor position
    public bool IsActive => Anchor != Cursor;
    public int Start => Math.Min(Anchor, Cursor);
    public int End => Math.Max(Anchor, Cursor);
    public int Length => End - Start;
}
```

### Visual Rendering
- Use ANSI escape codes for highlighting: `\e[7m` (inverse) `\e[0m` (reset)
- Or use background color: `\e[44m` (blue background)
- Selection should be visible on redraw

### CopyOrCancelLine Behavior
PSReadLine's Ctrl+C behavior:
- If text is selected: Copy selection to clipboard, clear selection
- If no selection: Cancel current line (like pressing Escape)
- This dual behavior is familiar to Windows users

### Implementation Complexity
- Medium-High complexity
- Requires modifying rendering to show selection
- Clipboard integration varies by platform
- Consider using TextCopy library for cross-platform clipboard

## Results

### Implementation Summary
- Created `Selection` class to track anchor and cursor positions
- Created `repl-console-reader.selection.cs` partial class with all selection handlers
- Implemented selection rendering with ANSI inverse video (`\e[7m`)
- Added platform-specific clipboard support (Windows/PowerShell, macOS/pbcopy, Linux/xclip|xsel)
- Cut operation adds to both clipboard and kill ring for seamless integration

### Key Bindings Added (All Profiles)
| Binding | Action |
|---------|--------|
| Shift+Left | SelectBackwardChar |
| Shift+Right | SelectForwardChar |
| Ctrl+Shift+Left | SelectBackwardWord |
| Ctrl+Shift+Right | SelectNextWord |
| Alt+Shift+B | SelectBackwardWord (Emacs/Default) |
| Alt+Shift+F | SelectNextWord (Emacs/Default) |
| Shift+Home | SelectBackwardsLine |
| Shift+End | SelectLine |
| Ctrl+Shift+A | SelectAll (Ctrl+A in VSCode) |
| Ctrl+C | CopyOrCancelLine |
| Ctrl+X | Cut |
| Ctrl+V | Paste |

### Files Created/Modified
- NEW: `source/timewarp-nuru-repl/input/selection.cs` - Selection model
- NEW: `source/timewarp-nuru-repl/input/repl-console-reader.selection.cs` - Selection handlers
- NEW: `tests/timewarp-nuru-repl-tests/repl-28-text-selection.cs` - 19 tests
- MODIFIED: `repl-console-reader.cs` - Added SelectionState field
- MODIFIED: `repl-console-reader.cursor-movement.cs` - Clear selection on movement
- MODIFIED: `repl-console-reader.editing.cs` - Handle selection in delete/escape
- MODIFIED: All key binding profiles - Added selection bindings
- MODIFIED: `timewarp-nuru-repl.csproj` - Added NoWarn for CA1031, RCS1075 (clipboard exceptions)

### Test Results
- 19 tests in `repl-28-text-selection.cs` - all pass
- All 30 existing REPL tests still pass

### Technical Notes
- Selection uses anchor (start point) and cursor (current point) model
- Start/End properties normalize for forward/backward selection
- Bounds checking prevents crashes when text changes while selection active
- Escape clears selection before clearing text to avoid index errors

### Commit
`feat(repl): implement PSReadLine text selection with clipboard support` (3eab9e1)
