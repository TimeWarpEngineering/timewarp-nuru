# Duplicate Completion Suggestions Bug Analysis

## Executive Summary

Duplicate completion suggestions appear in TimeWarp.Nuru REPL/shell completions because option completions are yielded from **two overlapping code paths** without proper deduplication using the `yielded` HashSet.

## Scope

Analyzed the TimeWarp.Nuru source generators (`completion-emitter.cs` and `repl-emitter.cs`) to understand why `ganda nuget outdated` shows duplicate options like:
- `--help`, `--help`
- `--config`, `-c`, `--config`, `-c`
- etc.

## Methodology

1. Examined `NuGetOutdatedCommand.cs` in the Ganda project to confirm option definitions
2. Traced the completion generation in `completion-emitter.cs` and `repl-emitter.cs`
3. Identified the `yielded` HashSet mechanism for deduplication
4. Found the bug in generated code emission logic

## Findings

### Bug Location

**Files:**
- `/source/timewarp-nuru-analyzers/generators/emitters/completion-emitter.cs` (lines 174-232)
- `/source/timewarp-nuru-analyzers/generators/emitters/repl-emitter.cs` (lines 175-233)

### Root Cause

The generated `GetCompletions` method yields completions from two separate sections:

#### Section 1: Context-Aware Route Options (lines 174-205)
```csharp
// Context-aware route option completions
foreach (CompletionDataExtractor.RouteOptionInfo routeOpt in routeOptions)
{
    // If prefix matches "nuget outdated"...
    if (string.Equals(prefix, "nuget outdated", ...))
    {
        // Yields options WITHOUT checking yielded HashSet
        yield return new CompletionCandidate("--config", ...);
        yield return new CompletionCandidate("-c", ...);
        // etc.
    }
}
```

#### Section 2: Global Options (lines 207-232)
```csharp
// Global option completions (when typing - or --)
if (currentInput.StartsWith("-", StringComparison.Ordinal))
{
    // ALSO yields options WITHOUT checking yielded HashSet
    yield return new CompletionCandidate("--config", ...);
    yield return new CompletionCandidate("-c", ...);
    // etc.
}
```

### The Overlap

When user types `nuget outdated -`:
1. **Section 1 fires**: `prefix == "nuget outdated"` matches → yields command's options
2. **Section 2 fires**: `currentInput.StartsWith("-")` is true → yields ALL matching options again

Both sections fire, both yield, no deduplication = **duplicates**.

### The `yielded` HashSet

The method correctly declares a HashSet for deduplication (line 104-105):
```csharp
// Track yielded completions to prevent duplicates
global::System.Collections.Generic.HashSet<string> yielded = new(...);
```

And uses it correctly for command completions (lines 121, 133):
```csharp
if (nextWord.StartsWith(...) && yielded.Add(nextWord))  // ✓ Correct usage
```

But **does not use it** for either option completion section.

## Comparison

| Completion Type | Uses `yielded` HashSet? | Result |
|-----------------|------------------------|--------|
| Command names | ✓ Yes | No duplicates |
| Options (context-aware) | ✗ No | **Duplicates possible** |
| Options (global) | ✗ No | **Duplicates possible** |

## Impact

- Affects all commands with options when user types partial option prefix
- Visible in REPL mode (interactive CLI)
- Visible in shell completion scripts (bash, zsh, fish, powershell)

## Recommendations

### Fix Priority: High

### Solution

**Add `yielded.Add()` checks to both option completion sections:**

#### For completion-emitter.cs (lines 186-199):
```csharp
// Before
if (opt.LongForm is not null)
{
    sb.AppendLine($"        if ((string.IsNullOrEmpty(currentInput) || \"--{opt.LongForm}\".StartsWith(...))");
    sb.AppendLine($"          yield return new CompletionCandidate(\"--{opt.LongForm}\", ...);");
}

// After  
if (opt.LongForm is not null)
{
    sb.AppendLine($"        if ((string.IsNullOrEmpty(currentInput) || \"--{opt.LongForm}\".StartsWith(...)) && yielded.Add(\"--{opt.LongForm}\"))");
    sb.AppendLine($"          yield return new CompletionCandidate(\"--{opt.LongForm}\", ...);");
}
```

Apply same pattern to:
- Long form in context-aware section (line 189-191)
- Short form in context-aware section (line 196-198)
- Long form in global section (line 219-221)
- Short form in global section (line 226-228)

### Files to Modify

1. `/source/timewarp-nuru-analyzers/generators/emitters/completion-emitter.cs`
2. `/source/timewarp-nuru-analyzers/generators/emitters/repl-emitter.cs`

### Test Addition

Add a test to verify no duplicate completions:
```csharp
[Fact]
public async Task Should_not_produce_duplicate_option_completions()
{
    // Arrange
    NuruApp app = NuruApp.CreateBuilder([])
        .Map("test").WithHandler(() => "ok")
        .WithOptions([
            new OptionDefinition("verbose", "v", "Enable verbose output", false, null)
        ])
        .AsCommand().Done()
        .Build();

    // Act
    IEnumerable<CompletionCandidate> completions = 
        app.GetCompletions(["test", "-"], hasTrailingSpace: false);

    // Assert
    List<string> values = completions.Select(c => c.Value).ToList();
    values.Count.ShouldBe(values.Distinct().Count(), "No duplicate completions should be returned");
}
```

## References

- Generated code: `completion-emitter.cs` lines 174-232
- Generated code: `repl-emitter.cs` lines 175-233
- Option extraction: `completion-data-extractor.cs` methods `ExtractOptions()` and `ExtractRouteOptions()`
- Ganda command: `/worktrees/.../ganda/.../NuGetOutdatedCommand.cs` (defines `--config`/`-c`, `--include-prerelease`/`-p`, `--package`, `--create-tasks`)