# CompletionEngine Architecture Redesign

**Task**: 062 Redesign CompletionEngine Architecture
**Date**: 2025-11-26
**Author**: Claude Code Assistant

---

## Executive Summary

This document defines the architecture for a unified CompletionEngine to replace the current ad-hoc `CompletionProvider`. The new design uses a pipeline architecture to eliminate the 3 separate code paths causing 18 documented bugs.

---

## Problem Analysis

### Current Architecture Issues

The current `CompletionProvider.cs` (568 lines) has **3 separate code paths**:

```
PATH A: args.Length <= 1
  └─ GetCommandCompletions() + GetCompletionsAfterCommand()
  └─ Decision point: line 39 (if args.Length <= 1)
  └─ Further branch: line 46 (if HasTrailingSpace && args.Length == 1)

PATH B: Iterate all endpoints (lines 61-68)
  └─ GetCompletionsForRoute() for each endpoint
  └─ Complex state tracking: segmentIndex, argIndex (lines 270-376)
  └─ Cursor position logic: cursorPosition comparisons

PATH C: Option-specific (triggered within PATH B)
  └─ GetOptionCompletions() (lines 450-487)
  └─ Only called when currentWord.StartsWith('-')
```

### Root Cause Analysis

| Bug # | Root Cause | Code Location |
|-------|-----------|---------------|
| 1-3 | Options not added in PATH A's GetCompletionsAfterCommand() | Lines 160-255 |
| 4-10 | Partial option matching only in GetOptionCompletions() | Line 387: `currentWord.StartsWith('-')` check |
| 11 | Case sensitivity varies by code path | Inconsistent StringComparison usage |
| 12 | No explicit state reset mechanism | No CompletionState abstraction |
| 13-18 | --help not injected by any code path | Missing from all 3 paths |

### Bug Categories Mapped to Code Paths

```
┌────────────────────┬────────────────────┬────────────────────┐
│ PATH A             │ PATH B             │ PATH C             │
│ (args.Length <= 1) │ (route iteration)  │ (options)          │
├────────────────────┼────────────────────┼────────────────────┤
│ Bug #1 (options    │ Bug #4-10 (partial │ Bug #11 (case)     │
│ not shown)         │ options don't      │                    │
│                    │ complete)          │                    │
│ Bug #2 (options    │ Bug #12 (state     │                    │
│ after param)       │ leaks)             │                    │
│                    │                    │                    │
│ Bug #3 (multiple   │                    │                    │
│ options)           │                    │                    │
│                    │                    │                    │
│ Bug #13-18 (--help │                    │                    │
│ missing)           │                    │                    │
└────────────────────┴────────────────────┴────────────────────┘
```

---

## Proposed Architecture

### Design Principles

1. **Single Code Path**: All input goes through the same pipeline
2. **Explicit State**: State is tracked in immutable data structures
3. **Composable Components**: Each stage is independently testable
4. **Predictable Behavior**: Same input always produces same output

### Pipeline Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│ Raw Input       │ ──► │ InputTokenizer  │ ──► │ RouteMatchEngine│ ──► │ CandidateGen    │
│ "backup da"     │     │                 │     │                 │     │                 │
└─────────────────┘     └─────────────────┘     └─────────────────┘     └─────────────────┘
                               │                        │                        │
                               ▼                        ▼                        ▼
                        ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
                        │ ParsedInput     │     │ MatchState[]    │     │ Completions     │
                        │ - CompletedWords│     │ - Matched segs  │     │ - Candidates    │
                        │ - PartialWord   │     │ - Consumed args │     │ - Sorted        │
                        │ - HasTrailing   │     │ - Available next│     │ - Deduped       │
                        └─────────────────┘     └─────────────────┘     └─────────────────┘
```

### Orchestration Flow

```csharp
public class CompletionEngine
{
    public ReadOnlyCollection<CompletionCandidate> GetCompletions(
        string input,
        bool hasTrailingSpace,
        EndpointCollection endpoints)
    {
        // Stage 1: Tokenize input
        ParsedInput parsed = InputTokenizer.Tokenize(input, hasTrailingSpace);

        // Stage 2: Match against all routes
        IReadOnlyList<RouteMatchState> matches = RouteMatchEngine.Match(
            parsed,
            endpoints);

        // Stage 3: Generate candidates from viable matches
        var candidates = CandidateGenerator.Generate(matches, parsed.PartialWord);

        // Stage 4: Inject global options (--help)
        candidates = InjectGlobalOptions(candidates, parsed);

        // Stage 5: Sort, dedupe, and return
        return FinalizeCompletions(candidates);
    }
}
```

---

## Component Specifications

### 1. InputTokenizer

**Purpose**: Parse raw input into structured tokens, clearly identifying what is being completed.

**Interface**:
```csharp
/// <summary>
/// Tokenizes command-line input for completion analysis.
/// </summary>
public interface IInputTokenizer
{
    /// <summary>
    /// Parse raw input string into structured tokens.
    /// </summary>
    /// <param name="input">The raw command-line input.</param>
    /// <param name="hasTrailingSpace">True if input ends with whitespace.</param>
    /// <returns>Parsed input structure.</returns>
    ParsedInput Tokenize(string input, bool hasTrailingSpace);
}
```

**Data Structure**:
```csharp
/// <summary>
/// Represents tokenized command-line input ready for completion analysis.
/// </summary>
/// <param name="CompletedWords">Fully typed words (not being actively edited).</param>
/// <param name="PartialWord">The word being typed (null if user pressed space after last word).</param>
/// <param name="HasTrailingSpace">True if input ends with whitespace.</param>
/// <param name="CursorWordIndex">Index of the word at cursor (for future cursor-in-middle support).</param>
public record ParsedInput(
    IReadOnlyList<string> CompletedWords,
    string? PartialWord,
    bool HasTrailingSpace,
    int CursorWordIndex
);
```

**Behavior Examples**:

| Input | HasTrailingSpace | CompletedWords | PartialWord | CursorWordIndex |
|-------|------------------|----------------|-------------|-----------------|
| `""` | false | [] | null | 0 |
| `"s"` | false | [] | "s" | 0 |
| `"status"` | false | [] | "status" | 0 |
| `"status "` | true | ["status"] | null | 1 |
| `"backup da"` | false | ["backup"] | "da" | 1 |
| `"backup data "` | true | ["backup", "data"] | null | 2 |
| `"backup --c"` | false | ["backup"] | "--c" | 1 |
| `"backup -c "` | true | ["backup", "-c"] | null | 2 |

**Key Design Decision**: `PartialWord` is null when `HasTrailingSpace` is true. This clearly indicates "user wants to see ALL possibilities for the next position" vs "user wants to filter by what they've typed".

---

### 2. RouteMatchEngine

**Purpose**: Determine which routes are viable matches and what can come next for each.

**Interface**:
```csharp
/// <summary>
/// Matches parsed input against registered routes.
/// </summary>
public interface IRouteMatchEngine
{
    /// <summary>
    /// Find all viable route matches for the given input.
    /// </summary>
    /// <param name="input">Parsed and tokenized input.</param>
    /// <param name="endpoints">All registered endpoints.</param>
    /// <returns>Match states for all viable routes.</returns>
    IReadOnlyList<RouteMatchState> Match(
        ParsedInput input,
        EndpointCollection endpoints);
}
```

**Data Structure**:
```csharp
/// <summary>
/// Represents the match state of a single route against input.
/// </summary>
/// <param name="Endpoint">The endpoint being matched.</param>
/// <param name="IsViable">True if route could still match with more input.</param>
/// <param name="IsExactMatch">True if route matches exactly with no more required input.</param>
/// <param name="SegmentsMatched">Number of route segments successfully matched.</param>
/// <param name="ArgsConsumed">Number of input arguments consumed.</param>
/// <param name="OptionsUsed">Set of option names already present in input.</param>
/// <param name="NextCandidates">What can come next for this route.</param>
public record RouteMatchState(
    Endpoint Endpoint,
    bool IsViable,
    bool IsExactMatch,
    int SegmentsMatched,
    int ArgsConsumed,
    IReadOnlySet<string> OptionsUsed,
    IReadOnlyList<NextCandidate> NextCandidates
);

/// <summary>
/// Represents what can come next at a particular position.
/// </summary>
/// <param name="Kind">Type of candidate (Literal, Parameter, Option).</param>
/// <param name="Value">The value to complete (e.g., "status", "--verbose").</param>
/// <param name="AlternateValue">Alternate form if applicable (e.g., "-v" for "--verbose").</param>
/// <param name="Description">Description for UI display.</param>
/// <param name="ParameterType">For parameters, the expected type.</param>
/// <param name="IsRequired">True if this is required to complete the route.</param>
public record NextCandidate(
    CandidateKind Kind,
    string Value,
    string? AlternateValue,
    string? Description,
    Type? ParameterType,
    bool IsRequired
);

/// <summary>
/// Kind of completion candidate.
/// </summary>
public enum CandidateKind
{
    Literal,      // Command/subcommand
    Parameter,    // Required or optional parameter
    Option        // Long or short form option
}
```

**Matching Algorithm**:

```
FOR each endpoint in endpoints:
    state = new MatchState(endpoint)

    FOR each completed word in input.CompletedWords:
        IF current segment is LiteralMatcher:
            IF exact match (case-insensitive):
                advance segment, consume word
            ELSE:
                mark NOT viable, break
        ELSE IF current segment is ParameterMatcher:
            consume word, advance segment
        ELSE IF word looks like option:
            IF matches any option in route:
                record option as used
                IF option expects value AND next word exists:
                    consume value word too
            ELSE:
                mark NOT viable, break

    IF state still viable:
        determine NextCandidates based on:
        - Remaining required segments
        - Available options (excluding used ones)
        - Current partial word filter

    add state to results
```

**Edge Cases**:

1. **Catch-all parameters**: Mark route as viable but suggest no more after catch-all
2. **Optional parameters**: Include in NextCandidates as non-required
3. **Repeated options**: Include in NextCandidates even if already used
4. **Multiple literal matches**: All get marked viable for prefix matching

---

### 3. CandidateGenerator

**Purpose**: Generate final completion candidates from match states.

**Interface**:
```csharp
/// <summary>
/// Generates completion candidates from route match states.
/// </summary>
public interface ICandidateGenerator
{
    /// <summary>
    /// Generate completion candidates from viable matches.
    /// </summary>
    /// <param name="matches">Route match states.</param>
    /// <param name="partialWord">Partial word for filtering (null = show all).</param>
    /// <returns>Filtered and deduplicated candidates.</returns>
    IEnumerable<CompletionCandidate> Generate(
        IReadOnlyList<RouteMatchState> matches,
        string? partialWord);
}
```

**Generation Logic**:

```csharp
public IEnumerable<CompletionCandidate> Generate(
    IReadOnlyList<RouteMatchState> matches,
    string? partialWord)
{
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var match in matches.Where(m => m.IsViable))
    {
        foreach (var next in match.NextCandidates)
        {
            // Filter by partial word if present
            if (!string.IsNullOrEmpty(partialWord))
            {
                bool matches = next.Value.StartsWith(
                    partialWord,
                    StringComparison.OrdinalIgnoreCase);

                // Also check alternate form for options
                if (!matches && next.AlternateValue != null)
                {
                    matches = next.AlternateValue.StartsWith(
                        partialWord,
                        StringComparison.OrdinalIgnoreCase);
                }

                if (!matches) continue;
            }

            // Deduplicate
            if (seen.Add(next.Value))
            {
                yield return new CompletionCandidate(
                    next.Value,
                    next.Description,
                    MapToCompletionType(next.Kind, next.ParameterType)
                );
            }

            // Also yield alternate form for options
            if (next.AlternateValue != null && seen.Add(next.AlternateValue))
            {
                // Only if partial matches alternate OR no partial
                if (string.IsNullOrEmpty(partialWord) ||
                    next.AlternateValue.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase))
                {
                    yield return new CompletionCandidate(
                        next.AlternateValue,
                        next.Description,
                        CompletionType.Option
                    );
                }
            }
        }
    }
}
```

**Special Handling**:

1. **Enum Parameters**: Expand to individual enum values
2. **File/Directory Parameters**: Return placeholder hints
3. **Option Deduplication**: Same option from multiple routes appears once

---

### 4. Global Option Injection

**Purpose**: Inject options that should always be available (e.g., --help).

```csharp
private IEnumerable<CompletionCandidate> InjectGlobalOptions(
    IEnumerable<CompletionCandidate> candidates,
    ParsedInput input)
{
    var candidateList = candidates.ToList();

    // --help is always available after at least one command word
    if (input.CompletedWords.Count > 0 ||
        (input.CompletedWords.Count == 0 && input.HasTrailingSpace))
    {
        string? partial = input.PartialWord;

        if (string.IsNullOrEmpty(partial) ||
            "--help".StartsWith(partial, StringComparison.OrdinalIgnoreCase))
        {
            // Only add if not already present
            if (!candidateList.Any(c =>
                c.Value.Equals("--help", StringComparison.OrdinalIgnoreCase)))
            {
                candidateList.Add(new CompletionCandidate(
                    "--help",
                    "Show help for this command",
                    CompletionType.Option
                ));
            }
        }
    }

    return candidateList;
}
```

---

### 5. CompletionEngine (Orchestrator)

**Complete Interface**:

```csharp
/// <summary>
/// Unified completion engine using pipeline architecture.
/// Replaces the previous ad-hoc CompletionProvider.
/// </summary>
public sealed class CompletionEngine : ICompletionEngine
{
    private readonly IInputTokenizer Tokenizer;
    private readonly IRouteMatchEngine MatchEngine;
    private readonly ICandidateGenerator CandidateGenerator;
    private readonly ITypeConverterRegistry TypeConverterRegistry;
    private readonly ILogger<CompletionEngine>? Logger;

    public CompletionEngine(
        ITypeConverterRegistry typeConverterRegistry,
        IInputTokenizer? tokenizer = null,
        IRouteMatchEngine? matchEngine = null,
        ICandidateGenerator? candidateGenerator = null,
        ILogger<CompletionEngine>? logger = null)
    {
        TypeConverterRegistry = typeConverterRegistry;
        Tokenizer = tokenizer ?? new InputTokenizer();
        MatchEngine = matchEngine ?? new RouteMatchEngine();
        CandidateGenerator = candidateGenerator ?? new DefaultCandidateGenerator(typeConverterRegistry);
        Logger = logger;
    }

    public ReadOnlyCollection<CompletionCandidate> GetCompletions(
        CompletionContext context,
        EndpointCollection endpoints)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(endpoints);

        // Build input string from args for tokenization
        string input = string.Join(" ", context.Args);

        Logger?.LogDebug(
            "GetCompletions: input='{Input}', hasTrailingSpace={HasTrailing}",
            input, context.HasTrailingSpace);

        // Stage 1: Tokenize
        ParsedInput parsed = Tokenizer.Tokenize(input, context.HasTrailingSpace);

        Logger?.LogDebug(
            "Tokenized: CompletedWords=[{Words}], PartialWord='{Partial}'",
            string.Join(", ", parsed.CompletedWords),
            parsed.PartialWord ?? "(null)");

        // Stage 2: Match routes
        IReadOnlyList<RouteMatchState> matches = MatchEngine.Match(parsed, endpoints);

        Logger?.LogDebug(
            "Matched {ViableCount} viable routes out of {TotalCount}",
            matches.Count(m => m.IsViable),
            endpoints.Endpoints.Count);

        // Stage 3: Generate candidates
        var candidates = CandidateGenerator.Generate(matches, parsed.PartialWord).ToList();

        // Stage 4: Inject global options
        candidates = InjectGlobalOptions(candidates, parsed).ToList();

        // Stage 5: Sort and return
        return FinalizeCompletions(candidates);
    }

    private ReadOnlyCollection<CompletionCandidate> FinalizeCompletions(
        IEnumerable<CompletionCandidate> candidates)
    {
        return candidates
            .GroupBy(c => c.Value, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(c => GetTypeSortOrder(c.Type))
            .ThenBy(c => c.Value, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    private static int GetTypeSortOrder(CompletionType type)
    {
        return type switch
        {
            CompletionType.Command => 0,
            CompletionType.Enum => 1,
            CompletionType.Parameter => 2,
            CompletionType.File => 3,
            CompletionType.Directory => 4,
            CompletionType.Custom => 5,
            CompletionType.Option => 6,
            _ => 99
        };
    }
}
```

---

## How This Design Fixes Each Bug

| Bug # | Description | How Fixed |
|-------|-------------|-----------|
| #1 | Options not shown after simple commands | RouteMatchEngine includes options in NextCandidates for all viable routes |
| #2 | Options not shown after parameter | Same - options always in NextCandidates regardless of segment position |
| #3 | Multiple options not shown | CandidateGenerator aggregates options from all viable routes |
| #4-10 | Partial option completion broken | CandidateGenerator.Generate() uses `StartsWith` with `OrdinalIgnoreCase` |
| #11 | Case sensitivity inconsistent | All comparisons use `StringComparison.OrdinalIgnoreCase` |
| #12 | State leaks between attempts | No mutable state - all operations on immutable records |
| #13-18 | --help not available | InjectGlobalOptions() adds --help after any command |

---

## Test Categories Coverage

All 20 test categories from task 061 are supported:

| Category | Component Responsible |
|----------|----------------------|
| 1. Empty input | InputTokenizer handles empty string |
| 2. Partial command | CandidateGenerator filters by PartialWord |
| 3. After complete command | ParsedInput.HasTrailingSpace = true |
| 4. Subcommand completion | RouteMatchEngine tracks SegmentsMatched |
| 5. Option completion (-) | CandidateGenerator includes short forms |
| 6. Option completion (--) | CandidateGenerator includes long forms |
| 7. Enum parameter | CandidateGenerator expands enum values |
| 8. Mid-word completion | ParsedInput.PartialWord captures partial |
| 9. Completion after options | RouteMatchEngine tracks OptionsUsed |
| 10. Multiple tabs | REPL handles cycling (not this component) |
| 11. Shift+Tab | REPL handles reverse (not this component) |
| 12. Alt+= show all | REPL handles (not this component) |
| 13. Escape cancel | REPL handles (not this component) |
| 14. Typing after completion | REPL handles (not this component) |
| 15. Backspace during completion | REPL handles (not this component) |
| 16. Complex option sequences | RouteMatchEngine handles option+value consumption |
| 17. Invalid completion contexts | RouteMatchEngine returns empty NextCandidates |
| 18. Case sensitivity | OrdinalIgnoreCase everywhere |
| 19. Help option | InjectGlobalOptions() |
| 20. Edge cases | Immutable design prevents state corruption |

---

## File Structure

```
Source/TimeWarp.Nuru.Completion/
├── Completion/
│   ├── CompletionCandidate.cs        (existing)
│   ├── CompletionContext.cs          (existing)
│   ├── CompletionType.cs             (existing)
│   ├── CompletionProvider.cs         (existing - TO BE REPLACED)
│   │
│   ├── Engine/                       (NEW)
│   │   ├── CompletionEngine.cs       (orchestrator)
│   │   ├── ICompletionEngine.cs      (interface)
│   │   │
│   │   ├── Tokenizing/
│   │   │   ├── IInputTokenizer.cs
│   │   │   ├── InputTokenizer.cs
│   │   │   └── ParsedInput.cs
│   │   │
│   │   ├── Matching/
│   │   │   ├── IRouteMatchEngine.cs
│   │   │   ├── RouteMatchEngine.cs
│   │   │   ├── RouteMatchState.cs
│   │   │   └── NextCandidate.cs
│   │   │
│   │   └── Generation/
│   │       ├── ICandidateGenerator.cs
│   │       └── DefaultCandidateGenerator.cs
```

---

## Migration Strategy

### Phase 1: Implement New Components (Tasks 063-065)
- Implement InputTokenizer (Task 063)
- Implement RouteMatchEngine (Task 064)
- Implement CandidateGenerator (Task 065)

### Phase 2: Integration (Task 066)
- Create CompletionEngine orchestrator
- Add integration tests comparing old vs new output
- Wire up via DI in NuruAppBuilder

### Phase 3: Replacement
- Update REPL to use new ICompletionEngine
- Deprecate old CompletionProvider
- Remove old implementation after validation

### Backward Compatibility
- CompletionContext remains unchanged
- CompletionCandidate remains unchanged
- CompletionType remains unchanged
- Only internal implementation changes

---

## Appendix: Architecture Diagram (ASCII)

```
                    ┌─────────────────────────────────────────────────────────┐
                    │                    CompletionEngine                      │
                    │                     (Orchestrator)                       │
                    └─────────────────────────────────────────────────────────┘
                                            │
                    ┌───────────────────────┼───────────────────────┐
                    │                       │                       │
                    ▼                       ▼                       ▼
         ┌─────────────────┐    ┌─────────────────────┐    ┌─────────────────┐
         │ InputTokenizer  │    │  RouteMatchEngine   │    │ CandidateGen    │
         │                 │    │                     │    │                 │
         │ • Tokenize()    │    │ • Match()           │    │ • Generate()    │
         └─────────────────┘    └─────────────────────┘    └─────────────────┘
                 │                       │                         │
                 ▼                       ▼                         ▼
         ┌─────────────────┐    ┌─────────────────────┐    ┌─────────────────┐
         │  ParsedInput    │    │  RouteMatchState[]  │    │ Completion      │
         │                 │    │                     │    │ Candidate[]     │
         │ • CompletedWords│    │ • Endpoint          │    │                 │
         │ • PartialWord   │    │ • IsViable          │    │ • Value         │
         │ • HasTrailing   │    │ • NextCandidates    │    │ • Description   │
         │ • CursorIndex   │    │ • OptionsUsed       │    │ • Type          │
         └─────────────────┘    └─────────────────────┘    └─────────────────┘

                    ┌─────────────────────────────────────────────────────────┐
                    │                Data Flow Example                         │
                    ├─────────────────────────────────────────────────────────┤
                    │                                                         │
                    │  Input: "backup da" (no trailing space)                 │
                    │                                                         │
                    │  1. Tokenize:                                           │
                    │     CompletedWords: ["backup"]                          │
                    │     PartialWord: "da"                                   │
                    │     HasTrailingSpace: false                             │
                    │                                                         │
                    │  2. Match Routes:                                       │
                    │     Route "backup {source}" → Viable                    │
                    │       NextCandidates: [{Parameter, source, ...}]        │
                    │     Route "backup {source} --compress" → Viable         │
                    │       NextCandidates: [{Parameter}, {Option, --compress}]│
                    │     Route "deploy {env}" → NOT Viable                   │
                    │                                                         │
                    │  3. Generate Candidates:                                │
                    │     Filter by "da": (parameter hints match)             │
                    │     Output: [data, database, daily, ...]                │
                    │                                                         │
                    │  4. Inject Global:                                      │
                    │     "--help" doesn't start with "da" → skip             │
                    │                                                         │
                    │  5. Finalize:                                           │
                    │     Sort, dedupe, return                                │
                    │                                                         │
                    └─────────────────────────────────────────────────────────┘
```

---

## Success Criteria Checklist

- [x] Architecture document complete
- [x] All interfaces defined (IInputTokenizer, IRouteMatchEngine, ICandidateGenerator)
- [x] Data structures specified (ParsedInput, RouteMatchState, NextCandidate)
- [x] Design reviewed against test categories (all 20 covered)
- [x] Migration strategy defined
- [x] Bug fix mapping documented
- [x] Ready for implementation in tasks 063-066
