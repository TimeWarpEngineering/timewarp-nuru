# NuruCoreApp and NuruApp Merge Analysis

**Date:** 2026-01-20  
**Author:** Claude Code Analysis

---

## Executive Summary

Merging `NuruCoreApp` and `NuruApp` into a single `NuruApp` class is **straightforward and low-risk**. The current architecture already has almost all functionality in `NuruCoreApp` - `NuruApp` is essentially a thin subclass that only provides the `CreateBuilder()` factory method. The merge requires:

1. Moving `CreateBuilder()` from `NuruApp` into `NuruCoreApp`
2. Updating source generator locators to recognize the new naming
3. Updating documentation and samples (100+ files reference the current pattern)
4. Adding a type alias for backward compatibility

---

## Scope

This analysis covers:
- **Source files**: `nuru-core-app.cs`, `nuru-app.cs`, `nuru-app-builder.cs`, `nuru-core-app-builder.cs`
- **Source generators**: `CreateBuilderLocator`, `BuildLocator`, `AppExtractor`
- **Tests**: All test files using the `NuruCoreApp` type pattern
- **Documentation**: All markdown files referencing the current API
- **Samples**: All sample applications

---

## Methodology

Analysis performed via:
1. Static code analysis of class hierarchies
2. Source generator locator inspection
3. Grep-based usage counting across the codebase
4. Sample and documentation review

---

## Current Architecture Analysis

### Class Hierarchy

```
NuruCoreApp (runtime application class)
├── Properties: Terminal, ReplOptions, LoggerFactory, ShellCompletionProvider, CompletionSourceRegistry, TracerProvider, MeterProvider
├── Methods: RunAsync(), RunReplAsync(), FlushTelemetryAsync(), ConfigureCompletionRegistry()
└── Used by: Source generator interceptors

NuruApp (thin subclass)
└── Only adds: CreateBuilder(string[], NuruCoreApplicationOptions?) → NuruAppBuilder
    └── Inherits everything else from NuruCoreApp

NuruCoreAppBuilder<TSelf> (CRTP pattern base)
├── Methods: Map(), AddRepl(), AddBehavior(), WithName(), WithDescription(), ConfigureHelp(), AddConfiguration(), ConfigureServices(), UseLogging(), UseTerminal(), UseTelemetry(), etc.
└── Build() → NuruCoreApp

NuruCoreAppBuilder (non-generic entry point)
└── Inherits from: NuruCoreAppBuilder<NuruCoreAppBuilder>

NuruAppBuilder (full-featured with Aspire)
└── Inherits from: NuruCoreAppBuilder<NuruAppBuilder>, implements IHostApplicationBuilder, IDisposable
```

### Key Finding: What NuruApp Actually Adds

Looking at `nuru-app.cs` (47 lines total):

```csharp
public class NuruApp : NuruCoreApp
{
  public NuruApp() : base() { }

  public static NuruAppBuilder CreateBuilder(string[] args, NuruCoreApplicationOptions? options = null)
  {
    // Creates NuruAppBuilder and returns it
  }
}
```

**NuruApp contributes only:**
- The `CreateBuilder()` factory method
- The default constructor calling `base()`

**All functionality is already in NuruCoreApp:**
- Telemetry (TracerProvider, MeterProvider, FlushTelemetryAsync)
- Completion (ShellCompletionProvider, CompletionSourceRegistry, ConfigureCompletionRegistry)
- Terminal, ReplOptions, LoggerFactory
- RunAsync() and RunReplAsync() methods (source generator intercepts these)

### Usage Pattern

The canonical usage pattern across 100+ files:

```csharp
// Current pattern (all samples, tests, documentation)
NuruCoreApp app = NuruApp.CreateBuilder(args)
    .Map("greet {name}")
        .WithHandler((string name) => $"Hello, {name}!")
        .Done()
    .Build();

await app.RunAsync(args);
```

---

## Source Generator Analysis

### CreateBuilderLocator

**File:** `source/timewarp-nuru-analyzers/generators/locators/create-builder-locator.cs`

```csharp
private const string MethodName = "CreateBuilder";
private const string TypeName = "NuruApp";  // <-- HARDCODED

public static bool IsPotentialMatch(RoslynSyntaxNode node)
{
    // Checks for NuruApp.CreateBuilder(...)
}
```

**Required change:** No change needed - the factory method stays on `NuruApp` (either as a type alias or as the merged class).

### BuildLocator

**File:** `source/timewarp-nuru-analyzers/generators/locators/build-locator.cs`

```csharp
if (methodSymbol.ReturnType.Name != "NuruCoreApp")  // <-- HARDCODED
    return false;
```

**Required change:** After merge, update this check to:
- Option A: Accept both "NuruCoreApp" and "NuruApp"
- Option B: Change to "NuruApp" and add type alias for "NuruCoreApp"

### AppExtractor

**File:** `source/timewarp-nuru-analyzers/generators/extractors/app-extractor.cs`

Uses semantic analysis to trace from `RunAsync()` back through `Build()` to the builder. No type name hardcoding found - uses symbol info.

---

## Implementation Options

### Option 1: Rename NuruCoreApp to NuruApp (Recommended)

**Steps:**
1. Rename `nuru-core-app.cs` to `nuru-app.cs`
2. Rename class `NuruCoreApp` to `NuruApp`
3. Update `BuildLocator` to check for `NuruApp` return type
4. Add `NuruCoreApp` as a type alias: `using NuruCoreApp = TimeWarp.Nuru.NuruApp;`
5. Update all documentation and samples to use `NuruApp` directly
6. Update MCP server tools to generate `NuruApp` type

**Pros:**
- Single unified type
- No inheritance hierarchy to understand
- Factory method co-located with class
- Clear, simple API

**Cons:**
- Type alias adds slight indirection
- Requires updating all samples/documentation

### Option 2: Merge by Moving Factory Method

**Steps:**
1. Move `CreateBuilder()` from `NuruApp` to `NuruCoreApp`
2. Keep `NuruApp` as empty subclass (for backward compat)
3. Update `BuildLocator` to accept both types
4. Deprecate `NuruApp` class

**Pros:**
- Minimal source changes
- Backward compatible
- No sample updates required

**Cons:**
- Dead class remains in API
- Two types that do the same thing
- Confusing for new users

### Option 3: Inline Factory Method (Minimal Change)

**Steps:**
1. Move `CreateBuilder()` into `NuruCoreApp`
2. Delete `NuruApp` class entirely
3. Update `BuildLocator` to check for `NuruApp` (which now doesn't exist - creates confusion)

**Not recommended** - creates type that doesn't exist.

---

## Recommended Implementation: Option 1

### Phase 1: Source Code Changes

#### 1.1 Rename `nuru-core-app.cs` to `nuru-app.cs`

```csharp
// Old: namespace TimeWarp.Nuru; public class NuruCoreApp { ... }
// New:
namespace TimeWarp.Nuru;

public class NuruApp
{
  // All existing NuruCoreApp content
  public ITerminal Terminal { get; }
  public ReplOptions? ReplOptions { get; init; }
  public ILoggerFactory? LoggerFactory { get; set; }
  // ... all other properties and methods

  // NEW: Factory method moved from old NuruApp
  public static NuruAppBuilder CreateBuilder(string[] args, NuruCoreApplicationOptions? options = null)
  {
    // Implementation from old NuruApp
  }
}
```

#### 1.2 Delete old `nuru-app.cs`

The factory method is now in the renamed class.

#### 1.3 Add backward-compatibility type alias

Add to `global-usings.cs` or a new compatibility file:

```csharp
// For backward compatibility with existing code
using NuruCoreApp = TimeWarp.Nuru.NuruApp;
```

#### 1.4 Update `BuildLocator`

```csharp
// Old:
if (methodSymbol.ReturnType.Name != "NuruCoreApp")

// New: Accept both for transition period
if (methodSymbol.ReturnType.Name != "NuruCoreApp" && 
    methodSymbol.ReturnType.Name != "NuruApp")
```

### Phase 2: Update Source Generator

#### 2.1 Update `CreateBuilderLocator.cs`

```csharp
// No changes needed - still looks for NuruApp.CreateBuilder
```

#### 2.2 Update `BuildLocator.cs`

```csharp
// Accept both return types during transition
public static bool IsConfirmedBuildCall(...)
{
    // ... existing checks ...
    
    // Check that the return type is NuruApp (or NuruCoreApp for backward compat)
    string returnTypeName = methodSymbol.ReturnType.Name;
    if (returnTypeName != "NuruApp" && returnTypeName != "NuruCoreApp")
        return false;
    
    return true;
}
```

### Phase 3: Update MCP Server Tools

**Files requiring updates:**
- `source/timewarp-nuru-mcp/tools/get-attributed-route-tool.cs`
- `source/timewarp-nuru-mcp/tools/get-type-converter-tool.cs`
- `source/timewarp-nuru-mcp/tools/get-behavior-tool.cs`
- `source/timewarp-nuru-mcp/tools/generate-handler-tool.cs`

**Pattern to update:**
```csharp
// Old: NuruCoreApp app = NuruApp.CreateBuilder(args)
// New: NuruApp app = NuruApp.CreateBuilder(args)
// OR: using alias makes old code still work
```

---

## Files Requiring Updates

### Source Files (3 files)

| File | Change |
|------|--------|
| `source/timewarp-nuru/nuru-core-app.cs` | Rename to `nuru-app.cs`, rename class to `NuruApp`, add factory method |
| `source/timewarp-nuru/nuru-app.cs` | Delete (factory method moved) |
| `source/timewarp-nuru/global-usings.cs` | Add type alias for backward compatibility |

### Source Generator Files (1 file)

| File | Change |
|------|--------|
| `source/timewarp-nuru-analyzers/generators/locators/build-locator.cs` | Update return type check |

### MCP Server Tools (4 files)

| File | Change |
|------|--------|
| `source/timewarp-nuru-mcp/tools/get-attributed-route-tool.cs` | Update generated code pattern |
| `source/timewarp-nuru-mcp/tools/get-type-converter-tool.cs` | Update generated code pattern |
| `source/timewarp-nuru-mcp/tools/get-behavior-tool.cs` | Update generated code pattern |
| `source/timewarp-nuru-mcp/tools/generate-handler-tool.cs` | Update generated code pattern |

### Documentation (100+ files)

Documentation files use the pattern `NuruCoreApp app = NuruApp.CreateBuilder(args)`. With the type alias, these will continue to work without changes. However, for clarity, consider updating:

| File | Change |
|------|--------|
| `readme.md` | Update code examples to use `NuruApp` directly |
| `documentation/user/getting-started.md` | Update examples |
| `documentation/user/features/*.md` | Update examples (10+ files) |
| `documentation/user/guides/*.md` | Update examples (5+ files) |
| `documentation/user/reference/*.md` | Update examples (5+ files) |

### Samples (15+ files)

| File | Change |
|------|--------|
| `samples/01-hello-world/*.cs` | Update 2-3 files |
| `samples/02-calculator/*.cs` | Update 3 files |
| `samples/03-*/**/*.cs` | Update multiple files |
| `samples/07-*/**/*.cs` | Update 6 files |
| `samples/08-*/**/*.cs` | Update 4 files |
| `samples/09-*/**/*.cs` | Update 4 files |
| `samples/12cs` | Update-*. 1 file |
| `samples/13-*/**/*.cs` | Update 4 files |
| `samples/14-*/**/*.cs` | Update 2 files |
| `samples/15-*.cs` | Update 1 file |

### Test Files (70+ files)

Tests use `NuruCoreApp` type extensively. With the type alias, these should work without modification, but consider updating for clarity.

---

## Backward Compatibility Strategy

### Type Alias Approach

Add to `global-usings.cs`:

```csharp
// Backward compatibility - NuruCoreApp is now NuruApp
using NuruCoreApp = TimeWarp.Nuru.NuruApp;
```

This allows existing code like:
```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)...Build();
```

To continue working because `NuruCoreApp` is an alias for `NuruApp`.

### Deprecation Attributes

Consider adding Obsolete attribute to the alias:

```csharp
/// <summary>
/// Backward compatibility alias. Use <see cref="NuruApp"/> directly.
/// </summary>
[System.Obsolete("Use NuruApp instead. This alias will be removed in a future version.")]
using NuruCoreApp = TimeWarp.Nuru.NuruApp;
```

---

## Breaking Changes Assessment

| Change | Breaking? | Impact |
|--------|-----------|--------|
| Rename `NuruCoreApp` class to `NuruApp` | Low | Type alias provides compatibility |
| Delete `NuruApp` class | Low | Factory method moved to main class |
| Update `BuildLocator` return type check | No | Accepts both types during transition |
| MCP tool output changes | Low | Generated code pattern only |

**Overall Breaking Change Risk: LOW**

The type alias ensures existing code continues to compile and work.

---

## Recommendations

### Priority 1 (Must Do)

1. **Rename `nuru-core-app.cs` to `nuru-app.cs`**
   - Move `CreateBuilder()` factory method into the class
   - Rename class from `NuruCoreApp` to `NuruApp`

2. **Update `BuildLocator`**
   - Accept both `NuruCoreApp` and `NuruApp` as return types
   - This ensures transition compatibility

3. **Add type alias for backward compatibility**
   - Add `using NuruCoreApp = TimeWarp.Nuru.NuruApp;` to `global-usings.cs`

### Priority 2 (Should Do)

4. **Update source generator debug diagnostics**
   - Ensure diagnostic messages reflect new type names

5. **Add Obsolete attribute to type alias**
   - Guides users toward the new naming

### Priority 3 (Nice to Have)

6. **Update documentation and samples**
   - Change examples from `NuruCoreApp app = ...` to `NuruApp app = ...`
   - This is cosmetic - existing patterns continue to work

7. **Update MCP server tools**
   - Generate `NuruApp` type in new generated code
   - Existing generated code continues to work via alias

---

## Testing Strategy

### Unit Tests

All existing tests should pass without modification due to the type alias. Verify:

1. Run full test suite: `dotnet run tests/ci-tests/run-ci-tests.cs`
2. Check that all 500+ tests pass
3. Pay special attention to routing tests

### Sample Validation

1. Run hello world sample: `./samples/01-hello-world/01-hello-world-lambda.cs`
2. Run calculator samples
3. Verify REPL mode works

### Source Generator Tests

1. Run generator-specific tests: `tests/timewarp-nuru-tests/generator/`
2. Verify interceptors work correctly
3. Check that Build() return type is correctly identified

---

## Rollout Plan

### Step 1: Implement Changes (1-2 hours)

1. Rename and modify source files
2. Update source generator locators
3. Add type alias

### Step 2: Verify Tests (30 minutes)

1. Run full test suite
2. Fix any failures

### Step 3: Documentation Updates (1-2 hours)

1. Update README
2. Update key documentation files
3. Update sample code

### Step 4: MCP Server Update (1 hour)

1. Update tool code generation
2. Test MCP tools still work

---

## Conclusion

Merging `NuruCoreApp` and `NuruApp` is **straightforward and low-risk**. The key insight is that `NuruApp` is already essentially empty - it only provides the `CreateBuilder()` factory method while all actual functionality lives in `NuruCoreApp`.

The recommended approach is:
1. **Rename `NuruCoreApp` to `NuruApp`**
2. **Move the factory method into the renamed class**
3. **Add a type alias for backward compatibility**
4. **Update source generator to accept both type names**

This results in a cleaner API with a single `NuruApp` type while maintaining full backward compatibility for existing code through the type alias.

---

## References

- **Source files analyzed:**
  - `source/timewarp-nuru/nuru-core-app.cs`
  - `source/timewarp-nuru/nuru-app.cs`
  - `source/timewarp-nuru/nuru-app-builder.cs`
  - `source/timewarp-nuru/builders/nuru-core-app-builder/nuru-core-app-builder.cs`
  - `source/timewarp-nuru-analyzers/generators/locators/create-builder-locator.cs`
  - `source/timewarp-nuru-analyzers/generators/locators/build-locator.cs`
  - `source/timewarp-nuru-analyzers/generators/extractors/app-extractor.cs`

- **Usage pattern found in 100+ files:**
  - All samples use `NuruCoreApp app = NuruApp.CreateBuilder(args)...Build();`
  - All tests use the same pattern
  - All documentation examples use the same pattern
