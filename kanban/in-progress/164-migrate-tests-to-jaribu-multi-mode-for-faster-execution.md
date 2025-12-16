# Migrate tests to Jaribu multi-mode for faster execution

## Description

Current test execution takes ~15 minutes because each of the 191 test files compiles and runs independently. Jaribu's multi-mode pattern allows all test files to be compiled together once, reducing execution time to ~10-30 seconds.

### Problem
- 191 test files × ~5-10 seconds compile time = ~15+ minutes
- Each file triggers separate compilation/JIT phase
- No shared compilation across files

### Solution
Use Jaribu multi-mode pattern:
1. Add `[ModuleInitializer]` to each test class for auto-registration
2. Wrap `RunAllTests()` in `#if !JARIBU_MULTI` to prevent self-execution in multi-mode
3. Create single orchestrator that compiles all tests together with `JARIBU_MULTI` defined

### Per-File Migration Pattern

From:
```csharp
#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

return await RunTests<MyTests>(clearCache: true);

[TestTag("Category")]
[ClearRunfileCache]
public class MyTests
{
    public static async Task Should_do_something() { ... }
}
```

To:
```csharp
#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Category")]
public class MyTests
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MyTests>();
    
    public static async Task Should_do_something() { ... }
}
```

## Checklist

### Phase 1: Infrastructure Setup
- [ ] Add `<Using Include="System.Runtime.CompilerServices" />` to `tests/Directory.Build.props`
- [ ] Create `tests/ci-tests/` directory
- [ ] Create `tests/ci-tests/Directory.Build.props` with JARIBU_MULTI and file includes
- [ ] Create `tests/ci-tests/run-ci-tests.cs` orchestrator

### Phase 2: Test File Migration (~191 files)
- [ ] Migrate Lexer tests (15 files)
- [ ] Migrate Parser tests (15 files)
- [ ] Migrate Routing tests (22 files)
- [ ] Migrate Core tests (26 files)
- [ ] Migrate Completion tests (26 files)
- [ ] Migrate REPL tests (47 files)
- [ ] Migrate MCP tests (7 files)
- [ ] Migrate Analyzer tests (3 files)
- [ ] Migrate Type conversion tests (1 file)
- [ ] Migrate Factory tests (1 file)
- [ ] Migrate Configuration tests (1 file)

### Phase 3: Validation
- [ ] Test standalone execution still works (individual files)
- [ ] Test multi-mode execution works (orchestrator)
- [ ] Verify test count matches between modes

### Phase 4: CI Integration
- [ ] Update tests/scripts/run-all-tests.cs to use orchestrator (or keep as fallback)
- [ ] Update CI workflow if needed

## Acceptance Criteria

- [ ] All tests pass in multi-mode
- [ ] Individual test files still run standalone
- [ ] CI pipeline uses multi-mode orchestrator
- [ ] Test execution time under 1 minute

## Implementation Details

### Phase 1: Infrastructure Files

**tests/Directory.Build.props** - Add this using statement:
```xml
<Using Include="System.Runtime.CompilerServices" />
```

**tests/ci-tests/Directory.Build.props:**
```xml
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
  <PropertyGroup>
    <DefineConstants>$(DefineConstants);JARIBU_MULTI</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <!-- All test files from all categories -->
    <Compile Include="../timewarp-nuru-analyzers-tests/auto/*.cs" />
    <Compile Include="../timewarp-nuru-core-tests/*.cs" Exclude="../timewarp-nuru-core-tests/*helper*.cs" />
    <Compile Include="../timewarp-nuru-tests/**/*.cs" Exclude="../timewarp-nuru-tests/**/*helper*.cs" />
    <Compile Include="../timewarp-nuru-completion-tests/**/*.cs" Exclude="../timewarp-nuru-completion-tests/**/*helper*.cs" />
    <Compile Include="../timewarp-nuru-repl-tests/**/*.cs" Exclude="../timewarp-nuru-repl-tests/**/*helper*.cs" />
    <Compile Include="../timewarp-nuru-mcp-tests/*.cs" />
  </ItemGroup>
</Project>
```

**tests/ci-tests/run-ci-tests.cs:**
```csharp
#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj
#:project ../../source/timewarp-nuru-completion/timewarp-nuru-completion.csproj
#:project ../../source/timewarp-nuru-mcp/timewarp-nuru-mcp.csproj

// Multi-mode Test Runner
// Test classes are auto-registered via [ModuleInitializer] when compiled with JARIBU_MULTI.

WriteLine("TimeWarp.Nuru Multi-Mode Test Runner");
WriteLine();

return await RunAllTests();
```

### Phase 2: Per-File Migration Changes

For each test file:
1. Change `return await RunTests<T>(clearCache: true);` → `return await RunAllTests();` inside `#if !JARIBU_MULTI`
2. Remove `[ClearRunfileCache]` attribute (orchestrator handles cache clearing)
3. Add `[ModuleInitializer]` method that calls `RegisterTests<T>()`

**Helper files** (no changes needed):
- `tests/timewarp-nuru-tests/lexer/lexer-test-helper.cs`
- `tests/timewarp-nuru-core-tests/compiled-route-test-helper.cs`
- `tests/timewarp-nuru-repl-tests/tab-completion/completion-test-helpers.cs`

**Files with multiple test classes** - Each class needs its own `[ModuleInitializer]`

### Test File Inventory

| Category | Directory | Files | Notes |
|----------|-----------|-------|-------|
| Analyzers | timewarp-nuru-analyzers-tests/auto | 3 | |
| Core | timewarp-nuru-core-tests | 26 | 1 helper file excluded |
| Lexer | timewarp-nuru-tests/lexer | 15 | 1 helper file excluded |
| Parser | timewarp-nuru-tests/parsing/parser | 15 | |
| Routing | timewarp-nuru-tests/routing | 22 | |
| TypeConversion | timewarp-nuru-tests/type-conversion | 1 | |
| Factory | timewarp-nuru-tests/factory | 1 | |
| Configuration | timewarp-nuru-tests/configuration | 1 | |
| Completion | timewarp-nuru-completion-tests | 26 | static + dynamic + engine |
| REPL | timewarp-nuru-repl-tests | 47 | includes tab-completion + command-line-parser |
| MCP | timewarp-nuru-mcp-tests | 7 | |
| **Total** | | **~191** | |

## Clarifying Questions (Decision Needed)

### 1. Migration Approach
**Options:**
- **A) Automated script migration** - Write a script to transform all 191 files programmatically
- **B) Manual category-by-category** - Migrate files manually, one category at a time

**Recommendation:** Option A is faster but riskier. Option B allows validation at each step.

### 2. Helper Files Handling
The current `run-all-tests.cs` excludes files with "helper" in the name. 
- Should we maintain this pattern in `Directory.Build.props` excludes?
- Or should helper files be explicitly namespaced to avoid issues?

**Current helper files:**
- `lexer-test-helper.cs`
- `compiled-route-test-helper.cs`
- `completion-test-helpers.cs`

### 3. Test File Project References
Some test files have additional `#:project` directives (e.g., REPL tests reference `timewarp-nuru-repl.csproj`).

**Options:**
- **A) Single orchestrator with all project references** - Simpler, one compilation unit
- **B) Multiple orchestrators per category** - More complex, but mirrors current category structure

**Recommendation:** Option A for simplicity unless compilation issues arise.

### 4. Existing `run-all-tests.cs`
**Options:**
- **A) Replace entirely** with new orchestrator
- **B) Keep as parallel execution fallback** (for debugging individual categories)
- **C) Update to optionally use multi-mode** via command-line flag

**Recommendation:** Option B or C - keep existing script for backward compatibility.

## Notes

- Helper files (no test classes) don't need `[ModuleInitializer]`
- Individual test files remain runnable standalone for debugging
- Single orchestrator chosen over per-category for simplicity
- Expected result: Test execution time reduced from ~15 minutes to ~10-30 seconds
- Reference implementation: TimeWarp.Jaribu's own test suite uses this pattern
