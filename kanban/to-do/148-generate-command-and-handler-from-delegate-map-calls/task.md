# Generate Command and Handler from Delegate Map Calls

## Description

Extend the existing `NuruInvokerGenerator` (or create a sibling generator) to emit `IRequest` Command classes and `IRequestHandler<T>` Handler classes from delegate-based `Map()` calls. This unifies the execution model so that delegates become pure syntactic sugar — all routes flow through the same Command/Handler pipeline.

Currently:
- Delegate routes use `DelegateExecutor` wrapper to go through the pipeline
- Command routes use Mediator dispatch

After this change:
- All delegate `Map()` calls generate Command + Handler at compile time
- Single execution model (everything is a command)
- `DelegateExecutor` becomes obsolete for generated code
- No dependency on `Mediator.SourceGenerator` — only `Mediator.Abstractions` for interfaces

## Checklist

- [ ] Review existing `NuruInvokerGenerator` implementation for reusable infrastructure
- [ ] Design Command class naming convention (e.g., `Add_Generated_Command` from pattern "add {x:double}")
- [ ] Design Handler class structure (nested vs separate, delegate invocation)
- [ ] Implement Command class generation from delegate signature
- [ ] Implement Handler class generation that wraps the original delegate
- [ ] Generate DI registration method (`AddNuruCommands()` or similar)
- [ ] Handle async delegates (`Func<..., Task>`) appropriately
- [ ] Handle return values for exit codes (`Func<..., int>`)
- [ ] Handle closures/captured variables in delegates
- [ ] Update `Map()` call transformation to use generated Command type
- [ ] Add tests for generated Command/Handler code
- [ ] Verify AOT compatibility of generated code
- [ ] Update documentation

## Notes

### Existing Infrastructure (from `NuruInvokerGenerator`)

The current generator already extracts:
- Pattern string from `Map()` first argument
- Delegate signature (parameter names, types, nullability)
- Return type (void, Task, Task<T>, value types)
- Async detection

This same data can generate Command + Handler classes.

### Example Transformation

Input:
```csharp
builder.Map("add {x:double} {y:double}", (double x, double y) =>
{
    Console.WriteLine($"{x + y}");
});
```

Generated output:
```csharp
[GeneratedCode("TimeWarp.Nuru.Analyzers", "1.0")]
internal sealed class Add_Generated_Command : IRequest
{
    public double X { get; set; }
    public double Y { get; set; }
}

[GeneratedCode("TimeWarp.Nuru.Analyzers", "1.0")]
internal sealed class Add_Generated_Handler : IRequestHandler<Add_Generated_Command>
{
    public ValueTask<Unit> Handle(Add_Generated_Command request, CancellationToken ct)
    {
        Console.WriteLine($"{request.X + request.Y}");
        return default;
    }
}
```

### Key Decisions Needed

1. **Naming strategy**: How to generate unique, readable class names from patterns
2. **Closure handling**: Delegates capturing local variables need special treatment
3. **Opt-in vs automatic**: Should this be automatic for all `Map()` calls or opt-in via attribute?

### Dependencies

- Reference `Mediator.Abstractions` for `IRequest`, `IRequestHandler<T>`, `Unit`
- No dependency on `Mediator.SourceGenerator` (avoids two-pass compilation issue)

### Benefits

- Single execution model for all routes
- Consistent pipeline behavior (middleware works identically)
- Better AOT support (no reflection for delegate invocation)
- Improved debuggability (real command types in stack traces)
- Testability even for "quick" delegate routes
