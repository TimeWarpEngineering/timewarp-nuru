# Implement CandidateGenerator

## Description

Implement the `CandidateGenerator` that takes route match states and produces the final list of completion candidates. This component aggregates candidates from all viable routes, applies filtering, removes duplicates, and sorts results appropriately.

**Input**: `RouteMatchState[]` + `string? partialWord`
**Output**: `ReadOnlyCollection<CompletionCandidate>` (sorted, deduplicated)

## Parent

062_Redesign-CompletionEngine-Architecture

## Requirements

- Collect candidates from all viable route match states
- Filter by partial word (case-insensitive prefix matching)
- Remove duplicate candidates (same value)
- Sort by type priority (commands first, options last)
- Sort alphabetically within type groups
- Include enum values for enum parameters
- Include type hints for typed parameters
- Always include --help option where applicable

## Checklist

- [ ] Create `CandidateGenerator` class
- [ ] Implement `Generate(RouteMatchState[], string?)` method
- [ ] Aggregate candidates from all viable states
- [ ] Apply partial word filtering (case-insensitive)
- [ ] Remove duplicates by value
- [ ] Sort by type priority then alphabetically
- [ ] Handle enum parameter values
- [ ] Add --help to appropriate contexts
- [ ] Add comprehensive unit tests
- [ ] XML documentation

## Notes

### Implementation

```csharp
namespace TimeWarp.Nuru.Completion;

/// <summary>
/// Generates completion candidates from route match states.
/// </summary>
public class CandidateGenerator
{
  /// <summary>
  /// Generate completion candidates from match states.
  /// </summary>
  public ReadOnlyCollection<CompletionCandidate> Generate(
    IEnumerable<RouteMatchState> matchStates,
    string? partialWord)
  {
    var candidates = new List<CompletionCandidate>();
    
    foreach (var state in matchStates.Where(s => s.IsViableMatch))
    {
      candidates.AddRange(state.AvailableNext);
    }
    
    // Filter by partial word
    if (!string.IsNullOrEmpty(partialWord))
    {
      candidates = candidates
        .Where(c => c.Value.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase))
        .ToList();
    }
    
    // Remove duplicates, sort by type then value
    return candidates
      .GroupBy(c => c.Value)
      .Select(g => g.First())
      .OrderBy(c => GetTypeSortOrder(c.Type))
      .ThenBy(c => c.Value, StringComparer.OrdinalIgnoreCase)
      .ToList()
      .AsReadOnly();
  }
  
  private static int GetTypeSortOrder(CompletionType type) => type switch
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
```

### Sorting Priority

1. **Commands** (literals, subcommands) - most likely what user wants
2. **Enum values** - constrained set of valid values
3. **Parameters** - type hints like `<file>`, `<int>`
4. **Files/Directories** - path completions
5. **Custom** - user-defined completions
6. **Options** - flags come last (--verbose, -v)

### Partial Word Filtering

```csharp
// Input: partialWord = "--v"
// Candidates: ["--verbose", "-v", "--version", "status"]
// Result: ["--verbose", "--version"] (only those starting with "--v")
```

### --help Handling

The `--help` option should be available in most contexts:
- After any complete command
- After any subcommand
- Not when in the middle of typing something else

This may need to be added by CandidateGenerator if not in route options:
```csharp
// Add --help if not already present and context is appropriate
if (shouldAddHelp && !candidates.Any(c => c.Value == "--help"))
{
  candidates.Add(new CompletionCandidate("--help", "Show help", CompletionType.Option));
}
```

### Enum Value Generation

When a route expects an enum parameter and user is at that position:
```csharp
// Route: deploy {env:environment}
// Input: "deploy " (trailing space)
// Generate enum values: Dev, Staging, Prod
```

The RouteMatchEngine should already include these in AvailableNext, but CandidateGenerator may need to expand enum types.

### Test Cases

```csharp
// Empty partial - return all candidates
var candidates = generator.Generate(states, null);
candidates.Count.ShouldBeGreaterThan(0);

// Partial filters
var candidates = generator.Generate(states, "st");
candidates.All(c => c.Value.StartsWith("st", OrdinalIgnoreCase)).ShouldBeTrue();

// Duplicates removed
// If "status" appears in multiple routes, only one candidate
var candidates = generator.Generate(multipleRouteStates, null);
candidates.Count(c => c.Value == "status").ShouldBe(1);

// Sorted correctly
var candidates = generator.Generate(states, null);
var types = candidates.Select(c => c.Type).ToList();
// Commands before Options
types.IndexOf(CompletionType.Command).ShouldBeLessThan(types.LastIndexOf(CompletionType.Option));
```

### Location

Create in: `Source/TimeWarp.Nuru.Completion/Completion/CandidateGenerator.cs`

### Success Criteria

- [ ] Aggregates from all viable states
- [ ] Filters by partial correctly
- [ ] No duplicates in output
- [ ] Sorted by type, then alphabetically
- [ ] --help included appropriately
- [ ] Enum values expanded
- [ ] Well-tested and documented
