# Migrate completion-26 and completion-11 to Jaribu multi-mode

## Description

Migrate two completion test files to support Jaribu multi-mode pattern, enabling them to run in the consolidated CI test runner.

## Checklist

**File 1: tests/timewarp-nuru-completion-tests/dynamic/completion-26-enum-partial-filtering.cs**
- [ ] Add `#if !JARIBU_MULTI` wrapper around `RunAllTests()` call
- [ ] Add `[ModuleInitializer]` registration method
- [ ] Remove `[ClearRunfileCache]` if present
- [ ] Add file to `ci-tests/Directory.Build.props`
- [ ] Verify standalone mode works
- [ ] Verify multi-mode works

**File 2: tests/timewarp-nuru-completion-tests/static/completion-11-enum-completion.cs**
- [ ] Add `#if !JARIBU_MULTI` wrapper around `RunAllTests()` call
- [ ] Add `[ModuleInitializer]` registration method
- [ ] Remove `[ClearRunfileCache]` if present
- [ ] Add file to `ci-tests/Directory.Build.props`
- [ ] Verify standalone mode works
- [ ] Verify multi-mode works

## Implementation Details

### Per-file changes

**Before:**
```csharp
#!/usr/bin/dotnet --

return await RunTests<TestClassName>();

[TestTag("Completion")]
public class TestClassName
{
```

**After:**
```csharp
#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Completion")]
public class TestClassName
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<TestClassName>();
```

### Verification commands

**Standalone:**
```bash
dotnet tests/timewarp-nuru-completion-tests/dynamic/completion-26-enum-partial-filtering.cs
dotnet tests/timewarp-nuru-completion-tests/static/completion-11-enum-completion.cs
```

**Multi-mode:**
```bash
dotnet tests/ci-tests/run-ci-tests.cs
```

## Notes

- These are the first completion tests to be migrated to multi-mode
- May need to add completion test helper to ci-tests if one exists
- Check if InternalsVisibleTo is needed for completion tests
