# Generate Handler Classes with Parameter Rewriting

## Description

Extend `NuruDelegateCommandGenerator` to emit Handler classes that wrap delegate bodies. This is the most complex part - requires rewriting parameter references in the delegate body.

## Parent

151-implement-delegate-generation-phase-2

## Dependencies

- Task 194: Command generation must be complete

## Checklist

### Handler Class Generation
- [x] Generate nested `Handler` class inside Command class
- [x] Apply `[GeneratedCode("TimeWarp.Nuru.Analyzers", "1.0.0")]` attribute
- [x] Implement `ICommandHandler<TCommand, TResult>`

### DI Constructor Injection
- [x] Generate constructor with DI parameters
- [x] Generate private readonly fields for each DI parameter
- [x] Use `PascalCase` naming for fields (per coding standard - no underscore prefix)
- [x] Example: `ILogger logger` → `private readonly ILogger Logger;`

### Handle Method
- [x] Generate `Handle(TCommand request, CancellationToken ct)` method
- [x] Return `ValueTask<TResult>` (Mediator convention)
- [x] Sync delegates: wrap in `ValueTask.FromResult()`
- [ ] Async delegates: await and return (not yet tested)

### Parameter Rewriting (Complex)
- [x] Route parameters: `env` → `request.Env`
- [x] DI parameters: `logger` → `Logger`
- [x] Walk the delegate body syntax tree using `DescendantNodesAndSelf()`
- [x] Replace `IdentifierNameSyntax` nodes with appropriate references
- [x] Handle nested scopes carefully (don't rewrite local variables with same name)
- [x] Handle expression-bodied lambdas: `(text) => text` → `request.Text`

### Delegate Body Extraction
- [x] Extract body from lambda expression
- [x] Handle expression lambdas: `x => x + 1`
- [x] Handle statement lambdas: `x => { return x + 1; }`
- [x] Preserve original formatting where possible

### Async Handling
- [x] Detect async lambda (`async (x) => await ...`)
- [ ] Generate async Handle method if needed (basic support only)
- [ ] Handle `Task` return (no result) → return `Unit.Value`
- [ ] Handle `Task<T>` return → return awaited result

### Error Handling
- [x] Skip handler generation for closures (captured variables)
- [x] Conservative closure detection: unresolved symbols treated as closures
- [ ] Emit diagnostic error for closures (NURU002 - not yet implemented)
- [ ] Emit diagnostic warning for method groups (NURU003 - not yet implemented)

## Results

**2025-12-19:** Core handler generation is complete and working.

- All 1673 CI tests pass (16 skipped for unimplemented features)
- Fixed bug where `DescendantNodes()` missed expression-bodied lambdas - changed to `DescendantNodesAndSelf()`
- Fixed closure detection to be conservative when symbol resolution fails
- Enabled `EmitCompilerGeneratedFiles` repo-wide for debugging

**Remaining work:**
- Full async lambda support (Task 195 continuation or separate task)
- Diagnostic reporting for closures/method groups (can be deferred)

## Example Output

**Input:**
```csharp
app.Map("deploy {env} --force")
    .WithHandler((string env, bool force, ILogger logger) => {
        logger.LogInformation("Deploying to {Env}", env);
    })
    .AsCommand()
    .Done();
```

**Generated:**
```csharp
[global::System.CodeDom.Compiler.GeneratedCode("TimeWarp.Nuru.Analyzers", "1.0.0")]
public sealed class Deploy_Generated_Command : global::Mediator.ICommand<global::Mediator.Unit>
{
  public string Env { get; set; } = string.Empty;
  public bool Force { get; set; }

  public sealed class Handler : global::Mediator.ICommandHandler<Deploy_Generated_Command, global::Mediator.Unit>
  {
    private readonly global::Microsoft.Extensions.Logging.ILogger Logger;
    
    public Handler(global::Microsoft.Extensions.Logging.ILogger logger)
    {
      Logger = logger;
    }
    
    public global::System.Threading.Tasks.ValueTask<global::Mediator.Unit> Handle(
      Deploy_Generated_Command request, 
      global::System.Threading.CancellationToken cancellationToken)
    {
      Logger.LogInformation("Deploying to {Env}", request.Env);
      return default;
    }
  }
}
```

## Notes

### Closure Detection

Closures capture variables from enclosing scope. Handler generation is skipped for these:

```csharp
string? capturedEnv = null;  // Captured variable
app.Map("deploy {env}")
    .WithHandler((string env) => { capturedEnv = env; return 0; })  // Handler skipped
    .AsCommand()
    .Done();
```

Command class is still generated, but without nested Handler. Delegate runs directly at runtime.

### Parameter Rewriting Strategy

Using `ParameterRewriter` class:
1. Collect local variables declared in lambda to avoid rewriting them
2. Visit all `IdentifierNameSyntax` nodes via `DescendantNodesAndSelf()`
3. If identifier matches a route parameter name → rewrite to `request.{PascalName}`
4. If identifier matches a DI parameter name → rewrite to `{PascalName}` field
5. Otherwise leave unchanged
