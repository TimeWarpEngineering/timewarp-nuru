# Split nuru-core-app.cs into partial files

## Description

The `nuru-core-app.cs` file (709 lines) is the main application class handling factory methods, execution, parameter binding, and validation. These distinct responsibilities should be organized into partial files for better maintainability.

**Location:** `source/timewarp-nuru-core/nuru-core-app.cs`

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

### File Creation
- [x] Create `nuru-core-app.execution.cs` - Mediator and delegate execution
- [x] Create `nuru-core-app.binding.cs` - Parameter binding and conversion
- [x] Create `nuru-core-app.validation.cs` - Configuration validation and help

### Documentation
- [x] Add `<remarks>` to main file listing all partial files
- [x] Add XML summary to each new partial file
- [x] Keep existing `#region Static Factory Methods` or convert to partial

### Verification
- [x] All tests pass
- [x] Build succeeds
- [x] No breaking API changes

## Notes

### Actual Split (Completed)

| New File | ~Lines | Content |
|----------|--------|---------|
| `.execution.cs` | ~120 | `ExecuteMediatorCommandAsync()`, `ExecuteDelegateAsync()`, `ExecuteDelegateWithPipelineAsync()` |
| `.binding.cs` | ~175 | `BindDelegateParameters()`, `BindParameters()`, `ConvertParameter()`, `IsOptionalParameter()`, `IsServiceParameter()` |
| `.validation.cs` | ~120 | `ValidateConfigurationAsync()`, `ShouldSkipValidation()`, `FilterConfigurationArgs()`, `DisplayValidationErrorsAsync()`, `ShowAvailableCommands()`, `GetDefaultAppName()`, `GetEffectiveAppName()`, `GetEffectiveDescription()` |
| Main file | ~280 | Factory methods, constructors, properties, `RunAsync()` orchestration |

### Dependencies to Consider

- `ServiceProvider`, `MediatorExecutor`, `Terminal`, `TypeConverterRegistry` fields are used across execution and binding
- `RunAsync()` orchestrates multiple concerns - keep in main file
- Suppression attributes must stay with their methods

### Current Organization

```
nuru-core-app.cs (718 lines -> ~280 lines)
├── <remarks> with partial file list
├── #region Static Factory Methods (lines 12-88)
├── #region Properties (lines 90-128)
├── #region Constructors (lines 130-174)
├── #region Run Orchestration (lines 176-272)
└── ConfigurationOverrideRegex partial method
```

### Reference Pattern

Followed `nuru-core-app-builder.*.cs` partial organization with clear XML documentation.
