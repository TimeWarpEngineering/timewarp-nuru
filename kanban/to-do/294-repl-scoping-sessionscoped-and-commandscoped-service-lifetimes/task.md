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

- [x] Design API for SessionScoped/CommandScoped registration
- [x] Extend `ServiceDefinition` or create `NuruServiceLifetime` enum
- [x] Update `ServiceExtractor` to recognize new registration patterns
- [x] Update emitter to generate scoped instance management
- [x] For SessionScoped: emit field that persists across REPL commands
- [x] For CommandScoped: emit new instance per command invocation
- [x] Handle `IDisposable` services - dispose at scope end
- [x] Update REPL loop to manage scope lifecycle
- [x] Document scoping behavior differences from ASP.NET

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
- 2026-09-30: review oracle (ganda task work, tw-implementation-review effort 1, reviewer `general`). Session: review-oracle (Cursor implementer-cursor profile, headless). Rounds: 2. Final counts: bug 4 fixed, suggestion 0, nit 0, open 0. Disposition: clean. Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`. Next host nodes: open-pr / done (no apply-review sibling).

## Results

Option A is the public API: `AddSessionScoped` and `AddCommandScoped` (type, factory, and `TryAdd` overloads; `AddSessionScoped` also accepts an instance). Lifetimes are an internal generator enum, not a public one.

Both DI paths follow the decision table. Source-generated DI keeps session instances in `__ses_` fields and command or REPL-scoped instances in `__cmd_` fields. A single CLI invocation still treats `AddScoped` as one process instance and does not dispose it. The REPL disposes command-scoped and `AddScoped` instances after each command, and session-scoped instances when the REPL exits. `DisposeAsync` is used when a type implements both dispose interfaces.

`UseMicrosoftDependencyInjection()` maps session scope to a root cache (`NuruSessionCache`) and command scope to `NuruCommandCache`. In the REPL, each command gets an `IServiceScope` so `AddScoped` is command-scoped. Session services stay on the root cache; a command scope drops them from its disposable list before it disposes, including when a scoped service injected them. `AddGeneratedMediator` still resolves per command. Starting the REPL (`--interactive`, `-i`, or `AutoStartWhenEmpty`) does not open a command cache, so nested commands can begin their own. A source-generated scoped service that injects a session-scoped or command-scoped service is built with that invocation's instances.

### How to validate

Smoke:

```bash
dotnet tests/ci-tests/run-ci-tests.cs
ganda repo audit
dotnet run tests/timewarp-nuru-tests/generator/generator-51-repl-service-scopes.cs
```

Expect:

- The CI runner exits 0. Multi-mode total 1799, passed 1793, skipped 6, failed 0. Standalone phase passes.
- `ganda repo audit` prints "Repository passes all audit checks." Passed 29, Failed 0. (`bin/dev` is gitignored; `ganda repo audit --fix` ran `self-install`.)
- `generator-51-repl-service-scopes.cs` exits 0 with 6 passed: source-generated and runtime DI, each covering one CLI invocation, a REPL script of two `check` commands plus `ping`, and an `AutoStartWhenEmpty` REPL. Singleton identity is stable and not disposed. Session identity is stable across the two REPL commands and disposed once on exit, including when a scoped service injects the session. Command-scoped and REPL `AddScoped` identities change per command and are disposed after the command. Transient identity changes per resolution. The auto-start host does not construct an extra command instance. `ping` prints `pong` through the generated mediator.

### Review

- 2026-09-30: review oracle (ganda task work, tw-implementation-review effort 1, reviewer `general`). Session: review-oracle (Cursor implementer-cursor profile, headless). Rounds: 2. Final counts: bug 4 fixed, suggestion 0, nit 0, open 0. Disposition: clean. Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`. Next host nodes: open-pr / done (no apply-review sibling).

