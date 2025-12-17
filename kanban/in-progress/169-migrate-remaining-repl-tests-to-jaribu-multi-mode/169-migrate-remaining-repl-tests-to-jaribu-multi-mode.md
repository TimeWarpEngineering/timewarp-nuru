# Migrate remaining repl tests to Jaribu multi-mode

## Description

Migrate all remaining repl test files (repl-01 through repl-35, excluding already migrated repl-16 and repl-17) to support Jaribu multi-mode pattern, enabling them to run in the consolidated CI test runner.

## Files to Migrate (34 files)

- [ ] repl-01-session-lifecycle.cs
- [ ] repl-02-command-parsing.cs
- [ ] repl-03-history-management.cs
- [ ] repl-03b-history-security.cs
- [ ] repl-04-history-persistence.cs
- [ ] repl-05-console-input.cs
- [ ] repl-06-tab-completion-basic.cs
- [ ] repl-07-tab-completion-advanced.cs
- [ ] repl-08-syntax-highlighting.cs
- [ ] repl-09-builtin-commands.cs
- [ ] repl-10-error-handling.cs
- [ ] repl-11-display-formatting.cs
- [ ] repl-12-configuration.cs
- [ ] repl-13-nuruapp-integration.cs
- [ ] repl-14-performance.cs
- [ ] repl-15-edge-cases.cs
- [ ] repl-18-psreadline-keybindings.cs
- [ ] repl-19-tab-cycling-bug.cs
- [ ] repl-20-double-tab-bug.cs
- [ ] repl-21-escape-clears-line.cs
- [ ] repl-22-prompt-display-no-arrow-history.cs
- [ ] repl-23-key-binding-profiles.cs
- [ ] repl-24-custom-key-bindings.cs
- [ ] repl-25-interactive-history-search.cs
- [ ] repl-26-kill-ring.cs
- [ ] repl-27-undo-redo.cs
- [ ] repl-28-text-selection.cs
- [ ] repl-29-word-operations.cs
- [ ] repl-30-basic-editing-enhancement.cs
- [ ] repl-31-multiline-buffer.cs
- [ ] repl-32-multiline-editing.cs
- [ ] repl-33-yank-arguments.cs
- [ ] repl-34-interactive-route-alias.cs
- [ ] repl-35-interactive-route-execution.cs

## Already Migrated (from task 168)

- [x] repl-16-enum-completion.cs
- [x] repl-17-sample-validation.cs

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
- Consider batching files by related functionality for easier review
- May want to add `timewarp-nuru-repl` project reference to ci-tests if not already present
