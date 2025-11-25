# Extract ReplCommands Class from ReplSession

## Description

Refactor `ReplSession.cs` by extracting all user command implementations into a new `ReplCommands` class. This completes the separation of concerns started in task 044, improving testability and reducing ReplSession to its core orchestration responsibility.

**Current state:** User command methods (5 methods, ~90 lines including largest method ShowReplHelp at 52 lines) are embedded in ReplSession
**Desired state:** Command logic isolated in dedicated `ReplCommands` class with clear interface

## Parent

Related to task 044_Extract-ReplHistory-Class (completed)

## Requirements

### Functional Requirements
- [ ] All REPL commands work identically to current implementation (exit, quit, q, help, ?, history, clear, cls, clear-history)
- [ ] ShowReplHelp displays same information (REPL commands + available app commands)
- [ ] Exit command properly terminates REPL session
- [ ] ClearHistory clears the ReplHistory instance
- [ ] ClearScreen clears terminal display
- [ ] All existing REPL functionality remains unchanged (no regression)

### Non-Functional Requirements
- [ ] No breaking changes to public API
- [ ] Internal API changes only affect ReplSession and NuruAppExtensions
- [ ] All existing tests pass without modification
- [ ] New unit tests cover ReplCommands independently
- [ ] Route registration updated to use ReplCommands instance

## Checklist

### Design
- [x] Review extraction analysis report (completed in `.agent/workspace/`)
- [ ] Design ReplCommands class interface
- [ ] Identify all dependencies (ReplSession, NuruApp, ReplOptions, ITerminal, ITypeConverterRegistry, ReplHistory)
- [ ] Plan integration with ReplSession (expose Stop() method or make ReplCommands friend)
- [ ] Design route registration pattern (access Commands from CurrentSession)
- [ ] Decide on CompletionProvider creation strategy (inject vs create in ShowReplHelp)

### Implementation
- [ ] Create `Source/TimeWarp.Nuru.Repl/Repl/ReplCommands.cs`
- [ ] Move `ShowReplHelp()` → `ReplCommands.ShowReplHelp()`
- [ ] Move `ShowHistory()` → `ReplCommands.ShowHistory()`
- [ ] Move `Exit()` → `ReplCommands.Exit()`
- [ ] Move `ClearHistory()` → `ReplCommands.ClearHistory()`
- [ ] Move `ClearScreen()` → `ReplCommands.ClearScreen()`
- [ ] Add `ReplCommands` field to ReplSession
- [ ] Create ReplCommands instance in ReplSession constructor
- [ ] Expose `Commands` property on ReplSession OR make methods call through to Commands
- [ ] Add `Stop()` method to ReplSession (internal) for Exit command to call
- [ ] Update `NuruAppExtensions.AddReplRoutes()` to access commands via CurrentSession.Commands
- [ ] Remove command methods from ReplSession
- [ ] Verify all compilation errors resolved

### Testing
- [ ] Run existing test suite - ensure all tests pass
- [ ] Add unit test: `ReplCommands.Exit` calls ReplSession.Stop()
- [ ] Add unit test: `ReplCommands.ClearHistory` calls ReplHistory.Clear()
- [ ] Add unit test: `ReplCommands.ClearScreen` calls Terminal.Clear()
- [ ] Add unit test: `ReplCommands.ShowHistory` displays history items with numbering
- [ ] Add unit test: `ReplCommands.ShowHistory` handles empty history
- [ ] Add unit test: `ReplCommands.ShowReplHelp` displays REPL commands section
- [ ] Add unit test: `ReplCommands.ShowReplHelp` displays keyboard shortcuts section
- [ ] Add unit test: `ReplCommands.ShowReplHelp` displays available app commands
- [ ] Add unit test: `ReplCommands.ShowReplHelp` handles completion errors gracefully
- [ ] Add unit test: `ReplCommands.ShowReplHelp` respects EnableColors option
- [ ] Integration test: Call all REPL commands through routes
- [ ] Integration test: Full REPL session using all commands

### Documentation
- [ ] Add XML documentation to ReplCommands class
- [ ] Add XML documentation to all public methods
- [ ] Update code review report with "Completed" status for extraction
- [ ] Update extraction recommendations with "Phase 2 Completed" status

## Notes

### Analysis References
- Code review: `.agent/workspace/replsession-code-review-2025-11-25.md`
- Method categorization: `.agent/workspace/replsession-method-categorization-2025-11-25.md`
- Extraction analysis: `.agent/workspace/replsession-extraction-recommendations-2025-11-25.md`

### Methods to Extract
1. `ShowReplHelp()` - Lines 254-307 (52 lines) - **Largest method in ReplSession**
2. `ShowHistory()` - Lines 311-324 (12 lines)
3. `Exit()` - Lines 329-332 (2 lines)
4. `ClearHistory()` - Lines 337-340 (2 lines)
5. `ClearScreen()` - Lines 345-348 (2 lines)

### Dependencies
- **ReplSession** - Need to call Stop() to set Running = false
- **NuruApp** - For accessing Endpoints in ShowReplHelp
- **ReplOptions** - For EnableColors and other display options
- **ITerminal** - For all output operations
- **ITypeConverterRegistry** - For creating CompletionProvider in ShowReplHelp
- **ReplHistory** - For ShowHistory and ClearHistory commands

### Integration Points
- **ReplSession** - Creates ReplCommands, exposes via Commands property
- **NuruAppExtensions.AddReplRoutes()** - Accesses commands via `ReplSession.CurrentSession?.Commands`

### Proposed ReplCommands Interface
```csharp
namespace TimeWarp.Nuru.Repl;

/// <summary>
/// Implements built-in REPL commands.
/// </summary>
internal sealed class ReplCommands
{
  private readonly ReplSession Session;
  private readonly NuruApp NuruApp;
  private readonly ReplOptions Options;
  private readonly ITerminal Terminal;
  private readonly ITypeConverterRegistry TypeConverterRegistry;
  private readonly ReplHistory History;

  internal ReplCommands
  (
    ReplSession session,
    NuruApp nuruApp,
    ReplOptions options,
    ITerminal terminal,
    ITypeConverterRegistry typeConverterRegistry,
    ReplHistory history
  )
  {
    Session = session ?? throw new ArgumentNullException(nameof(session));
    NuruApp = nuruApp ?? throw new ArgumentNullException(nameof(nuruApp));
    Options = options ?? throw new ArgumentNullException(nameof(options));
    Terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    TypeConverterRegistry = typeConverterRegistry ?? throw new ArgumentNullException(nameof(typeConverterRegistry));
    History = history ?? throw new ArgumentNullException(nameof(history));
  }

  /// <summary>
  /// Displays REPL help information including commands and available routes.
  /// </summary>
  public void ShowReplHelp() { /* move from ReplSession */ }

  /// <summary>
  /// Displays command history with line numbers.
  /// </summary>
  public void ShowHistory() { /* move from ReplSession */ }

  /// <summary>
  /// Exits the REPL session.
  /// </summary>
  public void Exit() 
  { 
    Session.Stop(); 
  }

  /// <summary>
  /// Clears the command history.
  /// </summary>
  public void ClearHistory() 
  { 
    History.Clear(); 
  }

  /// <summary>
  /// Clears the terminal screen.
  /// </summary>
  public void ClearScreen() 
  { 
    Terminal.Clear(); 
  }
}
```

### ReplSession Changes
```csharp
internal sealed class ReplSession
{
  // Add new field
  private readonly ReplCommands Commands;

  internal ReplSession(...)
  {
    // ... existing initialization ...
    
    // Create commands instance
    Commands = new ReplCommands(this, NuruApp, ReplOptions, Terminal, TypeConverterRegistry, History);
  }

  // Add internal property for route access
  internal ReplCommands GetCommands() => Commands;

  // Add Stop method for Exit command
  internal void Stop()
  {
    Running = false;
  }
}
```

### Route Registration Update
```csharp
// In NuruAppExtensions.cs
public static NuruAppBuilder AddReplRoutes(this NuruAppBuilder builder)
{
  ArgumentNullException.ThrowIfNull(builder);

  // Register REPL commands as routes via Commands instance
  builder
    .AddRoute("exit", () => ReplSession.CurrentSession?.GetCommands().Exit(), "Exit the REPL")
    .AddRoute("quit", () => ReplSession.CurrentSession?.GetCommands().Exit(), "Exit the REPL")
    .AddRoute("q", () => ReplSession.CurrentSession?.GetCommands().Exit(), "Exit the REPL")
    .AddRoute("history", () => ReplSession.CurrentSession?.GetCommands().ShowHistory(), "Show command history")
    .AddRoute("clear", () => ReplSession.CurrentSession?.GetCommands().ClearScreen(), "Clear the screen")
    .AddRoute("cls", () => ReplSession.CurrentSession?.GetCommands().ClearScreen(), "Clear the screen")
    .AddRoute("clear-history", () => ReplSession.CurrentSession?.GetCommands().ClearHistory(), "Clear command history")
    .AddAutoHelp();

  return builder;
}
```

### File Structure After Extraction
```
Source/TimeWarp.Nuru.Repl/Repl/
  ├─ ReplSession.cs      (~220 lines, down from ~365 after task 044)
  ├─ ReplHistory.cs      (~120 lines, from task 044)
  └─ ReplCommands.cs     (~100 lines) NEW
```

### Benefits
1. **Testability**: Command logic can be unit tested without full REPL session
2. **Maintainability**: Clear separation - commands vs orchestration
3. **Readability**: ReplSession focused on lifecycle, not command implementation
4. **Extensibility**: Easy to add new REPL commands (just add methods to ReplCommands)
5. **Size**: Reduces ReplSession by additional 90 lines (total reduction from 465 → ~220 = 53%)
6. **Cohesion**: Commands have single responsibility - implementing user-facing REPL commands

### Risks & Mitigations

**Risk: Exit command needs to modify ReplSession.Running flag**
- **Mitigation 1:** Add internal `Stop()` method to ReplSession that ReplCommands can call
- **Mitigation 2:** Alternative: Pass Action callback to ReplCommands constructor
- **Recommendation:** Use Mitigation 1 (cleaner, more explicit)

**Risk: Route registration becomes more verbose**
- **Impact:** Routes need to access `CurrentSession?.GetCommands().MethodName()` instead of `CurrentSession?.MethodName()`
- **Mitigation:** Accept the verbosity - it's more explicit and shows the separation
- **Alternative:** Add convenience properties to ReplSession (not recommended - defeats purpose)

**Risk: ShowReplHelp creates CompletionProvider instance**
- **Issue:** Violates dependency injection principle
- **Mitigation:** Accept for now (same as current implementation), refactor later if needed
- **Future:** Consider injecting CompletionProvider or using factory

**Risk: Circular dependency (ReplCommands needs ReplSession, ReplSession creates ReplCommands)**
- **Assessment:** Not a problem - this is composition, not circular reference
- **Pattern:** Commands aggregate contains reference to container for callbacks

### ShowReplHelp Complexity

The `ShowReplHelp` method is the largest method (52 lines) and has mixed responsibilities:
1. Display static REPL help text
2. Create CompletionProvider
3. Get available commands via completion
4. Display dynamic command list

**Current Approach for Task 045:**
- Move entire method as-is to ReplCommands
- Keep CompletionProvider creation inside ShowReplHelp

**Future Enhancement (separate task):**
- Inject CompletionProvider via constructor
- Split ShowReplHelp into smaller methods
- Consider creating dedicated help formatter

### Security Considerations
- No security implications - commands already existed, just relocated
- CompletionProvider creation same as current implementation
- Terminal output already sanitized

### Performance Impact
- **Negligible** - One additional object allocation (ReplCommands instance)
- Commands accessed via single method call (GetCommands())
- No performance-critical paths affected
- Slightly more indirection in route calls (acceptable)

### Breaking Changes
- **None** - All changes internal
- Public API surface unchanged
- Route behavior identical
- REPL user experience unchanged

### Testing Strategy

**Unit Tests (ReplCommands in isolation):**
- Mock ReplSession, Terminal, History
- Test each command method independently
- Verify correct delegation (Exit→Stop, ClearHistory→History.Clear, etc.)

**Integration Tests:**
- Full REPL session
- Execute each command via routes
- Verify side effects (history cleared, screen cleared, session stopped)

**Regression Tests:**
- Run existing REPL tests
- Ensure no behavior changes

## Implementation Notes

### Step-by-Step Implementation Plan

1. **Create ReplCommands.cs** with constructor and dependencies
2. **Copy methods** from ReplSession to ReplCommands (keep originals temporarily)
3. **Add ReplCommands field** to ReplSession
4. **Create ReplCommands instance** in ReplSession constructor
5. **Add GetCommands() method** to ReplSession
6. **Add Stop() method** to ReplSession
7. **Update NuruAppExtensions.AddReplRoutes()** to use GetCommands()
8. **Test** that routes still work
9. **Remove old methods** from ReplSession
10. **Verify compilation and run tests**
11. **Add new unit tests** for ReplCommands
12. **Clean up** and verify no regressions

[Notes will be added during implementation]
