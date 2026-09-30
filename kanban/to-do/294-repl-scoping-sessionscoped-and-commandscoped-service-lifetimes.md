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

## Decision 2026-09-30 (cockpit)

**Option A (extension methods):** `services.AddSessionScoped<TService, TImpl>()` and
`services.AddCommandScoped<TService, TImpl>()` (plus factory overloads if `AddSingleton` has them). No new enum
in the public API; an internal lifetime marker in the generator model is fine.

Semantics:

| Registration | Single CLI invocation | REPL |
|---|---|---|
| `AddSingleton` | one instance | one instance for the process |
| `AddSessionScoped` | one instance | one instance for the REPL session; disposed when the REPL exits |
| `AddCommandScoped` | one instance | a new instance per command; disposed after that command |
| `AddScoped` | one instance (today's behavior) | **CommandScoped** |
| `AddTransient` | new per resolution | new per resolution |

`IDisposable` / `IAsyncDisposable` instances are disposed at the end of their scope (async first when both).

**Both DI paths:** the source-generated static resolver (`service-resolver-emitter.cs`, which today maps
Scoped to a static field around lines 100 and 363) and the Microsoft DI path
(`.UseMicrosoftDependencyInjection()`): in REPL mode create an `IServiceScope` per command and dispose it
after the command; session-scoped maps to the root provider with disposal at REPL exit. The generated
mediator registration (`AddGeneratedMediator`, epic 443) must still resolve correctly per command.

Tests: each row of the table in both DI paths (a REPL test that runs two commands and checks instance
identity and disposal), plus a non-REPL invocation proving today's single-run behavior is unchanged.
Document the table in the user docs next to the existing DI documentation.

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

## Notes for the implementer

- #292 (static service injection) is done; its Scoped-as-singleton fallback is the current behavior.
- If a row of the table proves unworkable in one DI path, return `ORACLE_RESULT: Blocked — <row, path, reason>`.
- Commit and push your changes before reporting done. Run the build and test gate in the foreground.
