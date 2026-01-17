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
