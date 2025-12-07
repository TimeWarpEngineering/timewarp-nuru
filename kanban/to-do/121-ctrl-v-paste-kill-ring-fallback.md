# Ctrl+V Paste: Improve Clipboard Support + Kill Ring Fallback

## Summary

Improve clipboard support on Linux/WSL by adding `pwsh` and WSL detection, plus kill ring fallback when all clipboard methods fail.

## Problem

Current `HandlePaste()` on Linux:
- Only tries `xclip` / `xsel` (requires install, requires X11)
- WSL: Can write via `clip.exe` but can't read (no paste)
- Silent failure leaves users confused

## Solution

Update clipboard detection order for Linux:

### Get Clipboard (Read/Paste)
```
1. pwsh -command "Get-Clipboard"     # PowerShell Core (cross-platform)
2. powershell.exe -command "Get-Clipboard"  # WSL → Windows clipboard
3. xclip -selection clipboard -o     # X11
4. xsel --clipboard --output         # X11 fallback
5. Kill ring                         # Internal fallback
```

### Set Clipboard (Write/Copy)
```
1. pwsh -command "Set-Clipboard ..." # PowerShell Core (cross-platform)
2. clip.exe                          # WSL → Windows clipboard
3. xclip -selection clipboard        # X11
4. xsel --clipboard --input          # X11 fallback
```

## Behavior After Fix

| Platform | Copy | Paste | Notes |
|----------|------|-------|-------|
| Windows | ✅ | ✅ | PowerShell (existing) |
| macOS | ✅ | ✅ | pbcopy/pbpaste (existing) |
| Linux + pwsh | ✅ | ✅ | PowerShell Core handles it |
| Linux + X11 | ✅ | ✅ | xclip/xsel (existing) |
| WSL | ✅ | ✅ | clip.exe + powershell.exe |
| Linux (nothing) | ❌ | ⚠️ | Kill ring fallback for paste |

## Checklist

### Implementation
- [ ] Add `pwsh` detection for Get/Set clipboard on Linux
- [ ] Add WSL detection: check for `clip.exe` availability
- [ ] Add `powershell.exe -command "Get-Clipboard"` for WSL paste
- [ ] Add kill ring fallback to `HandlePaste()`
- [ ] Cache clipboard tool availability at startup (avoid repeated `which` calls)

### Testing
- [ ] Test pwsh clipboard on Linux (if available)
- [ ] Test WSL clipboard integration
- [ ] Test kill ring fallback when no clipboard available
- [ ] Test existing Windows/macOS behavior unchanged

## Notes

- `pwsh` (PowerShell Core) has cross-platform `Get-Clipboard`/`Set-Clipboard` built-in
- WSL: `clip.exe` writes to Windows clipboard, `powershell.exe Get-Clipboard` reads from it
- Kill ring fallback ensures `Ctrl+X` → `Ctrl+V` always works within REPL
- Long-term: Replace with `timewarp-clip` utility (timewarp-ganda#1)

## Related

- timewarp-ganda#1: Create timewarp-clip cross-platform clipboard utility
