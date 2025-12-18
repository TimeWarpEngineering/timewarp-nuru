# Add MessageType Detection to Attributed Route Generator

## Description

Update `NuruAttributedRouteGenerator` to detect `IQuery<T>`, `ICommand<T>`, and `IIdempotent` interfaces on attributed request classes and emit appropriate `WithMessageType()` calls.

Currently all attributed routes default to `MessageType.Command`. After this task, the generator will infer the correct message type from the interfaces.

## Parent

150-implement-attributed-routes-phase-1

## Checklist

### Add MessageType.Unspecified
- [ ] Add `Unspecified` value to `MessageType` enum
- [ ] Update help provider to show `( )` (blank) for `Unspecified`
- [ ] Ensure `Unspecified` behaves like `Command` for AI agent tooling

### Update Source Generator
- [ ] Detect if class implements `IQuery<T>` → emit `.WithMessageType(MessageType.Query)`
- [ ] Detect if class implements `ICommand<T>` → emit `.WithMessageType(MessageType.Command)`
- [ ] Detect if class implements `ICommand<T>` AND `IIdempotent` → emit `.WithMessageType(MessageType.IdempotentCommand)`
- [ ] Detect if class implements `IRequest<T>` (or none of above) → emit `.WithMessageType(MessageType.Unspecified)`

### Update Tests
- [ ] Add source generator test: `IQuery<T>` → `MessageType.Query`
- [ ] Add source generator test: `ICommand<T>` → `MessageType.Command`
- [ ] Add source generator test: `ICommand<T>, IIdempotent` → `MessageType.IdempotentCommand`
- [ ] Add source generator test: `IRequest<T>` → `MessageType.Unspecified`

### Verify Sample
- [ ] Run `samples/attributed-routes` and verify help shows correct indicators:
  - `greet` → `(Q)`
  - `deploy` → `(C)`
  - `config set` → `(I)`
  - `config get` → `(Q)`

## Notes

### Design Decision

| Interface | MessageType | Display | Behavior |
|-----------|-------------|---------|----------|
| `IRequest<T>` | `Unspecified` | `( )` blank | Treated as Command (safe default) |
| `IQuery<T>` | `Query` | `(Q)` | Safe to retry, read-only |
| `ICommand<T>` | `Command` | `(C)` | Mutating, needs confirmation |
| `ICommand<T>, IIdempotent` | `IdempotentCommand` | `(I)` | Mutating but safe to retry |

### Interface Detection

The generator needs to check the implemented interfaces on the request class:

```csharp
// Pseudo-code for interface detection
if (classSymbol.AllInterfaces.Any(i => i.Name == "IQuery" && i.TypeArguments.Length == 1))
  return MessageType.Query;
  
if (classSymbol.AllInterfaces.Any(i => i.Name == "ICommand" && i.TypeArguments.Length == 1))
{
  if (classSymbol.AllInterfaces.Any(i => i.Name == "IIdempotent"))
    return MessageType.IdempotentCommand;
  return MessageType.Command;
}

return MessageType.Unspecified;
```

### Sample Already Updated

The `samples/attributed-routes` commands have been updated to use:
- `IQuery<Unit>` for read-only commands
- `ICommand<Unit>` for mutating commands
- `ICommand<Unit>, IIdempotent` for idempotent commands

After this task, the help output should show correct indicators.

### Related

- Task 186: Source generation verification tests (add MessageType tests here)
- Future: Analyzer to warn when `IRequest<T>` is used (separate task)
