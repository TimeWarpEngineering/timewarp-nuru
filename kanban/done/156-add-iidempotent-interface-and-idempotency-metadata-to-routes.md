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
- [x] Create `MessageType` enum (`Query`, `Command`, `IdempotentCommand`)
- [x] Create `IIdempotent` marker interface (use `IQuery<T>` and `ICommand<T>` from Mediator)
- [x] Add `MessageType` property to `CompiledRoute`
- [x] Add `WithMessageType()` to `CompiledRouteBuilder`

### Implementation - Fluent API
- [x] Add `.AsQuery()` method via `RouteConfigurator`
- [x] Add `.AsCommand()` method via `RouteConfigurator` (explicit default)
- [x] Add `.AsIdempotentCommand()` method via `RouteConfigurator`

### Implementation - Type System Derivation
- [x] **Moved to Task 151** - Source generator detection of `IQuery<T>`, `ICommand<T>`, and `IIdempotent` interfaces belongs in the delegate generation phase where source generator infrastructure lives

### Implementation - Help Output
- [x] Display message type indicator in `--help` output: `(Q)`, `(C)`, `(I)`
- [x] Add legend to help footer

### Testing
- [x] Unit tests for `MessageType` enum
- [x] Unit tests for fluent API methods
- [x] **Moved to Task 151** - Unit tests for source generator derivation
- [x] Integration tests for help output

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

## Results

### Implementation Complete

**Files Created:**
- `source/timewarp-nuru-parsing/message-type.cs` - MessageType enum
- `source/timewarp-nuru-core/iidempotent.cs` - IIdempotent marker interface
- `source/timewarp-nuru-core/route-configurator.cs` - Fluent API configurator
- `tests/timewarp-nuru-core-tests/message-type-01-fluent-api.cs` - 7 passing tests
- `tests/timewarp-nuru-core-tests/message-type-02-help-output.cs` - 6 passing tests

**Files Modified:**
- `compiled-route-builder.cs` - Added `WithMessageType()` method
- `compiled-route.cs` - Added `MessageType` property (defaults to `Command`)
- `endpoint.cs` - Added `MessageType` property
- `help-provider.cs` - Added indicators `(Q)`, `(C)`, `(I)` with color coding and legend
- `nuru-core-app-builder.routes.cs` - `Map()` returns `RouteConfigurator`
- `nuru-app-builder.overrides.cs` - Removed obsolete covariant override methods

**Key Design Decisions:**
- `RouteConfigurator` enables fluent chaining: `app.Map(...).AsQuery()`
- Implicit conversion from `RouteConfigurator` to `NuruCoreAppBuilder` maintains backward compatibility
- Default is `Command` (safest choice for AI agents)
- Help output: Query=Blue `(Q)`, IdempotentCommand=Yellow `(I)`, Command=Red `(C)`

**Source generator work split between tasks:**
- **Task 150 (Attributed Routes):** Generator reads `IQuery<T>`, `ICommand<T>`, `IIdempotent` from user-written request classes and emits `WithMessageType()` call
- **Task 151 (Delegate Routes):** Generator decides MessageType based on fluent `.AsQuery()` / `.AsIdempotentCommand()` calls, generates appropriate interfaces on Command class
