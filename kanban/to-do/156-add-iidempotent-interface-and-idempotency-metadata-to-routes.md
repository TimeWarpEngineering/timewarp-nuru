# Add IIdempotent Interface and Idempotency Metadata to Routes

## Description

Add idempotency metadata to Nuru routes to enable AI agents to make informed decisions about command execution safety. This allows AI tools to know which commands are safe to run freely (read-only), safe to retry (idempotent), or require caution (non-idempotent).

## Parent

148-nuru-3-unified-route-pipeline

## Background

AI agents executing CLI commands need to know:
- "Can I run this to gather information, or will it change something?"
- "If this fails, can I safely retry?"
- "Should I ask the user before running this?"

See: `.agent/workspace/2024-12-15T14-30-00_unified-contracts-and-idempotency-analysis.md`

## Requirements

### Three Idempotency Levels

| Level | AI Behavior | Examples |
|-------|-------------|----------|
| **ReadOnly** | Run freely, retry safely | `list`, `get`, `status`, `show` |
| **Idempotent** | Safe to retry on failure | `set`, `enable`, `disable`, `upsert` |
| **NonIdempotent** | Confirm before running, don't auto-retry | `create`, `append`, `send` |

### Default Behavior

Non-idempotent as default (safe choice - AI will be cautious).

## Checklist

### Design
- [ ] Create `Idempotency` enum (`ReadOnly`, `Idempotent`, `NonIdempotent`)
- [ ] Create `IIdempotent` marker interface (use `IQuery<T>` and `ICommand<T>` from Mediator)
- [ ] Add `Idempotency` property to `CompiledRoute`
- [ ] Add `WithIdempotency()` to `CompiledRouteBuilder`

### Implementation - Fluent API
- [ ] Add `.ReadOnly()` method to `IRouteBuilder`
- [ ] Add `.Idempotent()` method to `IRouteBuilder`
- [ ] Add `.NonIdempotent()` method to `IRouteBuilder` (explicit default)

### Implementation - Type System Derivation
- [ ] Source generator detects `IQuery<T>` → sets `Idempotency.ReadOnly`
- [ ] Source generator detects `ICommand<T>` → sets `Idempotency.NonIdempotent`
- [ ] Source generator detects `ICommand<T>, IIdempotent` → sets `Idempotency.Idempotent`

### Implementation - Help Output
- [ ] Display idempotency indicator in `--help` output: `(R)`, `(I)`, `(W)`
- [ ] Add legend to help footer

### Testing
- [ ] Unit tests for `Idempotency` enum
- [ ] Unit tests for fluent API methods
- [ ] Unit tests for source generator derivation
- [ ] Integration tests for help output

## Notes

### Fluent API Usage

```csharp
app.Map("users list", handler).ReadOnly();
app.Map("config set {key} {value}", handler).Idempotent();
app.Map("user create {name}", handler);  // Non-idempotent by default
```

### Attributed Routes Usage

```csharp
// ReadOnly - derived from IQuery<T>
[NuruRoute("users list")]
public sealed class ListUsersRequest : IQuery<Response> { }

// Idempotent - derived from ICommand<T> + IIdempotent
[NuruRoute("config set")]
public sealed class SetConfigRequest : ICommand<Response>, IIdempotent { }

// Non-idempotent - derived from ICommand<T> alone
[NuruRoute("user create")]
public sealed class CreateUserRequest : ICommand<Response> { }
```

### Help Output

```bash
$ mytool --help

Commands:
  users list          (R)  List all users
  config set          (I)  Set configuration value
  user create         (W)  Create a new user

Legend: (R) Read-only  (I) Idempotent  (W) Non-idempotent write
```

### CompiledRouteBuilder Extension

```csharp
public CompiledRouteBuilder WithIdempotency(Idempotency idempotency)
{
    _idempotency = idempotency;
    return this;
}
```
