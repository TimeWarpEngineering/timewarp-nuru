# Add MessageType Detection to Attributed Route Generator

## Description

Update `NuruAttributedRouteGenerator` to detect `IQuery<T>`, `ICommand<T>`, and `IIdempotent` interfaces on attributed request classes and emit appropriate `WithMessageType()` calls.

Currently all attributed routes default to `MessageType.Command`. After this task, the generator will infer the correct message type from the interfaces.

## Parent

150-implement-attributed-routes-phase-1

## Checklist

### Add MessageType.Unspecified
- [x] Add `Unspecified` value to `MessageType` enum
- [x] Update help provider to show `( )` (blank) for `Unspecified`
- [x] Ensure `Unspecified` behaves like `Command` for AI agent tooling

### Update Source Generator
- [x] Detect if class implements `IQuery<T>` → emit `.WithMessageType(MessageType.Query)`
- [x] Detect if class implements `ICommand<T>` → emit `.WithMessageType(MessageType.Command)`
- [x] Detect if class implements `ICommand<T>` AND `IIdempotent` → emit `.WithMessageType(MessageType.IdempotentCommand)`
- [x] Detect if class implements `IRequest<T>` (or none of above) → emit `.WithMessageType(MessageType.Unspecified)`

### Update Tests
- [x] Add source generator test: `IQuery<T>` → `MessageType.Query`
- [x] Add source generator test: `ICommand<T>` → `MessageType.Command`
- [x] Add source generator test: `ICommand<T>, IIdempotent` → `MessageType.IdempotentCommand`
- [x] Add source generator test: `IRequest<T>` → `MessageType.Unspecified`

### Verify Sample
- [x] Run `samples/attributed-routes` and verify help shows correct indicators:
  - `greet` → `(Q)`
  - `deploy` → `(C)`
  - `config set` → `(I)`
  - `config get` → `(Q)`

## Results

Successfully implemented MessageType detection for attributed routes. The source generator now:

1. **Added `MessageType.Unspecified`** - New enum value for routes that use `IRequest<T>` instead of `IQuery<T>` or `ICommand<T>`. Displays as `( )` in help output (gray color) to indicate unclassified.

2. **Source Generator Changes** (`nuru-attributed-route-generator.cs`):
   - Added `InferMessageType(INamedTypeSymbol)` method to detect interfaces
   - Added `InferredMessageType` field to `AttributedRouteInfo` record
   - Emit `.WithMessageType()` call for both main routes and aliases

3. **Bug Fix**: Found and fixed issue where `NuruCoreAppBuilder.Build()` wasn't copying `MessageType` from `CompiledRoute` to `Endpoint`. Added: `MessageType = registered.CompiledRoute.MessageType`

4. **Tests**: Created `attributed-route-generator-04-messagetype.cs` with 8 tests covering all MessageType detection scenarios.

5. **Updated test helper** (`attributed-route-test-helpers.cs`) to include `Mediator.dll` reference for proper interface detection in tests.

### Files Modified
- `source/timewarp-nuru-parsing/message-type.cs` - Added `Unspecified` enum value
- `source/timewarp-nuru-core/help/help-provider.cs` - Handle `Unspecified` in display
- `source/timewarp-nuru-core/nuru-core-app-builder.cs` - Copy MessageType to Endpoint
- `source/timewarp-nuru-analyzers/analyzers/nuru-attributed-route-generator.cs` - Interface detection logic

### Files Created
- `tests/timewarp-nuru-analyzers-tests/auto/attributed-route-generator-04-messagetype.cs`

## Notes

### Design Decision

| Interface | MessageType | Display | Behavior |
|-----------|-------------|---------|----------|
| `IRequest<T>` | `Unspecified` | `( )` blank | Treated as Command (safe default) |
| `IQuery<T>` | `Query` | `(Q)` | Safe to retry, read-only |
| `ICommand<T>` | `Command` | `(C)` | Mutating, needs confirmation |
| `ICommand<T>, IIdempotent` | `IdempotentCommand` | `(I)` | Mutating but safe to retry |

### Interface Detection

The generator checks implemented interfaces on the request class:

```csharp
// Actual implementation in InferMessageType()
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

After this task, the help output shows correct indicators.

### Related

- Task 186: Source generation verification tests (add MessageType tests here)
- Future: Analyzer to warn when `IRequest<T>` is used (separate task)
