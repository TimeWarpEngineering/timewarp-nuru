# Migrate tab-completion tests to Jaribu multi-mode

## Description

Migrate the 8 test files and 1 helper file in `tests/timewarp-nuru-repl-tests/tab-completion/` to support Jaribu multi-mode pattern, enabling them to run in the consolidated CI test runner.

## Files to Migrate

### Helper File (migrate first)
- [ ] `completion-test-helpers.cs` - Contains `CompletionAssertions`, `KeySequenceHelpers`, `TestAppFactory`, and `Environment` enum

### Test Files
- [ ] `repl-20-tab-basic-commands.cs`
- [ ] `repl-21-tab-subcommands.cs`
- [ ] `repl-22-tab-enums.cs`
- [ ] `repl-23-tab-options.cs`
- [ ] `repl-24-tab-cycling.cs`
- [ ] `repl-25-tab-state-management.cs`
- [ ] `repl-26-tab-edge-cases.cs`
- [ ] `repl-27-tab-help-option.cs`

## Checklist

- [ ] Migrate `completion-test-helpers.cs`:
  - Already has namespace `TimeWarp.Nuru.Tests.TabCompletion`
  - Add to `ci-tests/Directory.Build.props`
  - Note: Contains `Environment` enum - may conflict with `System.Environment`
  
- [ ] For each test file:
  - Add `#if !JARIBU_MULTI` / `return await RunAllTests();` / `#endif` block
  - Wrap types in namespace block (e.g., `TimeWarp.Nuru.Tests.TabCompletion.BasicCommands`)
  - Add `[ModuleInitializer]` registration method
  - Remove `[ClearRunfileCache]` attribute
  - Remove `using TimeWarp.Nuru;` (already global)
  - Keep `using TimeWarp.Nuru.Tests.TabCompletion;` for helper access
  
- [ ] Add all files to `tests/ci-tests/Directory.Build.props`
- [ ] Test standalone mode for each file
- [ ] Test multi-mode: `dotnet tests/ci-tests/run-ci-tests.cs`
- [ ] Commit changes

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
