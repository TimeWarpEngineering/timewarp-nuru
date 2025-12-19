# Generate Command Classes from Delegate Signatures

## Description

Extend `NuruDelegateCommandGenerator` to emit Command classes from delegate signatures. Commands include only route parameters (not DI parameters).

## Parent

151-implement-delegate-generation-phase-2

## Dependencies

- Task 193: Generator detection must be complete

## Checklist

### Command Class Generation
- [ ] Generate `sealed class {Prefix}_Generated_Command`
- [ ] Apply `[GeneratedCode("TimeWarp.Nuru.Analyzers", "1.0.0")]` attribute
- [ ] Generate as class with properties (NOT record, NO primary constructor)

### Properties
- [ ] Generate property for each route parameter
- [ ] Use correct types from delegate signature
- [ ] Apply appropriate default values:
  - `string` → `= string.Empty`
  - `bool` → `= false` (implicit)
  - `int` → `= 0` (implicit)
  - Nullable types → `= null` (implicit)
- [ ] Use PascalCase for property names (convert from camelCase)

### Interface Implementation
- [ ] Default: implement `ICommand<TResult>`
- [ ] `void` delegate → `ICommand<Unit>`
- [ ] `T` delegate → `ICommand<T>`
- [ ] `Task` delegate → `ICommand<Unit>`
- [ ] `Task<T>` delegate → `ICommand<T>`
- [ ] Note: MessageType-specific interfaces handled in Task 196

### Naming Convention
- [ ] Extract prefix from first literal in route pattern
- [ ] Convert to PascalCase
- [ ] Examples:
  - `"deploy {env}"` → `Deploy_Generated_Command`
  - `"git commit"` → `GitCommit_Generated_Command`
  - `""` (default route) → `Default_Generated_Command`

## Example Output

**Input:**
```csharp
app.Map("deploy {env} --force")
    .WithHandler((string env, bool force, ILogger logger) => { ... })
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
