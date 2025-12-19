# Epic: Implement Delegate Generation (Phase 2)

## Type

Epic

## Description

Delegates in `Map()` calls automatically become Commands through the pipeline. The new API uses fluent chaining:

```csharp
app.Map("deploy {env} --force")
    .WithHandler((string env, bool force, ILogger logger) => {
        logger.LogInformation("Deploying to {Env}", env);
    })
    .AsCommand()
    .Done();
```

**Goal:** Quick prototyping with delegates while maintaining full pipeline benefits.

## Parent

148-epic-nuru-3-unified-route-pipeline

## Dependencies

- Task 149: Implement CompiledRouteBuilder (Phase 0) ✅
- Task 150: Implement Attributed Routes (Phase 1) ✅

## API Changes (Breaking - 3.0)

**Remove:**
- `Map(string pattern, Delegate handler)` - all overloads
- `MapDefault(Delegate handler)` - use `Map("")` instead

**New Standard Pattern:**
```csharp
app.Map("deploy {env} --force")
    .WithHandler((string env, bool force) => { ... })
    .AsCommand()
    .Done();

// Default route (replaces MapDefault)
app.Map("")
    .WithHandler(() => ShowHelp())
    .AsQuery()
    .Done();
```

## Subtasks

- **Task 192**: Remove Map(pattern, handler) and MapDefault overloads
- **Task 193**: Create NuruDelegateCommandGenerator (detection + pattern parsing)
- **Task 194**: Generate Command classes from delegate signatures
- **Task 195**: Generate Handler classes with parameter rewriting
- **Task 196**: Add MessageType detection (.AsQuery, .AsCommand, etc.)
- **Task 197**: Update NuruInvokerGenerator for new API
- **Task 198**: Update samples for new Map().WithHandler().Done() API
- **Task 199**: Add delegate generation tests

## Checklist

- [x] Task 192: API cleanup (removed Map(pattern, handler), MapDefault, added WithDescription())
- [ ] Task 193: Generator detection
- [ ] Task 194: Command generation
- [ ] Task 195: Handler generation (complex - parameter rewriting)
- [ ] Task 196: MessageType support
- [ ] Task 197: Invoker generator updates
- [x] Task 198: Sample updates (28 files migrated to fluent API)
- [ ] Task 199: Test coverage

## Example Transformation

**User writes:**
```csharp
app.Map("deploy {env} --force")
    .WithHandler((string env, bool force, ILogger logger) => {
        logger.LogInformation("Deploying to {Env}", env);
    })
    .AsCommand()
    .Done();
```

**Generator emits:**
```csharp
// Command
[GeneratedCode("TimeWarp.Nuru.Analyzers", "1.0.0")]
public sealed class Deploy_Generated_Command : ICommand<Unit>
{
    public string Env { get; set; } = string.Empty;
    public bool Force { get; set; }
}

// Handler
[GeneratedCode("TimeWarp.Nuru.Analyzers", "1.0.0")]
public sealed class Deploy_Generated_CommandHandler 
    : ICommandHandler<Deploy_Generated_Command, Unit>
{
    private readonly ILogger _logger;
    
    public Deploy_Generated_CommandHandler(ILogger logger)
    {
        _logger = logger;
    }
    
    public ValueTask<Unit> Handle(Deploy_Generated_Command request, CancellationToken ct)
    {
        _logger.LogInformation("Deploying to {Env}", request.Env);
        return default;
    }
}

// Registration (in ModuleInitializer)
NuruRouteRegistry.Register<Deploy_Generated_Command>(
    new CompiledRouteBuilder()
        .WithLiteral("deploy")
        .WithParameter("env")
        .WithOption("force")
        .WithMessageType(MessageType.Command)
        .Build(),
    "deploy {env} --force");
```

## Notes

### Design Decisions

1. **Remove old API entirely** - No backward compat, 3.0 is breaking
2. **Require Done()** - Enables MessageType fluent chain
3. **Accept verbosity** - `Map("x").WithHandler(...).Done()` vs `Map("x", ...)`
4. **Separate generators** - Easier to reason about
5. **Error on closures** - Captured variables emit diagnostic error

### Complexity Notes

- Handler generation is the most complex part (parameter rewriting)
- DI detection requires comparing route params vs delegate params
- Async handling has multiple variants (Task, Task<T>, ValueTask, etc.)

### Releasable

Yes - enables quick prototyping while getting full pipeline benefits.
