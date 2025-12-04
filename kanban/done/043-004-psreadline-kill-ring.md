# PSReadLine Kill Ring / Cut-Paste

## Description

Implement Emacs-style kill ring functionality in the Nuru REPL. The kill ring is a circular buffer of "killed" (cut) text that can be "yanked" (pasted) back. This is a core feature of Emacs-style editing and PSReadLine.

## Parent

043_PSReadLine-REPL-Compatibility

## Requirements

- Kill ring should hold multiple items (configurable, default 10)
- Consecutive kills should append to the same kill ring entry
- Kill ring should be separate from system clipboard

## Checklist

### Kill Ring Infrastructure (IMPLEMENT)
- [x] Create KillRing class with circular buffer
- [x] Track "last kill" for consecutive kill appending
- [x] Configurable ring size

### Line Killing (IMPLEMENT)
- [x] Ctrl+K: KillLine - Kill from cursor to end of line
- [x] Ctrl+U: BackwardKillInput - Kill from beginning of line to cursor (Unix style)
- [x] Alternative: Ctrl+U kills entire line in some configurations

### Word Killing (IMPLEMENT)
- [x] Ctrl+W: UnixWordRubout - Kill previous whitespace-delimited word
- [x] Alt+D: KillWord - Kill from cursor to end of current word
- [x] Alt+Backspace: BackwardKillWord - Kill from start of current word to cursor

### Yanking (IMPLEMENT)
- [x] Ctrl+Y: Yank - Paste most recent kill ring entry at cursor
- [x] Alt+Y: YankPop - Replace just-yanked text with previous kill ring entry
- [x] YankPop only works immediately after Yank or YankPop

### Testing
- [x] Test kill ring rotation
- [x] Test consecutive kills appending
- [x] Test YankPop cycling through ring

## Notes

### PSReadLine Function Reference
| Function | Description |
|----------|-------------|
| KillLine | Kill text from cursor to end of line |
| BackwardKillInput | Kill text from beginning of input to cursor |
| UnixWordRubout | Kill previous whitespace-delimited word |
| KillWord | Kill text from cursor to end of word |
| BackwardKillWord | Kill from start of word to cursor |
| Yank | Paste the most recently killed text |
| YankPop | Replace yanked text with previous kill ring item |

### Kill Ring Behavior
```
Kill Ring: [entry1, entry2, entry3] ← most recent
                                   ↑
                              current pointer

Ctrl+Y: Paste entry3 (most recent)
Alt+Y:  Replace with entry2, pointer moves
Alt+Y:  Replace with entry1, pointer moves
Alt+Y:  Replace with entry3, pointer wraps
```

### Consecutive Kill Behavior
When multiple kill commands are executed in sequence without other commands between them, the killed text is appended to the same kill ring entry:
```
Line: "hello world foo bar"
Cursor at: "hello |world foo bar"
Ctrl+K: kills "world foo bar" → ring: ["world foo bar"]
(without moving cursor or other input)
Ctrl+K again: appends to same entry → ring: ["world foo bar"]
```

### Implementation Notes
- KillRing should be a property of the REPL session
- Track "last command was kill" flag for append behavior
- Consider integration with system clipboard as optional feature
