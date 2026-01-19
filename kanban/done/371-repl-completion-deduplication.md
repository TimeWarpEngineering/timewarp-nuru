# REPL Completion Deduplication

## Description

The REPL completion system yields duplicate entries for commands that share a common prefix. For example, when typing `g` + Tab, the completion shows:

```
git  git  git  greet
```

The word `git` appears 3 times because there are 3 routes starting with `git`:
- `git status`
- `git commit -m {message}`
- `git log --count {n:int}`

This causes issues with:
1. **Visual clutter** - Same word shown multiple times
2. **Unique match detection** - When user types `gi` + Tab, it should auto-complete to `git` (unique match), but the REPL sees 3 separate `git` entries and cycles through them instead

## Failing Test

`repl-17-sample-validation.cs`:
- `Should_show_completions_and_autocomplete_unique_match`

## Root Cause

In `repl-emitter.cs`, the command prefix completion logic yields one entry per route:

```csharp
foreach (string cmd in CommandPrefixes)
{
  // Extract first word
  string firstWord = cmd;
  int spaceIdx = cmd.IndexOf(' ');
  if (spaceIdx >= 0) firstWord = cmd[..spaceIdx];

  // This yields "git" 3 times for git status, git commit, git log
  yield return new CompletionCandidate(firstWord, ...);
}
```

## Proposed Fix

Deduplicate completions in the generated `GetCompletions()` method using a `HashSet<string>`:

```csharp
HashSet<string> yieldedCommands = new(StringComparer.OrdinalIgnoreCase);

foreach (string cmd in CommandPrefixes)
{
  string firstWord = ...;
  if (yieldedCommands.Add(firstWord))
  {
    yield return new CompletionCandidate(firstWord, ...);
  }
}
```

Or deduplicate at the REPL session level when processing completions for display/cycling.

## Priority

Low - Cosmetic issue, core completion functionality works

## Blocked By

None

## Blocks

- Full completion of #368
