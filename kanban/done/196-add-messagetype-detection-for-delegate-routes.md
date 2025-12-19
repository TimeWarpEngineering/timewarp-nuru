# Add MessageType Detection for Delegate Routes

## Description

Extend `NuruDelegateCommandGenerator` to detect `.AsQuery()`, `.AsCommand()`, and `.AsIdempotentCommand()` calls and generate appropriate interfaces and MessageType.

## Parent

151-implement-delegate-generation-phase-2

## Dependencies

- Task 194: Command generation must be complete ✅
- Task 195: Handler generation must be complete ✅

## Checklist

### Unify Trigger Detection
- [x] Update `IsAsCommandInvocation()` predicate to match all three methods
- [x] Rename to `IsMessageTypeInvocation()`
- [x] Match: `AsCommand`, `AsIdempotentCommand`, `AsQuery`

### Track Message Type in Extracted Info
- [x] Add `GeneratedMessageType` enum to generator
- [x] Add `MessageType` field to `DelegateCommandInfo` record
- [x] Extract which method was called during chain walking
- [x] Store as enum: `Command`, `IdempotentCommand`, `Query`

### Interface Selection
- [x] `.AsCommand()` → `ICommand<TResult>` + `ICommandHandler<TCommand, TResult>`
- [x] `.AsIdempotentCommand()` → `ICommand<TResult>, IIdempotent` + `ICommandHandler<...>`
- [x] `.AsQuery()` → `IQuery<TResult>` + `IQueryHandler<TQuery, TResult>`

### Class Naming
- [x] `.AsCommand()` → `{Name}_Generated_Command`
- [x] `.AsIdempotentCommand()` → `{Name}_Generated_Command` (same, just adds interface)
- [x] `.AsQuery()` → `{Name}_Generated_Query`

### Handler Interface Selection
- [x] `.AsCommand()` / `.AsIdempotentCommand()` → `ICommandHandler<TCommand, TResult>`
- [x] `.AsQuery()` → `IQueryHandler<TQuery, TResult>`

### Route Registration (if applicable)
- [ ] Include `MessageType` in any route registration code (deferred - not part of current generator)

## Results

**2025-12-19:** Implementation complete.

All three message type methods now generate appropriate code:

| Method | Class Suffix | Interface | Handler Interface |
|--------|--------------|-----------|-------------------|
| `AsCommand()` | `_Generated_Command` | `ICommand<T>` | `ICommandHandler<T, TResult>` |
| `AsIdempotentCommand()` | `_Generated_Command` | `ICommand<T>, IIdempotent` | `ICommandHandler<T, TResult>` |
| `AsQuery()` | `_Generated_Query` | `IQuery<T>` | `IQueryHandler<T, TResult>` |

**Tests:** All 1673 CI tests pass.

**Example generated code for AsQuery():**
```csharp
[global::System.CodeDom.Compiler.GeneratedCode("TimeWarp.Nuru.Analyzers", "1.0.0")]
public sealed class Status_Generated_Query : global::Mediator.IQuery<string>
{
  public sealed class Handler : global::Mediator.IQueryHandler<Status_Generated_Query, string>
  {
    public global::System.Threading.Tasks.ValueTask<string> Handle(
      Status_Generated_Query request, 
      global::System.Threading.CancellationToken cancellationToken)
    {
      return new global::System.Threading.Tasks.ValueTask<string>("OK");
    }
  }
}
```

## Notes

### MessageType Importance

MessageType is used by AI agents to understand command behavior:
- **Query** - Safe to run freely, can retry, no side effects
- **Command** - Confirm before running, don't auto-retry
- **IdempotentCommand** - Safe to retry on failure

### Benefits of Common Generator

Single generator handles all three cases:
- Consistent behavior across all message types
- Full Mediator support for all route types
- Only difference is the interface type - logic is shared
- Easier to maintain than separate generators
