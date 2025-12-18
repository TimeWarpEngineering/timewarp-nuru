# Migrate lexer-01-basic-token-types.cs to Jaribu multi-mode

## Description

Migrate the first lexer test file to support Jaribu multi-mode pattern. This serves as the template for migrating all other test files.

**File:** `tests/timewarp-nuru-tests/lexer/lexer-01-basic-token-types.cs`

## Checklist

- [ ] Modify test file to support multi-mode
- [ ] Add file include to `tests/ci-tests/Directory.Build.props`
- [ ] Verify standalone mode works (`dotnet tests/timewarp-nuru-tests/lexer/lexer-01-basic-token-types.cs`)
- [ ] Verify multi-mode works (`dotnet tests/ci-tests/run-ci-tests.cs`)

## Implementation Details

### 1. Modify test file

**Current (lines 1-7):**
```csharp
#!/usr/bin/dotnet --

return await RunTests<BasicTokenTypesTests>();

[TestTag("Lexer")]
[ClearRunfileCache]
public class BasicTokenTypesTests
{
```

**Target:**
```csharp
#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Lexer")]
public class BasicTokenTypesTests
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<BasicTokenTypesTests>();
```

**Changes:**
1. Replace `return await RunTests<BasicTokenTypesTests>();` with `return await RunAllTests();` inside `#if !JARIBU_MULTI` block
2. Remove `[ClearRunfileCache]` attribute
3. Add `[ModuleInitializer]` method as first member of the class

### 2. Update orchestrator includes

Add to `tests/ci-tests/Directory.Build.props`:
```xml
<ItemGroup>
  <Compile Include="../timewarp-nuru-tests/lexer/lexer-01-basic-token-types.cs" />
</ItemGroup>
```

## Parent Task

This is part of [task 164](../in-progress/164-migrate-tests-to-jaribu-multi-mode-for-faster-execution.md).

## Notes

- This is the first test file migration - use as template for others
- Standalone mode: `#if !JARIBU_MULTI` block runs, calls `RunAllTests()` which runs just this one registered class
- Multi-mode: `JARIBU_MULTI` defined, block skipped, `[ModuleInitializer]` registers class for orchestrator
