# Fix Tab Completion Suggesting Command After Command

## Description

Tab completion incorrectly suggests the same command again after it's already been typed.

**Reproduction:**
1. Start REPL demo: `./repl-basic-demo.cs`
2. Type `h` then press Tab → completes to `help`
3. Press Space then Tab → suggests `help` again
4. Result: `help help` which is invalid

**Expected Behavior:**
After completing a command that takes no arguments (like `help`), pressing Tab should either:
- Show nothing (no completions available)
- Show subcommands or options if the command supports them

**Actual Behavior:**
Tab completion suggests `help` again, resulting in `help help`

## Requirements

1. Investigate CompletionProvider logic for when a command is already complete
2. Tab completion should be context-aware - after a complete command with no args, don't suggest commands again
3. If a command takes arguments, show argument completions (e.g., `greet <tab>` might show parameter hints)

## Checklist

- [ ] Analyze CompletionProvider.GetCompletions() logic
- [ ] Identify why it suggests commands after a complete command
- [ ] Fix completion logic to be context-aware
- [ ] Add test case for this scenario
- [ ] Verify fix works in REPL demo

## Notes

- The completion context should know when the cursor is positioned after a complete command
- Related files: `Source/TimeWarp.Nuru.Repl/Completion/CompletionProvider.cs`, `Source/TimeWarp.Nuru.Completion/`
- This is a UX issue that makes tab completion confusing for users
