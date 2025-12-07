# Ctrl+V Paste: Add Kill Ring Fallback

## Summary

When system clipboard is unavailable (Linux without xclip/xsel, WSL), `Ctrl+V` (paste) silently fails. Add fallback to internal kill ring so paste works reliably.

## Problem

Current `HandlePaste()` only reads from system clipboard:
- Returns `null` if clipboard tools unavailable
- User presses `Ctrl+V` and nothing happens
- Confusing because `Ctrl+X` (cut) worked (saved to kill ring)

## Solution

Modify `HandlePaste()` to fall back to kill ring when system clipboard returns null:

```csharp
internal void HandlePaste()
{
  string? clipboardText = GetClipboardText();
  
  // Fallback to kill ring if system clipboard unavailable
  if (string.IsNullOrEmpty(clipboardText))
    clipboardText = KillRing.Yank();
    
  if (string.IsNullOrEmpty(clipboardText))
    return;
  
  // ... rest unchanged
}
```

## Behavior After Fix

| Scenario | Before | After |
|----------|--------|-------|
| Cut in REPL → Paste in REPL (clipboard works) | ✅ | ✅ |
| Cut in REPL → Paste in REPL (no clipboard) | ❌ Silent fail | ✅ Uses kill ring |
| Copy from external app → Paste in REPL | ✅ | ✅ |
| Copy from external app → Paste (no clipboard) | ❌ | ❌ (expected) |

## Checklist

- [ ] Update `HandlePaste()` in `repl-console-reader.selection.cs` to fall back to kill ring
- [ ] Add test for paste with kill ring fallback
- [ ] Consider: Should we track if last yank was from clipboard vs kill ring for `YankPop` behavior?

## Notes

- `Ctrl+Y` (yank) already uses kill ring directly - this is the reliable cross-platform option
- Long-term fix: `timewarp-clip` utility (see timewarp-ganda#1)
- This is a quick improvement that helps immediately

## Related

- timewarp-ganda#1: Create timewarp-clip cross-platform clipboard utility
