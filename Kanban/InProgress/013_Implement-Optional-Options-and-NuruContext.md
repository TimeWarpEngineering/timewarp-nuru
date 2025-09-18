# Implement Optional Options and NuruContext

## Architecture Design Document

**See: [Route Syntax and Specificity Design](../../Documentation/Developer/Design/route-syntax-and-specificity.md)**

This document defines the complete syntax and behavior for:
- Optional flags with `?` modifier
- Repeated options with `*` modifier
- Positional parameter rules
- Arrays and catch-all patterns
- Command interception patterns

## Problem Statement

Currently, Nuru requires options defined in a route pattern to be present for the route to match. This leads to factorial explosion when multiple optional options are needed:
- 3 optional options = 3! (6) route definitions needed
- 4 optional options = 4! (24) route definitions needed

Additionally, some advanced scenarios (like progressively overriding existing CLI commands) need access to the full parsing context, not just the matched parameters.

## Core Design Decisions

1. **Explicit Flag Modifiers**: Route patterns are self-contained
   - `--flag` = Required flag
   - `--flag?` = Optional flag
   - `--flag {param}*` = Repeated flag (array)

2. **Required Options ARE Valid**: Many CLIs have required options (e.g., `git commit -m`)
   - If flag has no `?`, it's required
   - This supports intercepting existing commands accurately

## Proposed Solution

### Part 1: Make Options Inherently Optional

All options in route patterns should be optional by default:

```csharp
// This single route should match ALL of these:
.AddRoute("deploy {env} --version {ver} --config {cfg} --force",
    (string env, string? ver, string? cfg, bool force) => ...)

// Matches:
// - deploy prod
// - deploy prod --force
// - deploy prod --version 1.0
// - deploy prod --config prod.json --force
// - etc. (all 8 combinations)
```

#### Key Semantics:
- **Positional parameters** (`{env}`) - Required if non-nullable, optional if nullable
- **Option parameters** (`--version {ver}`) - ALWAYS optional (regardless of type)
- **Boolean options** (`--force`) - ALWAYS optional (false when omitted)
- If an option flag IS provided, it MUST have a value (no `--config` without value)
- **Best practice**: Option parameters should use nullable types to avoid runtime errors

### Part 2: NuruContext for Advanced Scenarios

For cases where delegates need more than just parameters, provide an optional context:

```csharp
// Simple delegate (current, keep working)
.AddRoute("build --config {mode}", (string? mode) => ...)

// Context-aware delegate (new)
.AddRoute("build --config {mode}", (NuruContext ctx) =>
{
    var mode = ctx.GetOption<string>("config");
    var hasVerbose = ctx.HasOption("verbose");
    // Access to everything without declaring all options
})

// Mixed - parameters AND context (new)
.AddRoute("build --config {mode}", (string? mode, NuruContext ctx) => ...)
```

## Implementation Strategy

### Phase 0: Update Tests to Define Expected Behavior

**CRITICAL: Update existing tests FIRST to clarify what should work**

Tests to update/revise:
- `/Tests/SingleFileTests/Features/test-optional-option-params.cs` - Current test expects wrong behavior
- `/Tests/SingleFileTests/Features/test-truly-optional-options.cs` - Needs to verify boolean options work correctly
- `/Tests/SingleFileTests/Features/test-mixed-options.cs` - Should show value options are optional too
- `/Tests/SingleFileTests/Features/test-option-combinations.cs` - Should show single route handles all combos
- `/Tests/SingleFileTests/Features/test-four-optional-options.cs` - Should prove no factorial explosion

Key test scenarios to validate:
```csharp
// Test 1: Options with values are fully optional
.AddRoute("build --config {mode}", (string? mode) => ...)
// Should match:
// - "build" (mode = null)
// - "build --config debug" (mode = "debug")
// Should NOT match:
// - "build --config" (error: flag requires value)

// Test 2: Multiple routes with same command type merge conceptually
.AddRoute<SyncCommand>("sync {repo?} --all --verbose")
// Single route handles all variations that previously needed 3+ routes

// Test 3: Boolean options remain optional (current behavior)
.AddRoute("test --verbose", (bool verbose) => ...)
// - "test" (verbose = false)
// - "test --verbose" (verbose = true)

// Test 4: Required vs Optional is based on position, not option
.AddRoute("deploy {env} --version {ver?}", (string env, string? ver) => ...)
// - "deploy" - ERROR (missing required positional)
// - "deploy prod" - OK (ver = null)
// - "deploy prod --version 1.0" - OK (ver = "1.0")
```

### Phase 1: Update Route Matching Logic

**File: `/Source/TimeWarp.Nuru/CommandResolver/RouteBasedCommandResolver.cs`**
- Modify matching algorithm to treat ALL options as optional (regardless of type)
- Line 218: Change `if (optionSegment.ExpectsValue)` logic
- Options should match even when not present in input

**File: `/Source/TimeWarp.Nuru.Parsing/Parsing/Compiler/RouteCompiler.cs`**
- Update compilation to mark ALL options as optional (not based on nullability)
- Lines 104-137: Modify option matcher creation
- Options are ALWAYS optional; nullability only affects positional parameters

### Phase 2: Add NuruContext Support

**New File: `/Source/TimeWarp.Nuru/NuruContext.cs`**
```csharp
public class NuruContext
{
    private readonly ParseResult parseResult;
    private readonly RouteMatchResult matchResult;
    private readonly string[] originalArgs;

    public string[] OriginalArgs => originalArgs;
    public IReadOnlyDictionary<string, object?> Options { get; }
    public IReadOnlyList<string> PositionalArgs { get; }
    public IReadOnlyList<string> UnmatchedArgs { get; }

    public T? GetOption<T>(string name);
    public bool HasOption(string name);
    public string? GetPositional(int index);
}
```

### Phase 3: AOT-Compatible Overloads

**File: `/Source/TimeWarp.Nuru/NuruAppBuilder.cs`**

Add new overloads that accept context (resolved at compile-time, no reflection):

```csharp
// Context-only overloads
public NuruAppBuilder AddRoute(string pattern, Action<NuruContext> handler)
public NuruAppBuilder AddRoute(string pattern, Func<NuruContext, Task> handler)

// Mixed parameter + context overloads
public NuruAppBuilder AddRoute<T1>(string pattern, Action<T1, NuruContext> handler)
public NuruAppBuilder AddRoute<T1, T2>(string pattern, Action<T1, T2, NuruContext> handler)
// ... up to reasonable parameter count
```

**File: `/Source/TimeWarp.Nuru/Endpoints/RouteEndpoint.cs`**
- Add `bool WantsContext` property (set at registration based on overload used)
- Store parameter count for proper casting at execution

**File: `/Source/TimeWarp.Nuru/NuruApp.cs`**
- Update execution to check `WantsContext` flag
- Only create NuruContext if needed
- Cast to appropriate delegate type based on stored metadata

### Phase 4: Update Parameter Binding

**File: `/Source/TimeWarp.Nuru/NuruApp.cs`** (Line ~322)
- Update `BindParameters` to handle optional options
- Pass null for omitted optional options (only works if parameter is nullable)
- For non-nullable option parameters: Either runtime error or use default(T)
- Validate that present options have values

### Phase 5: Add Analyzer Rule NURU010

**File: `/Source/TimeWarp.Nuru.Analyzers/Diagnostics/DiagnosticDescriptors.cs`**
- Add NURU010: "Option parameters must be nullable"
- Severity: Error (not just warning - this should be enforced)

**File: `/Source/TimeWarp.Nuru.Analyzers/RoutePatternAnalyzer.cs`**

**For Delegate Routes:**
- Check delegate parameter types match nullability rules
- Verify all option parameters in delegate have nullable types
- Boolean options are exempt (bool is fine, doesn't need bool?)

**For Mediator Command Routes:**
- When `AddRoute<TCommand>()` is used, analyze TCommand properties
- Match route pattern parameters to command properties by name
- Verify properties for options are nullable
- This is harder: Need to resolve generic type, find properties, match names

**Examples:**
```csharp
// ✅ Correct DELEGATE - option parameters are nullable
.AddRoute("build --config {mode}", (string? mode) => ...)

// ❌ Error NURU010 - delegate option parameter is not nullable
.AddRoute("build --config {mode}", (string mode) => ...)

// ✅ Correct MEDIATOR - option properties are nullable
public class BuildCommand : IRequest
{
    public string? Config { get; set; }  // Nullable - good!
    public bool Verbose { get; set; }    // Bool is fine
}
.AddRoute<BuildCommand>("build --config {config} --verbose")

// ❌ Error NURU010 - command option property is not nullable
public class BuildCommand : IRequest
{
    public string Config { get; set; }  // NOT nullable - error!
}
.AddRoute<BuildCommand>("build --config {config}")
```

**Implementation Note:**
The Mediator pattern checking is more complex because:
1. Need to resolve the generic type parameter `TCommand`
2. Find properties on that type
3. Match property names to route pattern parameter names (case-insensitive?)
4. Check nullability of matched properties

This might need to be a separate analyzer or a Phase 5b if too complex.

### Phase 6: Update Documentation

**File: `/Documentation/Developer/Reference/RoutePatternSyntax.md`**
- Update to clarify ALL options are optional by default
- Emphasize that nullability only affects positional parameters
- Add examples showing single route handles multiple option combinations
- Add section on NuruContext for advanced scenarios

### Phase 7: Update MCP Server

**File: `/Source/TimeWarp.Nuru.Mcp/Tools/GetSyntaxTool.cs`**
- Update the "optional" section to clarify options are always optional
- Update examples to show nullable option parameters
- Add section about NuruContext

**File: `/Source/TimeWarp.Nuru.Mcp/Tools/GenerateHandlerTool.cs`**
- Update handler generation to use nullable types for option parameters
- Add support for generating NuruContext-aware handlers

**File: `/Source/TimeWarp.Nuru.Mcp/Tools/ValidateRouteTool.cs`**
- Update validation to reflect new optional options semantics
- Warn/error on non-nullable option parameters

**File: `/Source/TimeWarp.Nuru.Mcp/README.md`**
- Update examples to show new optional options behavior
- Add NuruContext examples

## Test Scenarios

### Test 1: Single Route Handles All Combinations
```csharp
.AddRoute("deploy {env} --version {v?} --config {c?} --force",
    (string env, string? v, string? c, bool force) => ...)

// All 8 combinations should work with one route
```

### Test 2: Context Access
```csharp
.AddRoute("gh pr create", (NuruContext ctx) =>
{
    if (ctx.HasOption("--head"))
        // Custom behavior
    else
        Shell.Run("gh", ctx.OriginalArgs);  // Pass through
})
```

### Test 3: Progressive Override
```csharp
// Override specific git commands
.AddRoute("git commit", (NuruContext ctx) =>
{
    if (ctx.HasOption("-m"))
        // Custom commit logic
    else
        OpenEditor();  // Custom enhancement
})

// Pass through everything else
.AddRoute("git {*rest}", (string[] rest) => Shell.Run("git", rest))
```

### Test 4: Mixed Parameters and Context
```csharp
.AddRoute("backup {source} --dest {dest?}", (string source, string? dest, NuruContext ctx) =>
{
    // Use parameters for declared options
    // Use context to check for undeclared flags like --verbose
    if (ctx.HasOption("--verbose"))
        Console.WriteLine($"Backing up {source}...");
})
```

## Migration Considerations

### Breaking Changes
- Routes with options will now match when options are absent
- This changes existing behavior where options were required

### Migration Options
1. **Feature Flag** (Recommended for transition):
   ```csharp
   new NuruAppBuilder()
       .UseOptionalOptions()  // Opt-in to new behavior
   ```

2. **Major Version Bump**: Make it default in v2.0

3. **Analyzer Warning**: Warn when non-nullable types used with options

## Benefits

1. **Eliminates Factorial Explosion**: One route handles all option combinations
2. **Aligns with Industry Standards**: Options are optional in Click, Cobra, etc.
3. **Progressive Enhancement**: Can override commands incrementally via context
4. **AOT Compatible**: No reflection, all compile-time resolution
5. **Backward Compatible**: Existing delegates without context keep working
6. **Performance**: Context only created when needed

## Design Decisions from Discussion

### Why Not Brackets Syntax `[--config {mode}]`?
- Redundant for boolean options (they're already optional)
- The `?` in `{mode?}` already signals nullability to C# developers
- Simpler to align route pattern with delegate signature

### Why Not Required Options?
- If something is required, it should be a positional parameter
- Options by definition should be optional modifiers
- This is the universal pattern across CLI frameworks

### Why Context Pattern?
- Enables progressive command override (gh, gw use cases)
- Provides access to parse info without declaring every possible option
- Allows checking for undeclared options without factorial explosion
- Similar to ASP.NET Core's HttpContext pattern

## Progress Update (2025-09-18)

### Completed
- ✅ Created comprehensive design documents:
  - `route-syntax-and-specificity.md` - Complete syntax specification
  - `greenfield-route-syntax.md` - Focused greenfield approach
- ✅ Created test matrix and test files:
  - **Application-level tests** in `/Tests/SingleFileTests/test-matrix/`:
    - 12 test files covering all patterns (optional flags, repeated options, arrays, etc.)
  - **Parser tests** in `/Tests/SingleFileTests/Parser/`:
    - `test-parser-optional-flags.cs` - Tests for `?` modifier
    - `test-parser-repeated-options.cs` - Tests for `*` modifier
    - `test-parser-mixed-modifiers.cs` - Combined modifiers
    - `test-parser-error-cases.cs` - Invalid patterns
  - **Lexer tests** in `/Tests/SingleFileTests/Lexer/`:
    - `test-lexer-optional-modifiers.cs` - Verified lexer already handles `?` and `*`
- ✅ Established baseline for existing tests:
  - Fixed project paths in all parser/lexer tests
  - Documented current test status
  - Deleted obsolete test using old APIs
- ✅ **Key Finding**: Lexer already tokenizes `?` and `*` correctly - no lexer changes needed!

### Completed (2025-09-18 continued)
- ✅ **Phase 1 COMPLETE**: Parser now recognizes `?` on flags
   - Added `IsOptional` property to `OptionSyntax` record
   - Updated `ParseOption` method to check for `?` token after option name
   - **Test result**: `test-parser-optional-flags.cs` - ALL patterns with `--flag?` now parse successfully!

### Next Steps
1. **Phase 2**: Update RouteCompiler to create OptionMatcher with IsOptional
2. **Phase 3**: Update RouteBasedCommandResolver to check IsOptional property
3. **Phase 4**: Update parser to support `*` for repeated options
4. Continue with remaining phases...

## Success Criteria

- [ ] **Tests updated and passing that define the new behavior**
- [ ] Single route can handle all combinations of optional options
- [ ] No factorial explosion for multiple options
- [ ] SyncCommand pattern (same command, different routes) merges cleanly
- [ ] Context available when needed, not created when not needed
- [ ] AOT compilation still works (no reflection)
- [ ] Existing routes without context continue working
- [ ] Clear migration path for existing users
- [ ] Performance no worse than current implementation

## Test Files to Update/Create

1. **test-optional-options-behavior.cs** (new)
   - Replaces confusing existing tests
   - Clearly shows: options are optional, positionals are required
   - Shows `--flag` without value is an error for value options

2. **test-sync-command-pattern.cs** (new)
   - Shows how 3 routes become 1 with optional options
   - Validates command property binding works correctly

3. **test-nurucontext-access.cs** (new)
   - Tests context-only delegates
   - Tests mixed parameter + context delegates
   - Shows progressive override pattern (gh/gw style)

4. **Remove/Archive** these confusing tests:
   - test-optional-option-params.cs (wrong expectations)
   - test-option-combinations.cs (wrong pattern)
   - test-four-optional-options.cs (needs rewrite)

## Notes

This change represents a fundamental shift in how Nuru interprets options, aligning it with industry standards while maintaining its performance and AOT compatibility goals. The NuruContext addition provides an escape hatch for complex scenarios without forcing all users to deal with that complexity.