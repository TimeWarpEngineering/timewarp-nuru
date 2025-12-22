# Migrate remaining repl tests to Jaribu multi-mode

## Description

Migrate all remaining repl test files (repl-01 through repl-35, excluding already migrated repl-16 and repl-17) to support Jaribu multi-mode pattern, enabling them to run in the consolidated CI test runner.

## Files Migrated (34 files)

- [x] repl-01-session-lifecycle.cs (migrated in previous session)
- [x] repl-02-command-parsing.cs (migrated in previous session)
- [x] repl-03-history-management.cs (migrated in previous session)
- [x] repl-03b-history-security.cs (migrated in previous session)
- [x] repl-04-history-persistence.cs (migrated in previous session)
- [x] repl-05-console-input.cs (migrated in previous session)
- [x] repl-06-tab-completion-basic.cs (migrated in previous session)
- [x] repl-07-tab-completion-advanced.cs (migrated in previous session)
- [x] repl-08-syntax-highlighting.cs (migrated in previous session)
- [x] repl-09-builtin-commands.cs (migrated in previous session)
- [x] repl-10-error-handling.cs (migrated in previous session)
- [x] repl-11-display-formatting.cs (migrated in previous session)
- [x] repl-12-configuration.cs (migrated in previous session)
- [x] repl-13-nuruapp-integration.cs (migrated in previous session)
- [x] repl-14-performance.cs (migrated in previous session)
- [x] repl-15-edge-cases.cs (migrated in previous session)
- [x] repl-18-psreadline-keybindings.cs
- [x] repl-19-tab-cycling-bug.cs
- [x] repl-20-double-tab-bug.cs
- [x] repl-21-escape-clears-line.cs
- [x] repl-22-prompt-display-no-arrow-history.cs
- [x] repl-23-key-binding-profiles.cs
- [x] repl-24-custom-key-bindings.cs
- [x] repl-25-interactive-history-search.cs
- [x] repl-26-kill-ring.cs
- [x] repl-27-undo-redo.cs
- [x] repl-28-text-selection.cs
- [x] repl-29-word-operations.cs
- [x] repl-30-basic-editing-enhancement.cs
- [x] repl-31-multiline-buffer.cs
- [x] repl-32-multiline-editing.cs
- [x] repl-33-yank-arguments.cs
- [x] repl-34-interactive-route-alias.cs
- [x] repl-35-interactive-route-execution.cs

## Already Migrated (from task 168)

- [x] repl-16-enum-completion.cs
- [x] repl-17-sample-validation.cs

## Results

**Final CI Test Count: 527 tests**
- Passed: 517
- Failed: 8 (pre-existing failures, not related to migration)
  - 4 from KeyBindingProfile (help command assertion issues)
  - 4 from InteractiveRouteExecution (exit code issues)
- Skipped: 2 (flaky tests marked with [Skip] attribute)

All 34 files were successfully migrated to support Jaribu multi-mode.

## Implementation Pattern

For each file:

1. Replace entry point with `#if !JARIBU_MULTI` block
2. Add `[ModuleInitializer]` registration method
3. Wrap types in a namespace block (use unique namespace per file to avoid collisions)
4. Remove `[ClearRunfileCache]` if present
5. Remove redundant `using TimeWarp.Nuru;` (already global)
6. Add file to `ci-tests/Directory.Build.props`
7. Verify standalone mode works
8. Verify multi-mode works

### Namespace Convention

Use `TimeWarp.Nuru.Tests.ReplTests.<TestClassName>` pattern:
- `TimeWarp.Nuru.Tests.ReplTests.SessionLifecycle`
- `TimeWarp.Nuru.Tests.ReplTests.CommandParsing`
- etc.

### Example Transformation

**Before:**
```csharp
#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

using TimeWarp.Nuru;

return await RunTests<TestClassName>();

[TestTag("REPL")]
[ClearRunfileCache]
public class TestClassName
{
  // ...
}
```

**After:**
```csharp
#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.TestClassName
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

## Verification Commands

**Standalone:**
```bash
dotnet tests/timewarp-nuru-repl-tests/repl-XX-name.cs
```

**Multi-mode:**
```bash
dotnet tests/ci-tests/run-ci-tests.cs
```

## Notes

- Infrastructure already set up from task 168 (InternalsVisibleTo, project references, warning suppressions)
- Block namespaces required to avoid class name collisions in multi-mode
- Some tests had pre-existing failures unrelated to the migration
- Fixed namespace collision for `KillRing` type by using `KillRingTests` namespace instead
- Fixed `Environment.NewLine` reference by using fully qualified `System.Environment.NewLine`
