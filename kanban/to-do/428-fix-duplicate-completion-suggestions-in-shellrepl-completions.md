# Fix duplicate completion suggestions in shell/REPL completions

## Summary

Duplicate completion suggestions appear in TimeWarp.Nuru REPL and shell completions (bash, zsh, fish, PowerShell) because option completions are yielded from two overlapping code paths without deduplication.

**Example**: When typing `ganda nuget outdated --`, the user sees:
```
Available completions:
--help                --config              -c                    --include-prerelease
-p                    --package             --create-tasks        --config
-c                    --include-prerelease  -p                    --package
--create-tasks
```

Each option appears twice.

## Root Cause

The generated `GetCompletions` method in both `completion-emitter.cs` and `repl-emitter.cs` yields options from two sections:

1. **Context-aware route options** (lines 174-205) - fires when `prefix == "nuget outdated"`
2. **Global options** (lines 207-232) - fires when `currentInput.StartsWith("-")`

Both sections yield the same options without using the `yielded` HashSet for deduplication. The HashSet exists and is correctly used for command name completions, but was forgotten for option completions.

## Files to Modify

- `source/timewarp-nuru-analyzers/generators/emitters/completion-emitter.cs` (lines 186-228)
- `source/timewarp-nuru-analyzers/generators/emitters/repl-emitter.cs` (lines 186-228)

## Checklist

- [ ] Add `yielded.Add()` check to context-aware route options (long form)
- [ ] Add `yielded.Add()` check to context-aware route options (short form)
- [ ] Add `yielded.Add()` check to global options (long form)
- [ ] Add `yielded.Add()` check to global options (short form)
- [ ] Add unit test to verify no duplicate completions

## Fix Pattern

Change from:
```csharp
if ("--config".StartsWith(currentInput, ...))
    yield return new CompletionCandidate("--config", ...);
```

To:
```csharp
if ("--config".StartsWith(currentInput, ...) && yielded.Add("--config"))
    yield return new CompletionCandidate("--config", ...);
```

## Notes

The `yielded` HashSet is already declared at line 104-105 and used correctly for command completions. Need to apply same pattern to all four option yield locations (2 sections × 2 forms).

Full analysis: `.agent/workspace/2026-02-16T10-45-00_duplicate-completion-suggestions-analysis.md`
