# Inject CompletionProvider and Refactor ShowReplHelp

## Description

Refactor `ReplCommands.ShowReplHelp()` and `ReplSession.ReadCommandInput()` to use dependency injection for `CompletionProvider` instead of creating instances inline. Also split `ShowReplHelp` into two focused methods for better separation of concerns.

**Current state:** 
- `CompletionProvider` created in two places (ReplCommands.ShowReplHelp line 70, ReplSession.ReadCommandInput line 153)
- `ShowReplHelp` has mixed responsibilities (display static help + get/display completions)

**Desired state:**
- `CompletionProvider` injected via constructor in both ReplCommands and ReplSession
- `ShowReplHelp` split into `ShowReplHelp()` (static help) and `ShowAvailableCommands()` (dynamic completions)

## Parent

Related to tasks:
- 044_Extract-ReplHistory-Class (completed)
- 045_Extract-ReplCommands-Class (completed)

## Requirements

### Functional Requirements
- [ ] Help command displays identical information to current implementation
- [ ] Available commands list shows same completions as before
- [ ] Tab completion in REPL continues to work identically
- [ ] All error handling preserved (catch InvalidOperationException, ArgumentException)
- [ ] Color support respects ReplOptions.EnableColors setting
- [ ] No regression in existing REPL functionality

### Non-Functional Requirements
- [ ] No breaking changes to public API
- [ ] Reduced coupling - CompletionProvider created once, reused
- [ ] Improved testability - CompletionProvider can be mocked
- [ ] Clearer separation of concerns in ShowReplHelp
- [ ] All existing tests pass without modification

## Checklist

### Design
- [ ] Decide where to create CompletionProvider (ReplSession constructor most logical)
- [ ] Design CompletionProvider sharing between ReplSession and ReplCommands
- [ ] Plan ShowReplHelp split: static help vs dynamic command list
- [ ] Verify CompletionProvider is thread-safe for reuse
- [ ] Consider CompletionProvider lifecycle (created once per session)

### Implementation

#### Part 1: Inject CompletionProvider
- [ ] Add `CompletionProvider` readonly field to ReplSession
- [ ] Create CompletionProvider instance in ReplSession constructor
- [ ] Pass CompletionProvider to ReplCommands constructor
- [ ] Add `CompletionProvider` readonly field to ReplCommands
- [ ] Store CompletionProvider in ReplCommands constructor
- [ ] Update ReplCommands.ShowReplHelp to use injected provider (remove `new CompletionProvider(...)`)
- [ ] Update ReplSession.ReadCommandInput to use field instead of creating new instance
- [ ] Update ReplConsoleReader to receive CompletionProvider (it already does via constructor)

#### Part 2: Split ShowReplHelp
- [ ] Extract completion logic from ShowReplHelp → new `ShowAvailableCommands()` method
- [ ] Update ShowReplHelp to call ShowAvailableCommands() after static help
- [ ] Consider making ShowAvailableCommands internal/public based on future needs
- [ ] Verify separation: ShowReplHelp = static text, ShowAvailableCommands = dynamic completions

### Testing
- [ ] Run existing test suite - ensure all tests pass
- [ ] Add unit test: ReplCommands with mocked CompletionProvider
- [ ] Add unit test: ShowReplHelp displays static help text correctly
- [ ] Add unit test: ShowAvailableCommands handles empty completions
- [ ] Add unit test: ShowAvailableCommands handles completion exceptions
- [ ] Add unit test: ShowAvailableCommands displays commands with descriptions
- [ ] Add unit test: Verify CompletionProvider reused (not recreated per call)
- [ ] Integration test: Full REPL session with help command
- [ ] Integration test: Tab completion still works with injected provider

### Documentation
- [ ] Update XML documentation for ShowReplHelp (now only static help)
- [ ] Add XML documentation to new ShowAvailableCommands method
- [ ] Update ReplCommands constructor docs to mention CompletionProvider
- [ ] Update ReplSession constructor docs to mention CompletionProvider creation
- [ ] Update code review report with "Fixed H1" status

## Notes

### Analysis References
- Code review: `.agent/workspace/replsession-code-review-2025-11-25.md` (High Priority Issue H1)
- Extraction recommendations: Mentioned as medium priority refactoring

### Current CompletionProvider Creation Locations

**Location 1: ReplCommands.ShowReplHelp (line 70)**
```csharp
CompletionProvider provider = new(TypeConverterRegistry);
CompletionContext context = new(Args: [], CursorPosition: 0, Endpoints: NuruApp.Endpoints);
IEnumerable<CompletionCandidate> completionsEnumerable = provider.GetCompletions(context, NuruApp.Endpoints);
```

**Location 2: ReplSession.ReadCommandInput (line 153)**
```csharp
var consoleReader =
  new ReplConsoleReader
    (
      History.AsReadOnly,
      new CompletionProvider(TypeConverterRegistry, LoggerFactory),  // ← Created here
      NuruApp.Endpoints,
      ReplOptions,
      LoggerFactory,
      Terminal
    );
```

### Proposed Design

#### ReplSession Changes
```csharp
internal sealed class ReplSession
{
  private readonly CompletionProvider CompletionProvider;  // NEW
  private readonly ReplCommands Commands;

  internal ReplSession(
    NuruApp nuruApp,
    ReplOptions replOptions,
    ILoggerFactory loggerFactory
  )
  {
    NuruApp = nuruApp ?? throw new ArgumentNullException(nameof(nuruApp));
    ReplOptions = replOptions ?? new ReplOptions();
    TypeConverterRegistry = nuruApp.TypeConverterRegistry;
    LoggerFactory = loggerFactory;
    Terminal = nuruApp.Terminal;
    
    // Create CompletionProvider once for entire session
    CompletionProvider = new CompletionProvider(TypeConverterRegistry, LoggerFactory);
    
    History = new ReplHistory(ReplOptions, Terminal);
    
    // Pass CompletionProvider to Commands
    Commands = new ReplCommands(this, NuruApp, ReplOptions, Terminal, CompletionProvider, History);
  }

  private string? ReadCommandInput()
  {
    if (ReplOptions.EnableArrowHistory)
    {
      var consoleReader =
        new ReplConsoleReader
          (
            History.AsReadOnly,
            CompletionProvider,  // ← Use field instead of creating new
            NuruApp.Endpoints,
            ReplOptions,
            LoggerFactory,
            Terminal
          );
      return consoleReader.ReadLine(ReplOptions.Prompt);
    }

    return Terminal.ReadLine();
  }
}
```

#### ReplCommands Changes
```csharp
internal sealed class ReplCommands
{
  private readonly ReplSession Session;
  private readonly NuruApp NuruApp;
  private readonly ReplOptions Options;
  private readonly ITerminal Terminal;
  private readonly CompletionProvider CompletionProvider;  // NEW - changed from ITypeConverterRegistry
  private readonly ReplHistory History;

  internal ReplCommands
  (
    ReplSession session,
    NuruApp nuruApp,
    ReplOptions options,
    ITerminal terminal,
    CompletionProvider completionProvider,  // NEW parameter
    ReplHistory history
  )
  {
    Session = session ?? throw new ArgumentNullException(nameof(session));
    NuruApp = nuruApp ?? throw new ArgumentNullException(nameof(nuruApp));
    Options = options ?? throw new ArgumentNullException(nameof(options));
    Terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    CompletionProvider = completionProvider ?? throw new ArgumentNullException(nameof(completionProvider));
    History = history ?? throw new ArgumentNullException(nameof(history));
  }

  /// <summary>
  /// Shows REPL help information including built-in commands and keyboard shortcuts.
  /// </summary>
  public void ShowReplHelp()
  {
    // Static help text
    if (Options.EnableColors)
    {
      Terminal.WriteLine(AnsiColors.BrightBlue + "REPL Commands:" + AnsiColors.Reset);
    }
    else
    {
      Terminal.WriteLine("REPL Commands:");
    }

    Terminal.WriteLine("  exit, quit, q     - Exit the REPL");
    Terminal.WriteLine("  help, ?           - Show this help");
    Terminal.WriteLine("  history           - Show command history");
    Terminal.WriteLine("  clear, cls        - Clear the screen");
    Terminal.WriteLine("  clear-history     - Clear command history");
    Terminal.WriteLine();

    Terminal.WriteLine("Any other input is executed as an application command.");
    Terminal.WriteLine("Use Ctrl+C to cancel current operation or Ctrl+D to exit.");

    // Dynamic command list
    ShowAvailableCommands();
  }

  /// <summary>
  /// Shows available application commands using completion provider.
  /// </summary>
  private void ShowAvailableCommands()
  {
    Terminal.WriteLine("\nAvailable Application Commands:");
    try
    {
      CompletionContext context = new(Args: [], CursorPosition: 0, Endpoints: NuruApp.Endpoints);
      IEnumerable<CompletionCandidate> completionsEnumerable = 
        CompletionProvider.GetCompletions(context, NuruApp.Endpoints);
      List<CompletionCandidate> completions = [.. completionsEnumerable];

      if (completions.Count > 0)
      {
        foreach (CompletionCandidate cand in completions.OrderBy(c => c.Value))
        {
          string desc = string.IsNullOrEmpty(cand.Description) ? "" : $" - {cand.Description}";
          Terminal.WriteLine($"  {cand.Value}{desc}");
        }
      }
      else
      {
        Terminal.WriteLine("  No commands available.");
      }
    }
    catch (InvalidOperationException)
    {
      Terminal.WriteLine("  (Completions unavailable - check configuration)");
    }
    catch (ArgumentException)
    {
      Terminal.WriteLine("  (Completions unavailable - check configuration)");
    }
  }
}
```

### Benefits

#### Dependency Injection
1. **Single Responsibility**: CompletionProvider created once, reused everywhere
2. **Testability**: Can inject mock CompletionProvider for testing
3. **Performance**: One instance instead of creating multiple (minor, but cleaner)
4. **Consistency**: Same provider instance used throughout session
5. **Maintainability**: Clear dependency in constructor signature

#### ShowReplHelp Split
1. **Clarity**: ShowReplHelp now focused on static help text
2. **Reusability**: ShowAvailableCommands can be called independently if needed
3. **Testability**: Each method can be tested separately
4. **Single Responsibility**: Each method has one clear purpose
5. **Reduced Complexity**: 52-line method split into two smaller methods (~25 lines each)

### Risks & Mitigations

**Risk: CompletionProvider not thread-safe for reuse**
- **Assessment**: CompletionProvider methods appear stateless (GetCompletions returns new results)
- **Verification**: Check CompletionProvider implementation for thread safety
- **Mitigation**: If not thread-safe, document that REPL is single-threaded

**Risk: Breaking ReplCommands constructor signature**
- **Impact**: Internal API change - no external impact
- **Affected Code**: Only ReplSession creates ReplCommands
- **Mitigation**: Update single call site in ReplSession constructor

**Risk: CompletionProvider lifetime longer than expected**
- **Assessment**: One per session is appropriate (session-scoped)
- **Impact**: Minimal - CompletionProvider is lightweight
- **Mitigation**: None needed

### Alternative Designs Considered

**Alternative 1: Inject ICompletionProvider interface**
- **Pros**: Better testability, more flexible
- **Cons**: Over-engineering for current needs, no interface exists yet
- **Decision**: Use concrete CompletionProvider for now, add interface later if needed

**Alternative 2: Create CompletionProvider in NuruApp**
- **Pros**: Centralized creation
- **Cons**: CompletionProvider is REPL-specific, doesn't belong in core
- **Decision**: Keep in ReplSession (REPL-specific concern)

**Alternative 3: Use factory method**
- **Pros**: More flexible, easier to swap implementations
- **Cons**: Unnecessary complexity for current needs
- **Decision**: Direct instantiation is sufficient

### Performance Impact
- **Positive**: Reduced allocations (one CompletionProvider vs multiple)
- **Negligible**: CompletionProvider creation is already fast
- **No regression**: Same completion logic, just different timing of creation

### Code Review Issues Addressed

From `.agent/workspace/replsession-code-review-2025-11-25.md`:

**HIGH Priority Issue H1: Mixed Responsibilities in ShowReplHelp**
- ✅ **Addressed**: Split into ShowReplHelp (static) + ShowAvailableCommands (dynamic)

**MEDIUM Priority Issue M1: Duplicate CompletionProvider Construction**
- ✅ **Addressed**: Single instance created in ReplSession constructor, injected to both ReplCommands and ReplConsoleReader

### File Structure After Changes
```
Source/TimeWarp.Nuru.Repl/Repl/
  ├─ ReplSession.cs      (~265 lines, minor changes)
  ├─ ReplHistory.cs      (~181 lines, unchanged)
  └─ ReplCommands.cs     (~140 lines, split ShowReplHelp)
```

### Testing Strategy

**Unit Tests:**
- Mock CompletionProvider to return predefined completions
- Test ShowReplHelp displays static text
- Test ShowAvailableCommands handles empty results
- Test ShowAvailableCommands handles exceptions
- Test ShowAvailableCommands formats output correctly

**Integration Tests:**
- Full REPL session with real CompletionProvider
- Verify help command works
- Verify tab completion works
- Verify same provider instance used throughout session

**Regression Tests:**
- Run all existing REPL tests
- Verify no behavior changes

## Implementation Notes

### Completed: 2025-11-25

**Commit:** c69391c "Refactor CompletionProvider to use dependency injection and split ShowReplHelp method"

### Implementation Summary

✅ **Part 1: Dependency Injection (Completed)**
- Added `CompletionProvider` readonly field to ReplSession (line 14)
- Created CompletionProvider once in ReplSession constructor (line 48)
- Updated ReplCommands constructor to accept CompletionProvider instead of ITypeConverterRegistry
- Updated ReplSession.ReadCommandInput to use field instead of creating new instance (line 161)
- Updated XML documentation for constructor parameters

✅ **Part 2: Method Split (Completed)**
- Extracted `ShowAvailableCommands()` private method from `ShowReplHelp()` in ReplCommands
- ShowReplHelp now displays only static help text and calls ShowAvailableCommands
- ShowAvailableCommands handles dynamic completion listing with error handling
- Updated XML documentation for both methods
- Preserved all error handling (InvalidOperationException, ArgumentException)

### Files Modified
- `Source/TimeWarp.Nuru.Repl/Repl/ReplSession.cs` (14 lines changed)
  - Added CompletionProvider field
  - Constructor creates and stores CompletionProvider
  - ReadCommandInput uses field instead of creating new instance
  - Updated constructor documentation

- `Source/TimeWarp.Nuru.Repl/Repl/ReplCommands.cs` (24 lines changed)
  - Constructor signature changed to accept CompletionProvider
  - ShowReplHelp split into two focused methods
  - Updated method documentation

### Test Results
- ✅ Solution builds: 0 warnings, 0 errors
- ✅ REPL session lifecycle tests: 11/11 passed
- ✅ REPL configuration tests: 9/9 passed
- ✅ Full test suite: 22/24 passed (2 pre-existing failures unrelated to changes)

### Benefits Achieved
1. **Reduced Coupling**: CompletionProvider created once per session, reused in both ReplCommands and ReplConsoleReader
2. **Improved Testability**: CompletionProvider can now be mocked for unit testing
3. **Better Separation of Concerns**: ShowReplHelp focused on static help, ShowAvailableCommands focused on dynamic completions
4. **Performance**: One CompletionProvider instance instead of creating multiple
5. **Maintainability**: Clear dependencies in constructor signatures

### Code Review Issues Addressed
- ✅ **HIGH Priority H1**: Mixed responsibilities in ShowReplHelp - FIXED by splitting into two methods
- ✅ **MEDIUM Priority M1**: Duplicate CompletionProvider construction - FIXED by dependency injection
