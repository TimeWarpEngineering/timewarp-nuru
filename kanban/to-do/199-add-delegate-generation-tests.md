# Add Delegate Generation Tests

## Description

Add comprehensive test coverage for the `NuruDelegateCommandGenerator` to verify Command and Handler classes are generated correctly.

## Parent

151-implement-delegate-generation-phase-2

## Dependencies

- Task 193-196: Generator implementation must be complete

## Checklist

### Test File Setup
- [ ] Create `tests/timewarp-nuru-analyzers-tests/auto/delegate-command-generator-01-basic.cs`
- [ ] Create `tests/timewarp-nuru-analyzers-tests/auto/delegate-command-generator-02-source.cs`
- [ ] Use existing test patterns from `attributed-route-generator-*.cs`

### Basic Detection Tests
- [ ] Test simple delegate `(string name) => ...` detected
- [ ] Test delegate with multiple parameters detected
- [ ] Test async delegate detected
- [ ] Test method group reference detected

### Command Generation Tests
- [ ] Test Command class name follows convention
- [ ] Test Command properties match route parameters
- [ ] Test DI parameters NOT included in Command
- [ ] Test correct interface: `ICommand<Unit>` for void
- [ ] Test correct interface: `ICommand<int>` for int return
- [ ] Test correct interface: `IQuery<T>` when `.AsQuery()` used

### Handler Generation Tests
- [ ] Test Handler class name follows convention
- [ ] Test DI constructor injection generated
- [ ] Test private readonly fields for DI params
- [ ] Test Handle method signature correct
- [ ] Test parameter rewriting: `name` → `request.Name`
- [ ] Test DI parameter rewriting: `logger` → `_logger`

### Async Tests
- [ ] Test async lambda generates async Handler
- [ ] Test `Task` return → `ValueTask<Unit>`
- [ ] Test `Task<T>` return → `ValueTask<T>`
- [ ] Test `ValueTask` return handled
- [ ] Test `ValueTask<T>` return handled

### MessageType Tests
- [ ] Test default is `MessageType.Command`
- [ ] Test `.AsQuery()` → `MessageType.Query` + `IQuery<T>`
- [ ] Test `.AsCommand()` → `MessageType.Command`
- [ ] Test `.AsIdempotentCommand()` → `MessageType.IdempotentCommand` + `IIdempotent`

### Error Handling Tests
- [ ] Test closure detection emits error
- [ ] Test malformed pattern emits warning
- [ ] Test unsupported scenarios emit diagnostics

### Route Registration Tests
- [ ] Test `CompiledRouteBuilder` calls emitted
- [ ] Test `NuruRouteRegistry.Register<T>()` in ModuleInitializer
- [ ] Test route pattern string preserved for help

### Integration Tests
- [ ] Test full pipeline execution with generated Command/Handler
- [ ] Test Mediator dispatches to generated Handler
- [ ] Test DI injection works at runtime

## Example Test Cases

```csharp
// Test: Simple string parameter
[Fact]
public void GeneratesCommand_ForSimpleStringParameter()
{
    var source = @"
        app.Map(""greet {name}"")
            .WithHandler((string name) => Console.WriteLine(name))
            .AsCommand()
            .Done();
    ";
    
    // Verify generates:
    // - Greet_Generated_Command with Name property
    // - Greet_Generated_CommandHandler
}

// Test: DI parameter not in Command
[Fact]
public void ExcludesDIParameters_FromCommand()
{
    var source = @"
        app.Map(""log {message}"")
            .WithHandler((string message, ILogger logger) => logger.Log(message))
            .AsCommand()
            .Done();
    ";
    
    // Verify Command has Message but NOT Logger
    // Verify Handler has _logger field and constructor injection
}

// Test: Closure detection
[Fact]
public void EmitsError_ForClosure()
{
    var source = @"
        string prefix = ""hello"";
        app.Map(""greet"")
            .WithHandler(() => Console.WriteLine(prefix))  // Captures prefix
            .AsCommand()
            .Done();
    ";
    
    // Verify diagnostic error emitted
}
```

## Notes

Follow existing test patterns in `timewarp-nuru-analyzers-tests` for consistency.
