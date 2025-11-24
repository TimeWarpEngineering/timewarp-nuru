# PSReadLine Multiline Editing (Optional/Future)

## Description

Implement multiline editing support in the Nuru REPL, allowing users to enter and edit input that spans multiple lines. This is useful for complex commands, scripts, or structured input.

**Note: This is an optional/future enhancement. Basic single-line editing should be completed first.**

## Parent

043_PSReadLine-REPL-Compatibility

## Requirements

- Multiline input should be visually clear (continuation prompts)
- Navigation between lines should be intuitive
- Execution should require explicit action (not just Enter)

## Checklist

### Line Addition (IMPLEMENT)
- [ ] Shift+Enter: AddLine - Add new line without executing
- [ ] Ctrl+Enter: InsertLineAbove - Insert new line above current
- [ ] Alt+Enter: InsertLineBelow - Insert new line below current

### Line Navigation (IMPLEMENT)
- [ ] Up (at first line position): PreviousLine - Move to previous line
- [ ] Down (at last line position): NextLine - Move to next line
- [ ] Ctrl+Up: MoveLineUp - Move current line up
- [ ] Ctrl+Down: MoveLineDown - Move current line down

### Multiline Rendering (IMPLEMENT)
- [ ] Display continuation prompt for subsequent lines (e.g., ">> ")
- [ ] Track cursor position across lines
- [ ] Handle line wrapping correctly

### Execution (IMPLEMENT)
- [ ] Enter at end of complete input: Execute
- [ ] Enter in middle of input: Depends on context
- [ ] Consider syntax-aware completion detection

### Brace/Quote Matching (IMPLEMENT)
- [ ] Detect unclosed braces, brackets, quotes
- [ ] Auto-continue to new line when input is incomplete
- [ ] Visual indicator of matching delimiters

### Testing
- [ ] Test line navigation
- [ ] Test multiline rendering
- [ ] Test execution timing

## Notes

### PSReadLine Function Reference
| Function | Description |
|----------|-------------|
| AddLine | Add a new line below current and move cursor |
| InsertLineAbove | Insert a new line above current |
| InsertLineBelow | Insert a new line below current |
| PreviousLine | Move to previous line in multiline input |
| NextLine | Move to next line in multiline input |

### Multiline Display Example
```
> deploy --environment production \
>>   --tag v2.0.1 \
>>   --dry-run
```

Or with line numbers:
```
[1]> deploy \
[2]>   --environment production \
[3]>   --tag v2.0.1
```

### Input Model Change
Single-line: `string Input`
Multi-line: `List<string> Lines` or `string Input` with embedded newlines

Cursor position becomes: `(int Line, int Column)` instead of just `int Position`

### Execution Detection Options
1. **Always explicit**: Only Ctrl+Enter executes
2. **Syntax-aware**: Enter executes if input is syntactically complete
3. **Trailing backslash**: Line ending with `\` continues

Recommend option 3 for CLI compatibility (matches shell behavior).

### Implementation Complexity
- High complexity
- Significant changes to input model and rendering
- Consider implementing as optional mode
- May require refactoring cursor management

### Dependencies
- Should complete single-line editing first
- May benefit from selection (043_006) being done first
- Undo (043_005) becomes more complex with multiline
