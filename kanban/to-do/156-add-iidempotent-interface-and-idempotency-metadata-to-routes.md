# Add IIdempotent Interface and MessageType Metadata to Routes

## Description

Add message type metadata to Nuru routes to enable AI agents to make informed decisions about command execution safety. This uses established CQRS terminology (Query, Command, IdempotentCommand) that matches the Mediator interfaces.

## Parent

148-nuru-3-unified-route-pipeline

## Background

AI agents executing CLI commands need to know:
- "Can I run this to gather information, or will it change something?"
- "If this fails, can I safely retry?"
- "Should I ask the user before running this?"

See: `.agent/workspace/2024-12-15T14-30-00_unified-contracts-and-idempotency-analysis.md`

## Requirements

### Three Message Types (CQRS Terminology)

| MessageType | AI Behavior | Examples |
|-------------|-------------|----------|
| **Query** | Run freely, retry safely | `list`, `get`, `status`, `show` |
| **IdempotentCommand** | Safe to retry on failure | `set`, `enable`, `disable`, `upsert` |
| **Command** | Confirm before running, don't auto-retry | `create`, `append`, `send` |

### Default Behavior

Command as default (safe choice - AI will be cautious).

## Checklist

### Design
- [ ] Create `MessageType` enum (`Query`, `Command`, `IdempotentCommand`)
- [ ] Create `IIdempotent` marker interface (use `IQuery<T>` and `ICommand<T>` from Mediator)
- [ ] Add `MessageType` property to `CompiledRoute`
- [ ] Add `WithMessageType()` to `CompiledRouteBuilder`

### Implementation - Fluent API
- [ ] Add `.AsQuery()` method to `IRouteBuilder`
- [ ] Add `.AsCommand()` method to `IRouteBuilder` (explicit default)
- [ ] Add `.AsIdempotentCommand()` method to `IRouteBuilder`

### Implementation - Type System Derivation
- [ ] Source generator detects `IQuery<T>` → sets `MessageType.Query`
- [ ] Source generator detects `ICommand<T>` → sets `MessageType.Command`
- [ ] Source generator detects `ICommand<T>, IIdempotent` → sets `MessageType.IdempotentCommand`

### Implementation - Help Output
- [ ] Display message type indicator in `--help` output: `(Q)`, `(C)`, `(I)`
- [ ] Add legend to help footer

### Testing
- [ ] Unit tests for `MessageType` enum
- [ ] Unit tests for fluent API methods
- [ ] Unit tests for source generator derivation
- [ ] Integration tests for help output

## Notes

### Fluent API Usage

```csharp
app.Map("users list", handler).AsQuery();
app.Map("config set {key} {value}", handler).AsIdempotentCommand();
app.Map("user create {name}", handler);  // Command by default
```

### Attributed Routes Usage

```csharp
// Query - derived from IQuery<T>
[NuruRoute("users list")]
public sealed class ListUsersRequest : IQuery<Response> { }

// IdempotentCommand - derived from ICommand<T> + IIdempotent
[NuruRoute("config set")]
public sealed class SetConfigRequest : ICommand<Response>, IIdempotent { }

// Command - derived from ICommand<T> alone
[NuruRoute("user create")]
public sealed class CreateUserRequest : ICommand<Response> { }
```

### Help Output

```bash
$ mytool --help

Commands:
  users list          (Q)  List all users
  config set          (I)  Set configuration value
  user create         (C)  Create a new user

Legend: (Q) Query  (I) Idempotent  (C) Command
```

### CompiledRouteBuilder Extension

```csharp
public CompiledRouteBuilder WithMessageType(MessageType messageType)
{
    _messageType = messageType;
    return this;
}
```

### MessageType Enum

```csharp
public enum MessageType
{
    /// <summary>Query operation. No state change - safe to run and retry freely.</summary>
    Query,
    
    /// <summary>Command operation. State change, not repeatable - confirm before running.</summary>
    Command,
    
    /// <summary>Idempotent command. State change but repeatable - safe to retry on failure.</summary>
    IdempotentCommand
}
```
