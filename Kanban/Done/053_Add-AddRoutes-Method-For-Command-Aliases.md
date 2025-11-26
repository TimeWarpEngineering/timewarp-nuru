# Add AddRoutes() Method For Command Aliases

## Description

Add `AddRoutes()` method to `NuruAppBuilder` to support registering multiple route patterns (aliases) that invoke the same handler. This eliminates the need for repetitive route registrations when commands have multiple names (e.g., "exit", "quit", "q").

**Current approach (repetitive):**
```csharp
.AddRoute("exit", ReplSession.ExitAsync, "Exit the REPL")
.AddRoute("quit", ReplSession.ExitAsync, "Exit the REPL")
.AddRoute("q", ReplSession.ExitAsync, "Exit the REPL")
```

**Proposed approach (with AddRoutes):**
```csharp
.AddRoutes(["exit", "quit", "q"], ReplSession.ExitAsync, "Exit the REPL")
```

## Requirements

- Add `AddRoutes(string[] patterns, Delegate handler, string? description)` method to `NuruAppBuilder`
- Add generic overloads for mediator pattern support:
  - `AddRoutes<TCommand>(string[] patterns, string? description)`
  - `AddRoutes<TCommand, TResponse>(string[] patterns, string? description)`
- Each pattern in array creates a separate `Endpoint` for route matching
- First pattern in array is considered the primary command for help display
- Validate that patterns array is not null or empty
- Maintain backward compatibility with existing `AddRoute()` method
- No lexer/parser changes required (builder-level feature only)

## Checklist

### Implementation
- [ ] Add `AddRoutes()` method for delegate-based routing
- [ ] Add `AddRoutes<TCommand>()` for mediator pattern
- [ ] Add `AddRoutes<TCommand, TResponse>()` for mediator with response
- [ ] Add XML documentation with examples
- [ ] Add validation for null/empty patterns array
- [ ] Update `NuruAppExtensions.AddReplRoutes()` to use new method
- [ ] Verify all patterns create separate endpoints

### Testing
- [ ] Add unit tests for AddRoutes with multiple patterns
- [ ] Test validation (null array, empty array)
- [ ] Test route matching works for all aliases
- [ ] Test that all aliases invoke the same handler
- [ ] Add tests for generic mediator overloads
- [ ] Verify help generation shows aliases appropriately

### Documentation
- [ ] Add XML documentation to methods
- [ ] Consider adding design document similar to `optional-flag-alias-syntax.md`
- [ ] Update REPL examples to use AddRoutes()

## Notes

### Design Decisions

**Why `AddRoutes()` (plural) vs other approaches?**
- Follows .NET conventions: `Add()` / `AddRange()`, `Append()` / `AppendRange()`
- Minimal API surface - just singular vs plural
- Intuitive and discoverable via IntelliSense
- No new concepts like "Alias" to learn

**Implementation Strategy:**
- Each pattern creates a separate `Endpoint` via `AddRouteInternal()`
- No changes to Lexer/Parser needed
- Pure builder-level convenience method

**Help Display Options:**
1. Show all: `exit, quit, q              Exit the REPL`
2. Count: `exit (+2 aliases)            Exit the REPL`
3. Parens: `exit (quit, q)              Exit the REPL` ← Recommended

### Files to Modify

- `Source/TimeWarp.Nuru/NuruAppBuilder.cs` - Add AddRoutes methods
- `Source/TimeWarp.Nuru.Repl/NuruAppExtensions.cs` - Update to use AddRoutes
- Tests for validation and route matching

### Future Considerations

- Analyzer rule (NURU013): Suggest using `AddRoute()` for single pattern to `AddRoutes()`
- Analyzer rule: Detect duplicate route registrations and suggest using `AddRoutes()`

## Implementation Notes

**Method Signature:**
```csharp
public NuruAppBuilder AddRoutes(string[] patterns, Delegate handler, string? description = null)
{
    ArgumentNullException.ThrowIfNull(patterns);
    ArgumentNullException.ThrowIfNull(handler);
    
    if (patterns.Length == 0)
        throw new ArgumentException("At least one pattern required", nameof(patterns));
    
    foreach (string pattern in patterns)
    {
        AddRouteInternal(pattern, handler, description);
    }
    
    return this;
}
```

**Estimated effort:** ~30-45 minutes (implementation + tests + documentation)
