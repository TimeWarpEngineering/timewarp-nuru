# Add CLI tool equivalent to Get-PSReadLineKeyHandler

## Description

Create a CLI tool that displays key bindings and their associated handler functions, similar to PowerShell's `Get-PSReadLineKeyHandler`. This tool should list all available keyboard shortcuts, show what each key combination does, and optionally display details about the function that handles each key.

## Checklist

- [ ] Research Get-PSReadLineKeyHandler functionality and output format
- [ ] Design the CLI command structure and options
- [ ] Implement key binding enumeration
- [ ] Add output formatting (table, detailed view)
- [ ] Support filtering by key chord or function name
- [ ] Add unit tests
- [ ] Document the new CLI tool

## Notes

Get-PSReadLineKeyHandler is a PowerShell cmdlet that returns information about keyboard shortcuts used by PSReadLine. It shows:
- Key (the key chord, e.g., "Ctrl+a", "Alt+b")
- Function (what the key does, e.g., "BeginningOfLine", "DeleteChar")
- Description (what the function does)

The Nuru equivalent should provide similar functionality for TimeWarp.Nuru's key handling system.

### Reference Output Format

The PowerShell cmdlet organizes bindings by **function category**:

```
Basic editing functions
=======================
Key              Function            Description
---              --------            -----------
Enter            AcceptLine          Accept the input or move to the next line...
Backspace        BackwardDeleteChar  Delete the character before the cursor
Ctrl+C           Copy                Copy selected region to the system clipboard...

Cursor movement functions
=========================
LeftArrow       BackwardChar        Move the cursor back one character
Home            BeginningOfLine     Move the cursor to the beginning of the line

History functions
=================
DownArrow       NextHistory         Replace the input with the next item in the history
UpArrow         PreviousHistory     Replace the input with the previous item in the history

Completion functions
====================
Tab             TabCompleteNext     Complete the input using the next completion

Miscellaneous functions
=======================
Ctrl+Alt+?      ShowKeyBindings     Show all key bindings
Alt+?           WhatIsKey           Show the key binding for the next chord entered
```

### Key Design Considerations

- **Categorized output**: Bindings are grouped by function type (editing, cursor movement, history, completion, etc.)
- **Three-column format**: Key | Function | Description
- **Key chord format**: Uses standard notation like `Ctrl+`, `Shift+`, `Alt+`, `Enter`, `Tab`, arrow keys
- **Possible Nuru command name**: `nuru key-handler`, `nuru show-keys`, or `nuru key-bindings`
