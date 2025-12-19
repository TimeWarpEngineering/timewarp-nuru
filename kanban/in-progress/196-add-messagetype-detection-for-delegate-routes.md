# Add MessageType Detection for Delegate Routes

## Description

Extend `NuruDelegateCommandGenerator` to detect `.AsQuery()`, `.AsCommand()`, and `.AsIdempotentCommand()` calls and generate appropriate interfaces and MessageType.

**GAP IDENTIFIED (2025-12-19):** Currently, only `AsCommand()` triggers code generation. Both `AsIdempotentCommand()` and `AsQuery()` are valid API methods but produce NO generated code. See analysis document: `.agent/workspace/2025-12-19T22-30-00_route-definition-analysis.md`

## Parent

151-implement-delegate-generation-phase-2

## Dependencies

- Task 194: Command generation must be complete ✅
- Task 195: Handler generation must be complete ✅

## Checklist

### Unify Trigger Detection
- [ ] Update `IsAsCommandInvocation()` predicate to match all three methods
- [ ] Rename to `IsMessageTypeInvocation()` or similar
- [ ] Match: `AsCommand`, `AsIdempotentCommand`, `AsQuery`

### Track Message Type in Extracted Info
- [ ] Add `MessageType` field to `DelegateCommandInfo` record
- [ ] Extract which method was called during chain walking
- [ ] Store as enum: `Command`, `IdempotentCommand`, `Query`

### Interface Selection
- [ ] `.AsCommand()` → `ICommand<TResult>` + `ICommandHandler<TCommand, TResult>`
- [ ] `.AsIdempotentCommand()` → `ICommand<TResult>, IIdempotent` + `ICommandHandler<...>`
- [ ] `.AsQuery()` → `IQuery<TResult>` + `IQueryHandler<TQuery, TResult>`

### Class Naming
- [ ] `.AsCommand()` → `{Name}_Generated_Command`
- [ ] `.AsIdempotentCommand()` → `{Name}_Generated_Command` (same, just adds interface)
- [ ] `.AsQuery()` → `{Name}_Generated_Query`

### Handler Interface Selection
- [ ] `.AsCommand()` / `.AsIdempotentCommand()` → `ICommandHandler<TCommand, TResult>`
- [ ] `.AsQuery()` → `IQueryHandler<TQuery, TResult>`

### Route Registration (if applicable)
- [ ] Include `MessageType` in any route registration code
- [ ] Emit `WithMessageType(MessageType.Query)` for queries
- [ ] Emit `WithMessageType(MessageType.Command)` for commands  
- [ ] Emit `WithMessageType(MessageType.IdempotentCommand)` for idempotent

## Current State (GAP)

```csharp
// In NuruDelegateCommandGenerator - only looks for AsCommand()
private static bool IsAsCommandInvocation(Microsoft.CodeAnalysis.SyntaxNode node)
{
  // ...
  return memberAccess.Name.Identifier.Text == "AsCommand";  // ❌ Misses other two!
}
```

| Method | Sets MessageType | Generates Code |
|--------|------------------|----------------|
| `AsCommand()` | `MessageType.Command` | ✅ Yes |
| `AsIdempotentCommand()` | `MessageType.IdempotentCommand` | ❌ No (GAP) |
| `AsQuery()` | `MessageType.Query` | ❌ No (GAP) |

## Recommended Implementation

### Step 1: Update Predicate

```csharp
private static bool IsMessageTypeInvocation(Microsoft.CodeAnalysis.SyntaxNode node)
{
  if (node is not InvocationExpressionSyntax invocation)
    return false;

  if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
    return false;

  return memberAccess.Name.Identifier.Text is "AsCommand" or "AsIdempotentCommand" or "AsQuery";
}
```

### Step 2: Extract Message Type

```csharp
// In ExtractCommandInfo or chain walking
string methodName = memberAccess.Name.Identifier.Text;
MessageType messageType = methodName switch
{
  "AsQuery" => MessageType.Query,
  "AsIdempotentCommand" => MessageType.IdempotentCommand,
  _ => MessageType.Command
};
```

### Step 3: Generate Appropriate Interfaces

```csharp
string interfaceType = messageType switch
{
  MessageType.Query => $"global::Mediator.IQuery<{returnType}>",
  MessageType.IdempotentCommand => $"global::Mediator.ICommand<{returnType}>, global::TimeWarp.Nuru.IIdempotent",
  _ => $"global::Mediator.ICommand<{returnType}>"
};

string handlerInterface = messageType switch
{
  MessageType.Query => $"global::Mediator.IQueryHandler<{className}, {returnType}>",
  _ => $"global::Mediator.ICommandHandler<{className}, {returnType}>"
};
```

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
