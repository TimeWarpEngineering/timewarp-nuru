# Fix Help Command REPL Context Detection

## Description

The `help` command shows REPL-specific commands (exit, clear, history, etc.) even when invoked from CLI context. The `--help` flag correctly hides them. Both should behave consistently based on actual execution context.

**Issue:** [GitHub Issue #106](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/106)

**Root Cause:** Commit 8807221 hardcoded `HelpContext.Repl` for the `help` route and `HelpContext.Cli` for `--help` at registration time. Context should be determined at execution time.

## Requirements

- `help` and `--help` produce identical output in CLI context
- REPL commands shown only when inside REPL session
- Use DI singleton pattern (not static) for session state

## Checklist

### Implementation
- [x] Create `SessionContext` class in `source/timewarp-nuru-core/`
  - `IsReplSession` property (bool)
  - `HelpContext` computed property
- [x] Register `SessionContext` as singleton in `service-collection-extensions.cs`
- [x] Update `help-route-generator.cs` to inject `SessionContext` and use `session.HelpContext`
- [x] Update `repl-session.cs` to set/reset `sessionContext.IsReplSession` in `RunAsync`
- [x] Add `SessionContext` property to `NuruCoreApp`
- [x] Update `LightweightServiceProvider` to resolve `SessionContext`
- [x] Update `NuruCoreAppBuilder.Build()` to create `SessionContext` for non-DI path

### Verification
- [x] `dotnet run -- help` hides REPL commands
- [x] `dotnet run -- --help` hides REPL commands  
- [x] Both outputs are identical in CLI context
- [ ] REPL `help` command shows REPL commands when inside REPL (manual test needed)

### Tests Added
- [x] `tests/timewarp-nuru-core-tests/help-provider-03-session-context.cs` (7 tests, all passing)

## Notes

### Files to Modify

| File | Change |
|------|--------|
| `source/timewarp-nuru-core/session-context.cs` | NEW - Singleton session context |
| `source/timewarp-nuru-core/service-collection-extensions.cs` | Register as singleton |
| `source/timewarp-nuru-core/help/help-route-generator.cs` | Inject SessionContext, remove hardcoded context |
| `source/timewarp-nuru-repl/repl/repl-session.cs` | Set/reset IsReplSession |

### Why Singleton (not Scoped)

`RouteExecutionContext` is scoped but can't be injected into delegate handlers because `BindDelegateParameters` uses root `ServiceProvider`. Singletons can be resolved from root provider.

### SessionContext Design

```csharp
public sealed class SessionContext
{
  public bool IsReplSession { get; set; }
  public HelpContext HelpContext => IsReplSession ? HelpContext.Repl : HelpContext.Cli;
}
```

### Analysis

Full analysis available at: `.agent/workspace/2025-12-08T02-40-39_issue-106-help-repl-context.md`
