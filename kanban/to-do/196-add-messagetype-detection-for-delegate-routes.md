# Add MessageType Detection for Delegate Routes

## Description

Extend `NuruDelegateCommandGenerator` to detect `.AsQuery()`, `.AsCommand()`, and `.AsIdempotentCommand()` calls and generate appropriate interfaces and MessageType.

## Parent

151-implement-delegate-generation-phase-2

## Dependencies

- Task 194: Command generation must be complete
- Task 195: Handler generation must be complete

## Checklist

### Fluent Chain Detection
- [ ] Detect `.AsQuery()` after `.WithHandler()`
- [ ] Detect `.AsCommand()` after `.WithHandler()`
- [ ] Detect `.AsIdempotentCommand()` after `.WithHandler()`
- [ ] Default to Command if none specified (safest for AI agents)

### Interface Selection
- [ ] `.AsQuery()` → `IQuery<TResult>` + `IQueryHandler<TQuery, TResult>`
- [ ] `.AsCommand()` → `ICommand<TResult>` + `ICommandHandler<TCommand, TResult>`
- [ ] `.AsIdempotentCommand()` → `ICommand<TResult>, IIdempotent` + `ICommandHandler<...>`
- [ ] Update Task 194/195 generated code accordingly

### Route Registration
- [ ] Emit `WithMessageType(MessageType.Query)` for queries
- [ ] Emit `WithMessageType(MessageType.Command)` for commands
- [ ] Emit `WithMessageType(MessageType.IdempotentCommand)` for idempotent

### Syntax Tree Walking
- [ ] From `WithHandler()` invocation, walk up to find sibling calls
- [ ] Look for `AsQuery`, `AsCommand`, `AsIdempotentCommand` method calls
- [ ] Handle method chaining correctly

## Example

**Input:**
```csharp
app.Map("status")
    .WithHandler(() => "OK")
    .AsQuery()
    .Done();
```

**Generated:**
```csharp
// Query interface instead of Command
public sealed class Status_Generated_Query : global::Mediator.IQuery<string>
{
}

public sealed class Status_Generated_QueryHandler 
    : global::Mediator.IQueryHandler<Status_Generated_Query, string>
{
    public global::System.Threading.Tasks.ValueTask<string> Handle(
        Status_Generated_Query request, 
        global::System.Threading.CancellationToken ct)
    {
        return global::System.Threading.Tasks.ValueTask.FromResult("OK");
    }
}

// Registration includes MessageType
NuruRouteRegistry.Register<Status_Generated_Query>(
    new CompiledRouteBuilder()
        .WithLiteral("status")
        .WithMessageType(MessageType.Query)
        .Build(),
    "status");
```

## Notes

### MessageType Importance

MessageType is used by AI agents to understand command behavior:
- **Query** - Safe to run freely, can retry, no side effects
- **Command** - Confirm before running, don't auto-retry
- **IdempotentCommand** - Safe to retry on failure

Defaulting to Command is safest - prevents AI from accidentally running destructive operations.
