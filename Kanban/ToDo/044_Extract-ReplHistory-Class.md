# Extract ReplHistory Class from ReplSession

## Description

Refactor `ReplSession.cs` by extracting all history management functionality into a new `ReplHistory` class. This improves separation of concerns, testability, and reduces ReplSession from 465 to ~365 lines.

**Current state:** History management logic (5 methods, ~100 lines) is embedded in ReplSession
**Desired state:** History logic isolated in dedicated `ReplHistory` class with clear interface

## Requirements

### Functional Requirements
- [ ] History persistence (load/save) works identically to current implementation
- [ ] History filtering (ShouldIgnore patterns) works identically
- [ ] History deduplication (consecutive identical commands) preserved
- [ ] History size limiting (MaxHistorySize) works correctly
- [ ] All existing REPL functionality remains unchanged (no regression)

### Non-Functional Requirements
- [ ] No breaking changes to public API
- [ ] Internal API changes only affect ReplSession and ReplConsoleReader
- [ ] All existing tests pass without modification
- [ ] New unit tests cover ReplHistory independently

## Checklist

### Design
- [x] Review extraction analysis report (completed in `.agent/workspace/`)
- [ ] Design ReplHistory class interface
- [ ] Identify all dependencies (ReplOptions, ITerminal)
- [ ] Plan integration points with ReplSession and ReplConsoleReader
- [ ] Design read-only vs mutable access patterns

### Implementation
- [ ] Create `Source/TimeWarp.Nuru.Repl/Repl/ReplHistory.cs`
- [ ] Move `AddToHistory` → `ReplHistory.Add`
- [ ] Move `ShouldIgnoreCommand` → `ReplHistory.ShouldIgnore`
- [ ] Move `LoadHistory` → `ReplHistory.Load`
- [ ] Move `SaveHistory` → `ReplHistory.Save`
- [ ] Move `GetHistoryFilePath` → `ReplHistory.GetHistoryFilePath` (private)
- [ ] Add `Clear` method to ReplHistory
- [ ] Add indexer `this[int index]` for history access
- [ ] Add `Count` property
- [ ] Add `IReadOnlyList<string> Items` property for external access
- [ ] Update ReplSession constructor to create ReplHistory instance
- [ ] Update ReplSession to use ReplHistory API instead of direct List access
- [ ] Update ReplConsoleReader to accept ReplHistory (or IReadOnlyList)
- [ ] Remove history-related code from ReplSession
- [ ] Verify all compilation errors resolved

### Testing
- [ ] Run existing test suite - ensure all tests pass
- [ ] Add unit test: `ReplHistory.Add` with valid command
- [ ] Add unit test: `ReplHistory.Add` with duplicate consecutive command (should skip)
- [ ] Add unit test: `ReplHistory.Add` respects MaxHistorySize (trims oldest)
- [ ] Add unit test: `ShouldIgnore` with wildcard patterns (`*password*`, `*secret*`)
- [ ] Add unit test: `ShouldIgnore` with single character wildcard (`?`)
- [ ] Add unit test: `ShouldIgnore` case insensitivity
- [ ] Add unit test: `ShouldIgnore` with empty/null patterns
- [ ] Add unit test: `Load` with missing file (should not error)
- [ ] Add unit test: `Load` with existing history file
- [ ] Add unit test: `Load` respects MaxHistorySize
- [ ] Add unit test: `Save` creates directory if not exists
- [ ] Add unit test: `Save` with IOException (should warn, not crash)
- [ ] Add unit test: `GetHistoryFilePath` uses custom path if provided
- [ ] Add unit test: `GetHistoryFilePath` generates default path (~/.nuru/history/)
- [ ] Integration test: Full REPL session with history persistence

### Documentation
- [ ] Add XML documentation to ReplHistory class
- [ ] Add XML documentation to all public methods
- [ ] Document history filtering patterns in ReplHistory
- [ ] Update code review report with "Fixed" status
- [ ] Update extraction recommendations with "Completed" status

## Notes

### Analysis References
- Code review: `.agent/workspace/replsession-code-review-2025-11-25.md`
- Method categorization: `.agent/workspace/replsession-method-categorization-2025-11-25.md`
- Extraction analysis: `.agent/workspace/replsession-extraction-recommendations-2025-11-25.md`

### Methods to Extract
1. `AddToHistory(string command)` - Lines 350-365 (14 lines)
2. `ShouldIgnoreCommand(string command)` - Lines 367-388 (20 lines) - Already `internal`
3. `LoadHistory()` - Lines 390-417 (26 lines)
4. `SaveHistory()` - Lines 419-441 (21 lines)
5. `GetHistoryFilePath()` - Lines 443-463 (19 lines)

### Dependencies
- **ReplOptions** - For MaxHistorySize, HistoryIgnorePatterns, HistoryFilePath, PersistHistory
- **ITerminal** - For displaying warnings on I/O errors

### Integration Points
- **ReplSession** - Creates ReplHistory, calls Load/Save, uses Add
- **ReplConsoleReader** - Needs read-only access to history items for arrow key navigation

### Proposed ReplHistory Interface
```csharp
internal sealed class ReplHistory
{
  // Properties
  public IReadOnlyList<string> Items { get; }
  public int Count { get; }
  
  // Constructor
  internal ReplHistory(ReplOptions options, ITerminal terminal);
  
  // Public Methods
  public void Add(string command);
  public void Clear();
  public void Load();
  public void Save();
  public bool ShouldIgnore(string command);
  public string this[int index] { get; }
  
  // Private Methods
  private string GetHistoryFilePath();
}
```

### File Structure After Extraction
```
Source/TimeWarp.Nuru.Repl/Repl/
  ├─ ReplSession.cs      (~365 lines, down from 465)
  └─ ReplHistory.cs      (~120 lines) NEW
```

### Benefits
1. **Testability**: History logic can be unit tested without mocking entire REPL session
2. **Maintainability**: Clear separation - history persistence vs session orchestration
3. **Readability**: ReplSession focused on orchestration, not file I/O
4. **Extensibility**: Easy to swap history implementations (database, cloud storage, etc.)
5. **Size**: Reduces ReplSession by 100 lines (22%)

### Risks & Mitigations
- **Risk**: ReplConsoleReader expects `List<string>`, not `ReplHistory`
  - **Mitigation**: Expose `IReadOnlyList<string> Items` property on ReplHistory
  
- **Risk**: Breaking internal API for tests
  - **Mitigation**: Keep `ShouldIgnore` as internal, tests can access it
  
- **Risk**: History list shared by reference vs encapsulated
  - **Mitigation**: Use `IReadOnlyList<string>` for external access, keep internal list private

### Security Considerations
- History file path validation (prevent path traversal) - addressed in code review
- Wildcard pattern injection - already secure (uses Regex.Escape)
- Sensitive data in history - handled by HistoryIgnorePatterns (passwords, tokens, etc.)

### Performance Impact
- **None expected** - Same logic, different location
- History operations are already fast (List operations, regex matching)
- File I/O only on Load/Save (startup/shutdown)

## Implementation Notes

[Notes will be added during implementation]
