# Migrate completion and repl tests to Jaribu multi-mode

## Description

Migrate completion and repl test files to support Jaribu multi-mode pattern, enabling them to run in the consolidated CI test runner.

## Checklist

**File 1: tests/timewarp-nuru-completion-tests/dynamic/completion-26-enum-partial-filtering.cs**
- [x] Add `#if !JARIBU_MULTI` wrapper around `RunAllTests()` call
- [x] Add `[ModuleInitializer]` registration method
- [x] Remove `[ClearRunfileCache]` if present
- [x] Add file to `ci-tests/Directory.Build.props`
- [x] Verify standalone mode works
- [x] Verify multi-mode works

**File 2: tests/timewarp-nuru-completion-tests/static/completion-11-enum-completion.cs**
- [x] Add `#if !JARIBU_MULTI` wrapper around `RunAllTests()` call
- [x] Add `[ModuleInitializer]` registration method
- [x] Remove `[ClearRunfileCache]` if present
- [x] Add file to `ci-tests/Directory.Build.props`
- [x] Verify standalone mode works
- [x] Verify multi-mode works

**File 3: tests/timewarp-nuru-repl-tests/repl-16-enum-completion.cs**
- [x] Add `#if !JARIBU_MULTI` wrapper around `RunAllTests()` call
- [x] Add `[ModuleInitializer]` registration method
- [x] Add namespace block to avoid class name collision
- [x] Add file to `ci-tests/Directory.Build.props`
- [x] Verify standalone mode works
- [x] Verify multi-mode works

**File 4: tests/timewarp-nuru-repl-tests/repl-17-sample-validation.cs**
- [x] Add `#if !JARIBU_MULTI` wrapper around `RunAllTests()` call
- [x] Add `[ModuleInitializer]` registration method
- [x] Add namespace block to avoid class name collision
- [x] Remove `[ClearRunfileCache]` if present
- [x] Add file to `ci-tests/Directory.Build.props`
- [x] Verify standalone mode works
- [x] Verify multi-mode works

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

### For files with class name collisions (repl tests)

Wrap types in a namespace block:

```csharp
#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests
{
  [TestTag("REPL")]
  public class TestClassName
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<TestClassName>();
    // ...
  }
}
```

### Verification commands

**Standalone:**
```bash
dotnet tests/timewarp-nuru-completion-tests/dynamic/completion-26-enum-partial-filtering.cs
dotnet tests/timewarp-nuru-completion-tests/static/completion-11-enum-completion.cs
dotnet tests/timewarp-nuru-repl-tests/repl-16-enum-completion.cs
dotnet tests/timewarp-nuru-repl-tests/repl-17-sample-validation.cs
```

**Multi-mode:**
```bash
dotnet tests/ci-tests/run-ci-tests.cs
```

## Infrastructure Changes Made

- Created `source/timewarp-nuru/internals-visible-to.cs` with `InternalsVisibleTo("run-ci-tests")`
- Added `global using System.Runtime.CompilerServices;` to `source/timewarp-nuru/GlobalUsings.cs`
- Added ProjectReference to `timewarp-nuru-completion.csproj` in ci-tests
- Added `NoWarn` for RCS1163, IDE0161, IDE0065 in ci-tests

## Notes

- These are the first completion tests to be migrated to multi-mode
- Block namespaces required for repl tests to avoid class name collisions
- InternalsVisibleTo needed for `run-ci-tests` assembly to access internal constructors
