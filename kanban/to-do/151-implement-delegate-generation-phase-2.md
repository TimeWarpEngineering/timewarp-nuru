# Implement Delegate Generation (Phase 2)

## Description

Delegates in `Map()` calls automatically become Commands through the pipeline. Source generator detects `Map(string pattern, Delegate handler)` calls and generates Command class from delegate signature, Handler class from delegate body.

**Goal:** Quick prototyping with delegates while maintaining full pipeline benefits.

## Parent

148-generate-command-and-handler-from-delegate-map-calls

## Dependencies

- Task 149: Implement CompiledRouteBuilder (Phase 0) - must be complete
- Task 150: Implement Attributed Routes (Phase 1) - should be complete (shared infrastructure)

## Checklist

### Source Generator - Detection
- [ ] Detect `Map(string pattern, Delegate handler)` calls
- [ ] Extract pattern string from first argument
- [ ] Extract delegate from second argument
- [ ] Parse delegate signature (parameters, types, return type)
- [ ] Detect async delegates (`Task`, `Task<T>`, `ValueTask`, etc.)

### Source Generator - Command Generation
- [ ] Generate Command class from delegate signature
- [ ] Name convention: `{RoutePrefix}_Generated_Command`
- [ ] Include only route parameters (not DI parameters)
- [ ] Apply `[GeneratedCode]` attribute
- [ ] Generate as `sealed class` with properties (NOT record, NO primary constructor)

### Source Generator - Handler Generation
- [ ] Generate Handler class wrapping delegate body
- [ ] Name convention: `{RoutePrefix}_Generated_CommandHandler`
- [ ] Implement `IRequestHandler<TRequest, TResponse>` (Mediator interface)
- [ ] Rewrite parameter references (`x` -> `request.X`)
- [ ] Handle async delegates correctly
- [ ] Apply `[GeneratedCode]` attribute

### DI Integration
- [ ] Detect parameters not in route pattern (DI parameters)
- [ ] Generate constructor injection for DI parameters
- [ ] Store DI parameters as private readonly fields
- [ ] Use fields in handler body

### Return Values
- [ ] Handle `void` return -> `IRequest<Unit>`
- [ ] Handle `int` return -> `IRequest<int>`
- [ ] Handle `Task` return -> async handler, `IRequest<Unit>`
- [ ] Handle `Task<int>` return -> async handler, `IRequest<int>`
- [ ] Handle `Task<T>` return -> async handler, `IRequest<T>`

### Registration
- [ ] Emit `CompiledRouteBuilder` calls from parsed pattern
- [ ] Emit route registration code
- [ ] Emit DI registration for generated handler

### Testing
- [ ] Test simple delegate `(string name) => ...`
- [ ] Test delegate with multiple parameters
- [ ] Test delegate with option parameters (bool flags)
- [ ] Test async delegate returning `Task`
- [ ] Test async delegate returning `Task<int>`
- [ ] Test delegate with DI parameter (e.g., `ILogger`)
- [ ] Test generated Command has correct properties
- [ ] Test generated Handler executes delegate body
- [ ] Verify full pipeline execution

## Notes

### Reference

- **Design doc:** `kanban/to-do/148-generate-command-and-handler-from-delegate-map-calls/fluent-route-builder-design.md` (lines 836-951)

### Example Transformation

```csharp
// User writes:
app.Map("deploy {env} --force", (string env, bool force, ILogger logger) => 
{
    logger.LogInformation("Deploying to {Env}", env);
    return 0;
});

// Generator emits:

// 1. Command (only route parameters) - class with properties, NOT record
[GeneratedCode("TimeWarp.Nuru.Generator", "1.0.0")]
public sealed class Deploy_Generated_Command : IRequest<int>
{
    public string Env { get; set; } = string.Empty;
    public bool Force { get; set; }
}

// 2. Handler (DI via constructor, delegate body with rewritten params)
[GeneratedCode("TimeWarp.Nuru.Generator", "1.0.0")]
public sealed class Deploy_Generated_CommandHandler 
    : IRequestHandler<Deploy_Generated_Command, int>
{
    private readonly ILogger _logger;
    
    public Deploy_Generated_CommandHandler(ILogger logger)
    {
        _logger = logger;
    }
    
    public Task<int> Handle(Deploy_Generated_Command request, CancellationToken ct)
    {
        _logger.LogInformation("Deploying to {Env}", request.Env);
        return Task.FromResult(0);
    }
}

// 3. Route + Registration
private static readonly CompiledRoute __Route_Deploy = new CompiledRouteBuilder()
    .WithLiteral("deploy")
    .WithParameter("env")
    .WithOption("force")
    .Build();
```

**Notes:**
- Generated Commands use classes with properties, NOT records or primary constructors
- Actual interfaces are `IRequest<TResponse>` / `IRequestHandler<TRequest, TResponse>` (Mediator)
- We use "Command" in class names as it's more natural CLI terminology

### Complexity Notes

This is the most complex phase:
- Delegate body extraction requires syntax tree manipulation
- Parameter rewriting (`env` -> `command.Env`) needs careful handling
- DI parameter detection requires comparing route params vs delegate params
- Async handling has multiple variants

### Releasable

Yes - enables quick prototyping while getting full pipeline benefits.
