# Integrate CompletionEngine and Replace CompletionProvider

## Description

Integrate the new `InputTokenizer`, `RouteMatchEngine`, and `CandidateGenerator` components into a unified `CompletionEngine`. Replace the existing `CompletionProvider.GetCompletions()` implementation with a call to the new engine. Verify all existing tests pass, then remove dead code.

**Goal**: Single entry point that uses the new unified pipeline, eliminating all ad hoc code paths.

## Parent

062_Redesign-CompletionEngine-Architecture

## Requirements

- Create `CompletionEngine` that orchestrates the pipeline
- Replace `CompletionProvider.GetCompletions()` implementation
- Maintain backward compatibility (same public API)
- All existing tests must pass
- All new tests from task 061 should pass (bug fixes!)
- Remove old dead code after verification
- No performance regression

## Checklist

### Integration
- [ ] Create `CompletionEngine` orchestrator class
- [ ] Wire up InputTokenizer → RouteMatchEngine → CandidateGenerator
- [ ] Handle CompletionContext → ParsedInput conversion
- [ ] Inject dependencies (ITypeConverterRegistry, ILoggerFactory)

### Replace CompletionProvider
- [ ] Modify `GetCompletions()` to use CompletionEngine
- [ ] Keep public API unchanged
- [ ] Ensure backward compatibility

### Testing
- [ ] Run all existing REPL tests
- [ ] Run all TabCompletion tests from task 061
- [ ] Verify bug categories are fixed:
  - [ ] Options shown after command/parameter
  - [ ] Partial option completion works
  - [ ] Case sensitivity consistent
  - [ ] Subcommand context preserved
  - [ ] --help available everywhere
- [ ] Performance benchmarking (no regression)

### Cleanup
- [ ] Mark old methods as obsolete or remove
- [ ] Remove unused private methods
- [ ] Update XML documentation
- [ ] Clean up any TODO comments

### Documentation
- [ ] Update architecture docs if needed
- [ ] Document the new pipeline flow
- [ ] Update CLAUDE.md if needed

## Notes

### CompletionEngine Implementation

```csharp
namespace TimeWarp.Nuru.Completion;

/// <summary>
/// Unified completion engine using pipeline architecture.
/// </summary>
public class CompletionEngine
{
  private readonly RouteMatchEngine _matchEngine;
  private readonly CandidateGenerator _candidateGenerator;
  private readonly ILogger<CompletionEngine>? _logger;
  
  public CompletionEngine(
    ITypeConverterRegistry typeConverterRegistry,
    ILoggerFactory? loggerFactory = null)
  {
    _matchEngine = new RouteMatchEngine(typeConverterRegistry);
    _candidateGenerator = new CandidateGenerator();
    _logger = loggerFactory?.CreateLogger<CompletionEngine>();
  }
  
  public ReadOnlyCollection<CompletionCandidate> GetCompletions(
    string input,
    EndpointCollection endpoints)
  {
    // 1. Parse input
    var parsedInput = InputTokenizer.Parse(input);
    _logger?.LogDebug("Parsed: {Words} partial={Partial}", 
      parsedInput.CompletedWords, parsedInput.PartialWord);
    
    // 2. Compute match states
    var matchStates = _matchEngine.ComputeMatchStates(parsedInput, endpoints);
    _logger?.LogDebug("Viable routes: {Count}", matchStates.Count(s => s.IsViableMatch));
    
    // 3. Generate candidates
    var candidates = _candidateGenerator.Generate(matchStates, parsedInput.PartialWord);
    _logger?.LogDebug("Generated {Count} candidates", candidates.Count);
    
    return candidates;
  }
}
```

### CompletionProvider Integration

```csharp
public class CompletionProvider
{
  private readonly CompletionEngine _engine;
  
  public CompletionProvider(ITypeConverterRegistry registry, ILoggerFactory? loggerFactory)
  {
    _engine = new CompletionEngine(registry, loggerFactory);
  }
  
  public ReadOnlyCollection<CompletionCandidate> GetCompletions(
    CompletionContext context,
    EndpointCollection endpoints)
  {
    // Convert context to raw input string
    string input = string.Join(" ", context.Args);
    if (context.HasTrailingSpace)
      input += " ";
    
    return _engine.GetCompletions(input, endpoints);
  }
}
```

### Bug Fixes Expected

After integration, these should all pass:

| Bug Category | Before | After |
|--------------|--------|-------|
| Options after command | FAIL | PASS |
| Partial option completion | FAIL | PASS |
| Case sensitivity | Inconsistent | Consistent |
| Subcommand context | Lost | Preserved |
| --help availability | Inconsistent | Always |

### Code to Remove

After verification, remove from CompletionProvider:
- `GetCommandCompletions()` - replaced by RouteMatchEngine
- `GetCompletionsAfterCommand()` - replaced by RouteMatchEngine
- `GetCompletionsForRoute()` - replaced by RouteMatchEngine
- `GetOptionCompletions()` - replaced by RouteMatchEngine
- `GetParameterCompletions()` - moved to CandidateGenerator
- `IsExactCommandMatch()` - no longer needed
- `GetTypeForConstraint()` - moved to RouteMatchEngine

### Performance Considerations

The new implementation should be:
- Same or better time complexity
- Fewer allocations (reuse collections)
- Early bailout for non-viable routes
- No redundant string operations

Benchmark before/after:
```csharp
// Run existing benchmarks
// Compare: Candidates/second, Memory allocated
```

### Rollback Plan

If issues discovered:
1. Keep old code in `CompletionProvider_Legacy.cs`
2. Add feature flag to switch between old/new
3. Gradually migrate after stabilization

### Success Criteria

- [ ] All tests pass (existing + new)
- [ ] All 5 bug categories fixed
- [ ] No performance regression
- [ ] Code is cleaner (fewer lines, single path)
- [ ] Ready for production use

### Estimated Impact

| Metric | Before | After |
|--------|--------|-------|
| CompletionProvider lines | 568 | ~100 |
| Code paths | 3 | 1 |
| Test pass rate | ~50% | 100% |
| Bug categories | 5 | 0 |
