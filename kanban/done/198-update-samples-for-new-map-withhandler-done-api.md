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
- [x] `samples/calculator/calc-delegate.cs`
- [x] `samples/calculator/calc-createbuilder.cs`
- [x] Other calc-*.cs files if applicable

### Hello World
- [x] `samples/hello-world/hello-world.cs`

### Async Examples
- [x] `samples/async-examples/async-examples.cs`

### Logging Samples
- [x] `samples/logging/console-logging.cs`
- [x] `samples/logging/serilog-logging.cs`

### Configuration Samples
- [x] `samples/configuration/*.cs`

### Testing Samples
- [x] `samples/testing/*.cs`

### REPL Demo
- [x] `samples/repl-demo/*.cs`

### Other Samples
- [x] `samples/timewarp-nuru-sample/program.cs`
- [x] `samples/pipeline-middleware/pipeline-middleware.cs` (no old API usage found)
- [x] `samples/unified-middleware/unified-middleware.cs` (no old API usage found)
- [x] `samples/terminal/*.cs` (no old API usage found)
- [x] `samples/aot-example/aot-example.cs`
- [x] `samples/syntax-examples.cs`
- [x] `samples/builtin-types-example.cs`
- [x] `samples/custom-type-converter-example.cs`

### Additional Files (outside samples/)
- [x] `tests/test-apps/timewarp-nuru-testapp-delegates/program.cs`
- [x] `benchmarks/timewarp-nuru-benchmarks/commands/nuru-direct-command.cs`

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
- [x] Build entire solution after updates
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
