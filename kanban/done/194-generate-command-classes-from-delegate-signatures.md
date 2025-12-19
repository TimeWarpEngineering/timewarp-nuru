# Generate Command Classes from Delegate Signatures

## Description

Extend `NuruDelegateCommandGenerator` to emit Command classes from delegate signatures. Commands include only route parameters (not DI parameters).

## Parent

151-implement-delegate-generation-phase-2

## Dependencies

- Task 193: Generator detection must be complete ✅

## Checklist

### Command Class Generation
- [x] Generate `sealed class {Prefix}_Generated_Command`
- [x] Apply `[GeneratedCode("TimeWarp.Nuru.Analyzers", "1.0.0")]` attribute
- [x] Generate as class with properties (NOT record, NO primary constructor)

### Properties
- [x] Generate property for each route parameter
- [x] Use correct types from delegate signature
- [x] Apply appropriate default values:
  - `string` → `= string.Empty`
  - `bool` → `= false` (implicit)
  - `int` → `= 0` (implicit)
  - Nullable types → `= null` (implicit)
- [x] Use PascalCase for property names (convert from camelCase)

### Interface Implementation
- [x] Default: implement `ICommand<TResult>`
- [x] `void` delegate → `ICommand<Unit>`
- [x] `T` delegate → `ICommand<T>`
- [x] `Task` delegate → `ICommand<Unit>`
- [x] `Task<T>` delegate → `ICommand<T>`
- [ ] Note: MessageType-specific interfaces handled in Task 196

### Naming Convention
- [x] Extract prefix from first literal in route pattern
- [x] Convert to PascalCase
- [x] Examples:
  - `"deploy {env}"` → `Deploy_Generated_Command`
  - `"git commit"` → `GitCommit_Generated_Command`
  - `""` (default route) → `Default_Generated_Command`

## Implementation

Created `NuruDelegateCommandGenerator` source generator:
- Detects `AsCommand()` invocations in fluent API
- Walks back syntax tree to find `WithHandler()` and `Map()` calls
- Parses route pattern to identify route parameters vs DI parameters
- Generates sealed Command classes with properties for route parameters only

### Files Created/Modified
- `source/timewarp-nuru-analyzers/analyzers/nuru-delegate-command-generator.cs` - New generator
- `tests/timewarp-nuru-analyzers-tests/auto/delegate-command-generator-01-basic.cs` - Tests

### Test Results
All 9 tests passing:
- Should_run_generator_without_errors
- Should_generate_command_class
- Should_generate_command_with_options
- Should_exclude_di_parameters (DI parameters like ILogger not included)
- Should_generate_multiword_command_name (git commit → GitCommit_Generated_Command)
- Should_generate_default_command_name (empty pattern → Default_Generated_Command)
- Should_use_return_type_for_command_interface (void→Unit, int→int)
- Should_handle_nullable_parameters (string? → nullable property)
- Should_not_generate_for_asquery (only AsCommand generates, not AsQuery)

## Example Output

**Input:**
```csharp
app.Map("deploy {env} --force")
    .WithHandler((string env, bool force, ILogger logger) => { ... })
    .AsCommand()
    .Done();
```

**Generated:**
```csharp
[global::System.CodeDom.Compiler.GeneratedCode("TimeWarp.Nuru.Analyzers", "1.0.0")]
public sealed class Deploy_Generated_Command : global::Mediator.ICommand<global::Mediator.Unit>
{
    public string Env { get; set; } = string.Empty;
    public bool Force { get; set; }
}
```

## Notes

- Only route parameters become properties
- DI parameters (like `ILogger`) are NOT included - they go in the Handler
- Keep generated code fully qualified to avoid namespace conflicts
- Generator only triggers on `AsCommand()` calls, not `AsQuery()` or other message types
