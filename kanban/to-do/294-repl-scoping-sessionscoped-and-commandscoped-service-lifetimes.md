# REPL scoping - SessionScoped and CommandScoped service lifetimes

## Parent

#265 Epic: V2 Source Generator Implementation

## Description

Extend the static service injection system (#292) with REPL-aware scoping. Standard DI `Scoped` lifetime is ambiguous for CLI apps - this task introduces explicit scopes:

- **SessionScoped**: One instance per REPL session (created when REPL starts, disposed when REPL exits)
- **CommandScoped**: One instance per command invocation (created per REPL command, disposed after)

## Motivation

In REPL mode, users may want different scoping behavior:
- Database connections should persist across commands (SessionScoped)
- Unit-of-work patterns should reset per command (CommandScoped)
- Standard `Scoped` from ASP.NET doesn't map cleanly to CLI

## Proposed API

### Option A: New Extension Methods
```csharp
.ConfigureServices(services => {
  services.AddSessionScoped<IDbConnection, DbConnection>();
  services.AddCommandScoped<IUnitOfWork, UnitOfWork>();
})
```

### Option B: Custom Enum
```csharp
.ConfigureServices(services => {
  services.Add<IDbConnection, DbConnection>(NuruLifetime.SessionScoped);
  services.Add<IUnitOfWork, UnitOfWork>(NuruLifetime.CommandScoped);
})
```

### Scoped Default Behavior
When user writes `services.AddScoped<>()`:
- In normal CLI mode → treat as `Singleton` (one invocation = one scope)
- In REPL mode → treat as `CommandScoped` (each command = new scope)

## Checklist

- [ ] Design API for SessionScoped/CommandScoped registration
- [ ] Extend `ServiceDefinition` or create `NuruServiceLifetime` enum
- [ ] Update `ServiceExtractor` to recognize new registration patterns
- [ ] Update emitter to generate scoped instance management
- [ ] For SessionScoped: emit field that persists across REPL commands
- [ ] For CommandScoped: emit new instance per command invocation
- [ ] Handle `IDisposable` services - dispose at scope end
- [ ] Update REPL loop to manage scope lifecycle
- [ ] Document scoping behavior differences from ASP.NET

## Generated Code Concept

```csharp
// SessionScoped - persists across REPL commands
private static DbConnection? __session_DbConnection;

// In REPL loop start:
__session_DbConnection = new DbConnection();

// In each command handler:
DbConnection dbConnection = __session_DbConnection!;
UnitOfWork unitOfWork = new UnitOfWork(dbConnection); // CommandScoped - new each time

// In REPL loop end:
__session_DbConnection?.Dispose();
```

## Dependencies

- Requires #292 (static service injection) to be completed first

## Notes

This is a follow-up to #292 which initially treats `Scoped` as `Singleton`. This task adds proper REPL-aware scoping when needed.
