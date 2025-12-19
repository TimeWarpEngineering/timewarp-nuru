# Update Samples for New Map().WithHandler().Done() API

## Description

Update all sample applications to use the new fluent API:

```csharp
// Old
app.Map("deploy {env}", (string env) => Deploy(env));

// New
app.Map("deploy {env}")
    .WithHandler((string env) => Deploy(env))
    .AsCommand()
    .Done();
```

## Parent

151-implement-delegate-generation-phase-2

## Dependencies

- Task 192: Old API removed (samples will have compile errors)

## Checklist

### Calculator Sample
- [ ] `samples/calculator/calc-delegate.cs`
- [ ] `samples/calculator/calc-createbuilder.cs`
- [ ] Other calc-*.cs files if applicable

### Hello World
- [ ] `samples/hello-world/hello-world.cs`

### Async Examples
- [ ] `samples/async-examples/async-examples.cs`

### Logging Samples
- [ ] `samples/logging/console-logging.cs`
- [ ] `samples/logging/serilog-logging.cs`

### Configuration Samples
- [ ] `samples/configuration/*.cs`

### Testing Samples
- [ ] `samples/testing/*.cs`

### REPL Demo
- [ ] `samples/repl-demo/*.cs`

### Other Samples
- [ ] `samples/timewarp-nuru-sample/program.cs`
- [ ] `samples/pipeline-middleware/pipeline-middleware.cs`
- [ ] `samples/unified-middleware/unified-middleware.cs`
- [ ] `samples/terminal/*.cs`
- [ ] Any other files using `Map(pattern, handler)` or `MapDefault(handler)`

### Migration Pattern

For each file:
1. Find all `Map("pattern", handler)` calls
2. Convert to `Map("pattern").WithHandler(handler).AsCommand().Done()`
3. Find all `MapDefault(handler)` calls
4. Convert to `Map("").WithHandler(handler).AsQuery().Done()`
5. Choose appropriate MessageType:
   - Side effects → `.AsCommand()`
   - Read-only → `.AsQuery()`
   - Retryable mutations → `.AsIdempotentCommand()`

### Build Verification
- [ ] Build entire solution after updates
- [ ] Run samples to verify they work

## Notes

### Common Patterns

**Void handler (side effect):**
```csharp
// Old
app.Map("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"));

// New
app.Map("greet {name}")
    .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
    .AsCommand()
    .Done();
```

**Return value handler:**
```csharp
// Old
app.Map("add {x:int} {y:int}", (int x, int y) => x + y);

// New
app.Map("add {x:int} {y:int}")
    .WithHandler((int x, int y) => x + y)
    .AsQuery()
    .Done();
```

**Async handler:**
```csharp
// Old
app.Map("fetch {url}", async (string url) => await httpClient.GetAsync(url));

// New
app.Map("fetch {url}")
    .WithHandler(async (string url) => await httpClient.GetAsync(url))
    .AsQuery()
    .Done();
```

**Default route:**
```csharp
// Old
app.MapDefault(() => ShowHelp());

// New
app.Map("")
    .WithHandler(() => ShowHelp())
    .AsQuery()
    .Done();
```
