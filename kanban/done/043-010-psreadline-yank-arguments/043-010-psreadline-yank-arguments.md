# PSReadLine Yank Arguments (Optional/Future)

## Description

Implement argument yanking functionality in the Nuru REPL, allowing users to quickly insert arguments from previous commands. This is a productivity feature commonly used in bash and PSReadLine.

**Note: This is an optional/future enhancement.**

## Parent

043_PSReadLine-REPL-Compatibility

## Requirements

- Argument parsing should handle quoted strings correctly
- Yank should work with command history
- Multiple consecutive yanks should cycle through arguments

## Checklist

### Last Argument Yanking (IMPLEMENT)
- [x] Alt+.: YankLastArg - Insert last argument from previous history entry
- [x] Alt+_: YankLastArg - Alternative binding
- [x] Consecutive Alt+. cycles through history (each press = last arg of older command)

### Nth Argument Yanking (IMPLEMENT)
- [x] Alt+Ctrl+Y: YankNthArg - Insert Nth argument from previous command
- [x] Alt+0 Alt+.: Yank 0th arg (command name) from previous
- [x] Alt+1 Alt+.: Yank 1st arg from previous
- [x] etc.

### Argument Extraction (IMPLEMENT)
- [x] Parse history entries into arguments
- [x] Handle quoted arguments: `"arg with spaces"`
- [x] Handle escaped characters: `arg\ with\ spaces`

### Testing
- [x] Test with various argument formats
- [x] Test cycling through history
- [x] Test Nth argument selection

## Notes

### PSReadLine Function Reference
| Function | Description |
|----------|-------------|
| YankLastArg | Insert the last argument from the previous history line |
| YankNthArg | Insert the Nth argument from the previous history line |

### YankLastArg Behavior
```
History:
  [1] git commit -m "Initial commit"
  [2] git push origin main
  [3] echo "done"

Current input: "|"
Alt+.: "done|"  (last arg of most recent: echo "done")
Alt+.: "main|"  (last arg of git push: main)
Alt+.: "Initial commit|"  (last arg of git commit, quoted string)
```

### YankNthArg Behavior
```
Previous command: git commit -m "Initial commit"
Arguments: [0]="git" [1]="commit" [2]="-m" [3]="Initial commit"

Alt+0 then Alt+.: inserts "git"
Alt+1 then Alt+.: inserts "commit"
Alt+3 then Alt+.: inserts "Initial commit"
```

### Argument Parsing
Need to parse command lines into arguments:
```csharp
// Simple case
"git push origin main" → ["git", "push", "origin", "main"]

// Quoted strings
"echo \"hello world\"" → ["echo", "hello world"]

// Escaped spaces
"touch file\\ name.txt" → ["touch", "file name.txt"]
```

Consider reusing existing parsing logic from the route parser if applicable.

### Consecutive Yank Detection
Track whether last command was YankLastArg:
- If yes, remove previously yanked text and insert from older history
- If no, insert last arg from most recent history

### Implementation Complexity
- Medium complexity
- Requires argument parsing logic
- State tracking for consecutive yanks
- Integration with history system

### Dependencies
- History navigation (043_002) should be complete
- May share argument parsing with route parsing

## Results

**Completed:** 2025-12-05

### What was implemented

**New File**: `source/timewarp-nuru-repl/input/repl-console-reader.yank-arg.cs`
- `HandleYankLastArg()` - Insert last argument from previous history entry (Alt+. / Alt+_)
- `HandleYankNthArg()` - Insert Nth argument from previous command (Alt+Ctrl+Y)
- `HandleDigitArgument()` - Handle Alt+0 through Alt+9 for digit prefix
- `ParseHistoryArguments()` - Parse command line into arguments with:
  - Double quote handling: `"arg with spaces"` → `arg with spaces`
  - Single quote handling: `'arg with spaces'` → `arg with spaces`
  - Escape handling: `arg\ with\ spaces` → `arg with spaces`
  - Escaped quotes inside quotes: `"say \"hello\""` → `say "hello"`

**Modified Files**:
- `repl-console-reader.kill-ring.cs` - Added call to `ResetYankArgTracking()` in `ResetKillTracking()`
- All 4 key binding profiles - Added bindings for Alt+., Alt+_, Alt+Ctrl+Y, and Alt+0-9

### Test coverage
- `repl-33-yank-arguments.cs`: 20 tests (9 parsing + 6 YankLastArg + 3 YankNthArg + 2 edge cases)

### Commit
`b616bfd` - feat(repl): implement PSReadLine YankLastArg and YankNthArg
