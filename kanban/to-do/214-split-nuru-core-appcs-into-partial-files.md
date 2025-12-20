# Split nuru-core-app.cs into partial files

## Description

The `nuru-core-app.cs` file (709 lines) is the main application class handling factory methods, execution, parameter binding, and validation. These distinct responsibilities should be organized into partial files for better maintainability.

**Location:** `source/timewarp-nuru-core/nuru-core-app.cs`

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

### File Creation
- [ ] Create `nuru-core-app.execution.cs` - Mediator and delegate execution
- [ ] Create `nuru-core-app.binding.cs` - Parameter binding and conversion
- [ ] Create `nuru-core-app.validation.cs` - Configuration validation and help

### Documentation
- [ ] Add `<remarks>` to main file listing all partial files
- [ ] Add XML summary to each new partial file
- [ ] Keep existing `#region Static Factory Methods` or convert to partial

### Verification
- [ ] All tests pass
- [ ] Build succeeds
- [ ] No breaking API changes

## Notes

### Proposed Split

| New File | ~Lines | Content |
|----------|--------|---------|
| `.execution.cs` | ~200 | `ExecuteMediatorCommandAsync()`, `ExecuteDelegateAsync()`, `ExecuteDelegateWithPipelineAsync()` |
| `.binding.cs` | ~150 | `BindDelegateParameters()`, `BindParameters()`, `ConvertParameter()`, `IsOptionalParameter()`, `IsServiceParameter()` |
| `.validation.cs` | ~80 | `ValidateConfigurationAsync()`, `ShouldSkipValidation()`, `FilterConfigurationArgs()`, `DisplayValidationErrorsAsync()`, `ShowAvailableCommands()` |
| Main file | ~280 | Factory methods, constructors, properties, `RunAsync()` orchestration |

### Dependencies to Consider

- `ServiceProvider`, `MediatorExecutor`, `Terminal`, `TypeConverterRegistry` fields are used across execution and binding
- `RunAsync()` orchestrates multiple concerns - keep in main file
- Suppression attributes must stay with their methods

### Current Organization

```
nuru-core-app.cs (709 lines)
├── #region Static Factory Methods (lines 12-88)
├── Properties and constructors (lines 90-174)
├── RunAsync orchestration (lines 176-284)
├── Mediator execution (lines 286-316)
├── Delegate execution (lines 318-404)
├── Parameter binding (lines 406-563)
├── Configuration validation (lines 593-666)
└── Help and utilities (lines 586-716)
```

### Reference Pattern

Follow `endpoint-resolver.cs` partial organization with clear XML documentation.
