# Redesign CompletionEngine Architecture

## Description

Replace the current ad hoc completion logic in `CompletionProvider.cs` with a unified state machine architecture. The current implementation has 3 separate code paths with inconsistent logic, causing 5 categories of bugs. This task designs the new architecture that will be implemented in subsequent tasks.

**Problem**: Current `CompletionProvider` (568 lines) uses heuristic-based decisions scattered across:
- `GetCompletions()` → decides based on args.Length <= 1
- `GetCompletionsAfterCommand()` → separate logic for "after first word"
- `GetCompletionsForRoute()` → yet another path for multi-word
- `GetOptionCompletions()` → option-specific logic

**Solution**: Unified pipeline: `Input → TokenParser → RouteMatcher → CandidateGenerator → Completions`

## Requirements

- Document the new architecture with clear interfaces
- Define data structures for completion state
- Specify how each component interacts
- Ensure design handles all 20 test categories from task 061
- Design must support:
  - Partial word completion
  - Option completion (-, --)
  - Enum parameter completion
  - Subcommand hierarchies
  - Case-insensitive matching
  - Trailing space detection

## Checklist

- [x] Analyze current CompletionProvider code paths
- [x] Document bug categories and root causes
- [x] Design InputTokenizer interface and data structures
- [x] Design RouteMatchState for tracking match progress
- [x] Design CandidateGenerator interface
- [x] Define CompletionEngine orchestration
- [x] Create architecture diagram
- [x] Review design against all 20 test categories
- [x] Document design in `documentation/developer/design/completion/`

## Notes

### Current Architecture Problems

**3 Separate Code Paths**:
```
PATH A: args.Length <= 1
  └─ GetCommandCompletions() + GetCompletionsAfterCommand()

PATH B: Iterate all endpoints  
  └─ GetCompletionsForRoute() for each endpoint

PATH C: Option-specific
  └─ GetOptionCompletions()
```

**Bug Categories Caused**:
1. Options not shown after command/parameter
2. Partial option completion broken (--l → --limit)
3. Case sensitivity inconsistent
4. Subcommand context lost
5. --help option inconsistent

### Proposed Architecture

**Unified Pipeline**:
```
┌─────────────┐     ┌──────────────┐     ┌───────────────────┐     ┌─────────────┐
│ Raw Input   │ ──► │ InputTokenizer│ ──► │ RouteMatchEngine  │ ──► │ Candidate   │
│ "backup da" │     │              │     │                   │     │ Generator   │
└─────────────┘     └──────────────┘     └───────────────────┘     └─────────────┘
                           │                      │                       │
                           ▼                      ▼                       ▼
                    ┌──────────────┐     ┌───────────────────┐     ┌─────────────┐
                    │ ParsedInput  │     │ RouteMatchState[] │     │ Completions │
                    │ - Words[]    │     │ - MatchedSegments │     │ - Candidates│
                    │ - PartialWord│     │ - ConsumedArgs    │     │ - Sorted    │
                    │ - HasTrailing│     │ - AvailableNext   │     │ - Deduped   │
                    └──────────────┘     └───────────────────┘     └─────────────┘
```

### Key Design Decisions

**1. InputTokenizer** (Task 063)
```csharp
record ParsedInput(
  string[] CompletedWords,    // Fully typed words
  string? PartialWord,        // Word being typed (null if trailing space)
  bool HasTrailingSpace,      // User pressed space after last word
  int CursorPosition          // Character position (for future use)
);
```

**2. RouteMatchState** (Task 064)
```csharp
record RouteMatchState(
  Endpoint Endpoint,
  bool IsViableMatch,         // Route could still match
  int SegmentsMatched,        // How many route segments consumed
  int ArgsConsumed,           // How many input args consumed
  List<string> OptionsUsed,   // Options already in input
  List<CompletionCandidate> AvailableNext  // What can come next
);
```

**3. CandidateGenerator** (Task 065)
```csharp
interface ICandidateGenerator
{
  IEnumerable<CompletionCandidate> Generate(
    RouteMatchState state,
    string? partialWord
  );
}
```

### Test Categories to Support

All 20 categories from task 061:
1. Empty input / start of command
2. Partial command matching
3. After complete command (space + tab)
4. Subcommand completion
5. Option completion (-)
6. Option completion (--)
7. Enum parameter completion
8. Mid-word completion
9. Completion after options
10. Multiple tabs (forward cycling)
11. Shift+Tab (reverse cycling)
12. Alt+= (show all)
13. Escape (cancel)
14. Typing after completion
15. Backspace during completion
16. Complex option sequences
17. Invalid completion contexts
18. Case sensitivity
19. Help option availability
20. Edge cases

### Success Criteria

- [x] Architecture document complete
- [x] All interfaces defined
- [x] Data structures specified
- [x] Design reviewed against test categories
- [x] Ready for implementation in tasks 063-066

### Related Tasks

- Task 061: Implement Comprehensive Tab Completion Tests (provides validation)
- Task 063: Implement InputTokenizer
- Task 064: Implement RouteMatchEngine
- Task 065: Implement CandidateGenerator
- Task 066: Integrate and Replace CompletionProvider
