# Generate Handler Classes with Parameter Rewriting

## Description

Extend `NuruDelegateCommandGenerator` to emit Handler classes that wrap delegate bodies. This is the most complex part - requires rewriting parameter references in the delegate body.

## Parent

151-implement-delegate-generation-phase-2

## Dependencies

- Task 194: Command generation must be complete

## Checklist

### Handler Class Generation
- [ ] Generate `sealed class {Prefix}_Generated_CommandHandler`
- [ ] Apply `[GeneratedCode("TimeWarp.Nuru.Analyzers", "1.0.0")]` attribute
- [ ] Implement `ICommandHandler<TCommand, TResult>` or `IQueryHandler<TQuery, TResult>`

### DI Constructor Injection
- [ ] Generate constructor with DI parameters
- [ ] Generate private readonly fields for each DI parameter
- [ ] Use `_camelCase` naming for fields
- [ ] Example: `ILogger logger` → `private readonly ILogger _logger;`

### Handle Method
- [ ] Generate `Handle(TCommand request, CancellationToken ct)` method
- [ ] Return `ValueTask<TResult>` (Mediator convention)
- [ ] Sync delegates: wrap in `ValueTask.FromResult()`
- [ ] Async delegates: await and return

### Parameter Rewriting (Complex)
- [ ] Route parameters: `env` → `request.Env`
- [ ] DI parameters: `logger` → `_logger`
- [ ] Walk the delegate body syntax tree
- [ ] Replace `IdentifierNameSyntax` nodes with appropriate references
- [ ] Handle nested scopes carefully (don't rewrite local variables with same name)

### Delegate Body Extraction
- [ ] Extract body from lambda expression
- [ ] Handle expression lambdas: `x => x + 1`
- [ ] Handle statement lambdas: `x => { return x + 1; }`
- [ ] Preserve original formatting where possible

### Async Handling
- [ ] Detect async lambda (`async (x) => await ...`)
- [ ] Generate async Handle method if needed
- [ ] Handle `Task` return (no result) → return `Unit.Value`
- [ ] Handle `Task<T>` return → return awaited result

### Error Handling
- [ ] Emit diagnostic error for closures (captured variables)
- [ ] Emit diagnostic error if body extraction fails
- [ ] Emit diagnostic warning for complex scenarios

## Example Output

**Input:**
```csharp
app.Map("deploy {env} --force")
    .WithHandler((string env, bool force, ILogger logger) => {
        logger.LogInformation("Deploying to {Env}", env);
    })
```

**Generated:**
```csharp
[global::System.CodeDom.Compiler.GeneratedCode("TimeWarp.Nuru.Analyzers", "1.0.0")]
public sealed class Deploy_Generated_CommandHandler 
    : global::Mediator.ICommandHandler<Deploy_Generated_Command, global::Mediator.Unit>
{
    private readonly global::Microsoft.Extensions.Logging.ILogger _logger;
    
    public Deploy_Generated_CommandHandler(global::Microsoft.Extensions.Logging.ILogger logger)
    {
        _logger = logger;
    }
    
    public global::System.Threading.Tasks.ValueTask<global::Mediator.Unit> Handle(
        Deploy_Generated_Command request, 
        global::System.Threading.CancellationToken ct)
    {
        _logger.LogInformation("Deploying to {Env}", request.Env);
        return default;
    }
}
```

## Notes

### Closure Detection

Closures capture variables from enclosing scope. We can't easily transform these:

```csharp
string prefix = "deploy-";  // Captured variable
app.Map("deploy {env}")
    .WithHandler((string env) => Console.WriteLine(prefix + env));  // ❌ Error
```

Detect by checking if lambda references symbols from enclosing scope that aren't:
- Parameters of the lambda
- Static members
- Instance members via `this`

### Parameter Rewriting Strategy

Use a `CSharpSyntaxRewriter`:
1. Visit all `IdentifierNameSyntax` nodes
2. If identifier matches a route parameter name → rewrite to `request.{PascalName}`
3. If identifier matches a DI parameter name → rewrite to `_{camelName}`
4. Otherwise leave unchanged
