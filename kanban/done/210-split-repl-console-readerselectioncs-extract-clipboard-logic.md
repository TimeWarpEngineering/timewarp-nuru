# Split repl-console-reader.selection.cs - extract clipboard logic

## Description

The `repl-console-reader.selection.cs` file (807 lines) contains both text selection logic and clipboard operations. The clipboard functionality (~450 lines) is a completely separate cross-cutting concern with platform-specific code that should be extracted to its own partial file.

**Location:** `source/timewarp-nuru-repl/input/repl-console-reader.selection.cs`

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

- [x] Create `repl-console-reader.clipboard.cs` partial file
- [x] Move `GetClipboardText()` and all overloads to clipboard file
- [x] Move `SetClipboardText()` and all overloads to clipboard file
- [x] Move all platform-specific clipboard methods (Windows, macOS, Linux)
- [x] Move `ClipboardToolCache` nested class to clipboard file
- [x] Update XML documentation in main file's `<remarks>` to include new partial
- [x] Add appropriate XML summary to new clipboard partial file
- [x] Verify all tests pass
- [x] Verify build succeeds

## Notes

### Current Structure

```
repl-console-reader.selection.cs (807 lines)
├── Character Selection (~80 lines)
│   ├── HandleSelectBackwardChar()
│   └── HandleSelectForwardChar()
├── Word Selection (~80 lines)
│   ├── HandleSelectBackwardWord()
│   └── HandleSelectNextWord()
├── Line Selection (~100 lines)
│   ├── HandleSelectBackwardsLine()
│   ├── HandleSelectLine()
│   └── HandleSelectAll()
├── Selection Actions (~100 lines)
│   ├── HandleCopy()
│   ├── HandleCut()
│   ├── HandlePaste()
│   └── HandleDeleteSelection()
└── Clipboard Operations (~450 lines) ← EXTRACT THIS
    ├── GetClipboardText() - with platform detection
    ├── SetClipboardText() - with platform detection
    ├── Windows-specific methods
    ├── macOS-specific methods
    ├── Linux-specific methods (xclip, xsel, wl-clipboard)
    └── ClipboardToolCache nested class
```

### Target Structure

After refactoring:
- `repl-console-reader.selection.cs` (~350 lines) - Text selection operations
- `repl-console-reader.clipboard.cs` (~450 lines) - Platform-specific clipboard

### Why This Split Works Well

1. Clipboard code is entirely self-contained (static methods, nested static class)
2. Clipboard is infrastructure, selection is user interaction logic
3. Platform-specific code is isolated from UI logic
4. The `ClipboardToolCache` class is an implementation detail of clipboard

### Reference Pattern

Follow the existing `ReplConsoleReader` partial class pattern:
- 12 existing partial files organized by functional area
- Each partial has XML summary describing its purpose
- Main file's `<remarks>` lists all partials

## Results

### Files Created/Modified

| File | Lines | Description |
|------|-------|-------------|
| `repl-console-reader.clipboard.cs` | 393 | New file - Platform-specific clipboard operations |
| `repl-console-reader.selection.cs` | 302 | Modified - Selection logic only (down from 808) |
| `repl-console-reader.cs` | +1 | Updated `<remarks>` to include clipboard partial |

### Line Count Changes

- **Before:** 808 lines in selection.cs
- **After:** 302 lines (selection) + 393 lines (clipboard) = 695 total
- **Net reduction:** 113 lines (14% reduction due to removing redundant comments)

### Test Results

All 19 text selection tests pass.

### Structure Improvement

The `ReplConsoleReader` now has 10 partial files organized by functional area:
1. repl-console-reader.cs - Core
2. repl-console-reader.cursor-movement.cs
3. repl-console-reader.history.cs
4. repl-console-reader.editing.cs
5. repl-console-reader.search.cs
6. repl-console-reader.kill-ring.cs
7. repl-console-reader.undo.cs
8. repl-console-reader.selection.cs
9. repl-console-reader.clipboard.cs ← NEW
10. repl-console-reader.word-operations.cs
