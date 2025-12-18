# Migrate tab-completion tests to Jaribu multi-mode

## Description

Migrate the 8 test files and 1 helper file in `tests/timewarp-nuru-repl-tests/tab-completion/` to support Jaribu multi-mode pattern, enabling them to run in the consolidated CI test runner.

## Files to Migrate

### Helper File (migrate first)
- [x] `completion-test-helpers.cs` - Contains `CompletionAssertions`, `KeySequenceHelpers`, `TestAppFactory`, and `Environment` enum

### Test Files
- [x] `repl-20-tab-basic-commands.cs`
- [x] `repl-21-tab-subcommands.cs`
- [x] `repl-22-tab-enums.cs`
- [x] `repl-23-tab-options.cs`
- [x] `repl-24-tab-cycling.cs`
- [x] `repl-25-tab-state-management.cs`
- [x] `repl-26-tab-edge-cases.cs`
- [x] `repl-27-tab-help-option.cs`

## Checklist

- [x] Migrate `completion-test-helpers.cs`:
  - Already has namespace `TimeWarp.Nuru.Tests.TabCompletion`
  - Add to `ci-tests/Directory.Build.props`
  - Note: Contains `Environment` enum - may conflict with `System.Environment`
  
- [x] For each test file:
  - Add `#if !JARIBU_MULTI` / `return await RunAllTests();` / `#endif` block
  - Wrap types in namespace block (e.g., `TimeWarp.Nuru.Tests.TabCompletion.BasicCommands`)
  - Add `[ModuleInitializer]` registration method
  - Remove `[ClearRunfileCache]` attribute
  - Remove `using TimeWarp.Nuru;` (already global)
  - Keep `using TimeWarp.Nuru.Tests.TabCompletion;` for helper access
  
- [x] Add all files to `tests/ci-tests/Directory.Build.props`
- [x] Test standalone mode for each file
- [x] Test multi-mode: `dotnet tests/ci-tests/run-ci-tests.cs`
- [x] Commit changes

## Special Considerations

### Helper File Structure
The helper file `completion-test-helpers.cs` already has:
- File-scoped namespace: `namespace TimeWarp.Nuru.Tests.TabCompletion;`
- Extension methods: `CompletionAssertions`, `KeySequenceHelpers`
- Factory: `TestAppFactory.CreateReplDemoApp()`
- Enum: `Environment` (Dev, Staging, Prod)

For multi-mode, this file needs to be included **before** the test files since it defines shared types.

### Environment Enum Conflict
The helper defines `Environment` enum which may conflict with `System.Environment`. Tests use `Environment` (the enum) so ensure proper namespace resolution.

### Test Pattern
Each test file uses Setup/CleanUp pattern with static fields:
```csharp
private static TestTerminal Terminal = null!;
private static NuruCoreApp App = null!;

public static async Task Setup()
{
  Terminal = new TestTerminal();
  App = TestAppFactory.CreateReplDemoApp(Terminal);
  await Task.CompletedTask;
}

public static async Task CleanUp()
{
  Terminal?.Dispose();
  Terminal = null!;
  App = null!;
  await Task.CompletedTask;
}
```

## Notes

- Current CI test count: 546 tests
- These tests reference both `timewarp-nuru.csproj` and `timewarp-nuru-repl.csproj`
- The helper file already has a namespace, so it mainly needs to be added to Directory.Build.props
- Test files need full migration pattern (namespace wrapping, ModuleInitializer, etc.)

## Results

**Completed:** All 8 test files and 1 helper file migrated to Jaribu multi-mode.

### Test Counts Added
| File | Tests |
|------|-------|
| repl-20-tab-basic-commands.cs | 19 |
| repl-21-tab-subcommands.cs | 12 |
| repl-22-tab-enums.cs | 17 |
| repl-23-tab-options.cs | 23 |
| repl-24-tab-cycling.cs | 14 |
| repl-25-tab-state-management.cs | 15 |
| repl-26-tab-edge-cases.cs | 21 |
| repl-27-tab-help-option.cs | 12 |
| **Total New Tests** | **133** |

### CI Test Count
- **Before:** 546 tests
- **After:** 679 tests (+133 from tab-completion)

### Namespaces Used
- `TimeWarp.Nuru.Tests.TabCompletion` (helper file)
- `TimeWarp.Nuru.Tests.TabCompletion.BasicCommands`
- `TimeWarp.Nuru.Tests.TabCompletion.Subcommands`
- `TimeWarp.Nuru.Tests.TabCompletion.Enums`
- `TimeWarp.Nuru.Tests.TabCompletion.Options`
- `TimeWarp.Nuru.Tests.TabCompletion.Cycling`
- `TimeWarp.Nuru.Tests.TabCompletion.StateManagement`
- `TimeWarp.Nuru.Tests.TabCompletion.EdgeCases`
- `TimeWarp.Nuru.Tests.TabCompletion.HelpOption`

All tab-completion tests pass in both standalone and multi-mode.
