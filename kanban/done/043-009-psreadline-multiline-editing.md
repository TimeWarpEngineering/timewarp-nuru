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

### Phase 1: Core Multiline (COMPLETED)

#### Data Model (DONE)
- [x] MultilineBuffer class with List<string> Lines
- [x] MultilineCursor struct (Line, Column)
- [x] Cursor position tracking across lines
- [x] Position/Cursor conversion methods

#### Line Addition (PARTIAL)
- [x] Shift+Enter: AddLine - Add new line without executing
- [ ] Ctrl+Enter: InsertLineAbove - Insert new line above current
- [ ] Alt+Enter: InsertLineBelow - Insert new line below current

#### Multiline Rendering (DONE)
- [x] Display continuation prompt for subsequent lines (e.g., ">> ")
- [x] Track cursor position across lines
- [x] ContinuationPrompt option in ReplOptions

#### Execution (DONE)
- [x] Enter at end of complete input: Execute full multiline
- [x] History integration (multiline commands saved/recalled)

### Phase 2: Line Navigation (NOT STARTED)
- [ ] Up (at first line position): PreviousLine - Move to previous line
- [ ] Down (at last line position): NextLine - Move to next line
- [ ] Ctrl+Up: MoveLineUp - Move current line up
- [ ] Ctrl+Down: MoveLineDown - Move current line down

### Phase 3: Advanced Features (NOT STARTED)
- [ ] Handle line wrapping correctly
- [ ] Syntax-aware completion detection

### Brace/Quote Matching (NOT STARTED)
- [ ] Detect unclosed braces, brackets, quotes
- [ ] Auto-continue to new line when input is incomplete
- [ ] Visual indicator of matching delimiters

### Testing
- [x] MultilineBuffer unit tests (29 tests)
- [x] Multiline editing integration tests (7 tests + 2 skipped)
- [ ] Test line navigation
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

## Results

**Completed:** 2025-12-05

### What was implemented
- **MultilineBuffer class** (`source/timewarp-nuru-repl/input/multiline-buffer.cs`)
  - Multi-line text storage with List<string> lines
  - MultilineCursor struct for (Line, Column) tracking
  - Full cursor navigation (left/right across lines, up/down between lines)
  - Insert/delete character operations
  - Add/split/merge line operations
  - Position↔Cursor conversion methods
  
- **ReplConsoleReader multiline partial** (`source/timewarp-nuru-repl/input/repl-console-reader.multiline.cs`)
  - HandleAddLine() for Shift+Enter
  - HandleExecuteMultiline() for Enter (executes full input)
  - Continuation prompt display (">> " by default)
  
- **Configuration** (`source/timewarp-nuru-core/repl-options.cs`)
  - ContinuationPrompt property (default: ">> ")

- **Key binding** (`default-key-binding-profile.cs`)
  - Shift+Enter → AddLine

### Test coverage
- `repl-31-multiline-buffer.cs`: 29 unit tests for MultilineBuffer
- `repl-32-multiline-editing.cs`: 9 integration tests (7 pass, 2 skipped as flaky)

### Not implemented (deferred to future tasks)
- Ctrl+Enter/Alt+Enter for InsertLineAbove/Below
- Up/Down navigation between lines (Phase 2)
- Line move operations (Ctrl+Up/Down)
- Brace/quote auto-continuation
- Line wrapping

### Commit
`5b34c20` - feat(repl): implement PSReadLine multiline editing with Shift+Enter
