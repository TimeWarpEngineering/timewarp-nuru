# Fix Tab Completion Suggesting Command After Command

## Description

Tab completion incorrectly suggests the same command again after it's already been typed.

**Reproduction:**
1. Start REPL demo: `./repl-basic-demo.cs`
2. Type `h` then press Tab -> completes to `help`
3. Press Space then Tab -> suggests `help` again
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

- [x] Analyze CompletionProvider.GetCompletions() logic
- [x] Identify why it suggests commands after a complete command
- [x] Fix completion logic to be context-aware
- [x] Add test case for this scenario
- [x] Verify fix works in REPL demo

## Implementation Notes

### Root Cause

The bug was in `CompletionProvider.GetCompletions()` (lines 38-43):

```csharp
if (context.Args.Length <= 1)
{
  candidates.AddRange(GetCommandCompletions(endpoints, context.Args.ElementAtOrDefault(0) ?? ""));
  return [.. candidates];
}
```

When user types `"help "` (with trailing space) and presses Tab:
1. `CommandLineParser.Parse("help ")` returns `["help"]` (1 arg - trailing space is consumed)
2. Since `args.Length == 1`, it calls `GetCommandCompletions` with `"help"`
3. This returns all commands starting with "help" - including "help" itself

### Solution

1. Added `HasTrailingSpace` property to `CompletionContext` record
2. Updated `ReplConsoleReader.HandleTabCompletion()` to detect trailing whitespace
3. Updated `CompletionProvider.GetCompletions()` to check:
   - If `HasTrailingSpace == true` AND `Args.Length == 1`
   - Check if the single arg exactly matches a registered command
   - If so, return subcommand/option completions instead of re-suggesting the command

### Files Modified

- `Source/TimeWarp.Nuru.Completion/Completion/CompletionContext.cs` - Added `HasTrailingSpace` property
- `Source/TimeWarp.Nuru.Completion/Completion/CompletionProvider.cs` - Added `IsExactCommandMatch()` and `GetCompletionsAfterCommand()` methods
- `Source/TimeWarp.Nuru.Repl/Input/ReplConsoleReader.cs` - Detect trailing space and pass to context
- `Tests/TimeWarp.Nuru.Repl.Tests/repl-07-tab-completion-advanced.cs` - Added two test cases

### Test Results

- All 143 REPL tests pass (10/10 in repl-07)
- All 145 static completion tests pass
- All 121 dynamic completion tests pass
