# Implement RouteMatchEngine

## Description

Implement the `RouteMatchEngine` that takes parsed input and computes match state for each registered route. This is the core logic that determines which routes are viable matches and what completions should be offered next.

**Input**: `ParsedInput` + `EndpointCollection`
**Output**: `RouteMatchState[]` for all potentially matching routes

## Parent

062_Redesign-CompletionEngine-Architecture

## Requirements

- Match completed words against route segments (literals, parameters)
- Track which arguments have been consumed
- Track which options have been used
- Determine what can come next (literals, parameters, options)
- Handle subcommand hierarchies (git status, git commit, git log)
- Support partial matching for the current word
- Case-insensitive matching for literals and options
- Handle optional parameters and options correctly

## Checklist

- [ ] Create `RouteMatchState` record
- [ ] Create `RouteMatchEngine` class
- [ ] Implement `ComputeMatchStates(ParsedInput, EndpointCollection)` method
- [ ] Match literals (exact and partial)
- [ ] Match parameters (consume any value)
- [ ] Track option usage (don't re-suggest used options)
- [ ] Compute available next completions per route
- [ ] Handle enum parameters (provide values)
- [ ] Handle catch-all parameters
- [ ] Filter out non-viable routes early
- [ ] Add comprehensive unit tests
- [ ] XML documentation

## Notes

### Data Structures

```csharp
namespace TimeWarp.Nuru.Completion;

/// <summary>
/// Represents the match state of a single route against the input.
/// </summary>
public record RouteMatchState(
  Endpoint Endpoint,
  bool IsViableMatch,              // Could this route still match?
  int SegmentsMatched,             // Route segments consumed
  int ArgsConsumed,                // Input words consumed
  IReadOnlyList<string> OptionsUsed,  // Options already in input
  IReadOnlyList<CompletionCandidate> AvailableNext  // What can come next
);
```

### Matching Algorithm

```
For each route:
  1. Start with segmentIndex=0, argIndex=0
  
  2. For each completed word in input:
     - If current segment is Literal:
       - If word matches literal → advance both indices
       - Else → route not viable
     - If current segment is Parameter:
       - Consume word as parameter value → advance both indices
     - Check if word is an option:
       - If matches route option → record as used, advance arg only
  
  3. After processing completed words:
     - Route is viable if we didn't fail any match
     - AvailableNext = remaining segments + unused options
     
  4. If there's a partial word:
     - Filter AvailableNext to those matching partial
```

### Example Walkthrough

**Input**: `"backup data --com"` (ParsedInput: completed=["backup","data"], partial="--com")

**Route**: `backup {source} --compress,-c --output,-o {dest?}`

```
Step 1: Match "backup" against Literal("backup") → ✓
Step 2: Match "data" against Parameter("source") → ✓
Step 3: No more completed words
Step 4: AvailableNext = [--compress, -c, --output, -o, {dest?}]
Step 5: Filter by partial "--com" → [--compress]
```

**Result**: RouteMatchState with AvailableNext = [--compress]

### Handling Options

Options can appear anywhere after their required position. The engine should:
1. Collect all options from the input
2. Match them against route options
3. Don't include already-used options in AvailableNext
4. Options without values are boolean (just presence)
5. Options with values consume the next argument

### Handling Subcommands

Routes like `git status` and `git commit -m {msg}`:
- Both start with Literal("git")
- After matching "git", each route offers different next segments
- `git status` → no more required segments
- `git commit` → needs `-m` and `{message}`

### Edge Cases

1. **Empty input**: All routes viable, suggest first segments
2. **Complete command**: Route fully matched, suggest options only
3. **Too many args**: Route not viable (consumed all segments but more input)
4. **Partial option**: `--c` should match `--compress` and `--count`
5. **Case insensitivity**: `Git` should match `git`

### Location

Create in: `Source/TimeWarp.Nuru.Completion/Completion/RouteMatchEngine.cs`

### Test Cases

```csharp
// Empty input - all routes viable
var states = engine.ComputeMatchStates(new ParsedInput([], null, false), endpoints);
states.All(s => s.IsViableMatch).ShouldBeTrue();

// Partial command
var states = engine.ComputeMatchStates(new ParsedInput([], "g", false), endpoints);
states.Where(s => s.IsViableMatch).Select(s => s.AvailableNext)
  .ShouldContain("git", "greet");

// After command with space
var states = engine.ComputeMatchStates(new ParsedInput(["build"], null, true), endpoints);
var buildState = states.First(s => s.Endpoint.RoutePattern.StartsWith("build"));
buildState.AvailableNext.ShouldContain("--verbose", "-v");

// Partial option
var states = engine.ComputeMatchStates(new ParsedInput(["build"], "--v", false), endpoints);
var buildState = states.First(s => s.Endpoint.RoutePattern.StartsWith("build"));
buildState.AvailableNext.ShouldContain("--verbose");
```

### Success Criteria

- [ ] Correctly matches all route types
- [ ] Handles options at any position
- [ ] Subcommand hierarchies work
- [ ] Partial matching accurate
- [ ] Case insensitive
- [ ] Efficient (early bailout for non-viable routes)
- [ ] Well-tested and documented
