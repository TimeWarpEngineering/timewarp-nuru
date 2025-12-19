# Create NuruDelegateCommandGenerator (Detection + Pattern Parsing)

## Description

Create new source generator that detects `Map(pattern).WithHandler(delegate)` chains and extracts the necessary information for code generation.

## Parent

151-implement-delegate-generation-phase-2

## Dependencies

- Task 192: API cleanup must be complete (old overloads removed)

## Checklist

### Generator Setup
- [ ] Create `source/timewarp-nuru-analyzers/analyzers/nuru-delegate-command-generator.cs`
- [ ] Implement `IIncrementalGenerator`
- [ ] Register syntax provider for `WithHandler` invocations

### Detection
- [ ] Find `WithHandler(delegate)` calls on `EndpointBuilder`
- [ ] Walk syntax tree back to find `Map(pattern)` call
- [ ] Extract pattern string from `Map()` argument
- [ ] Handle both lambda expressions and method groups

### Delegate Signature Extraction
- [ ] Extract parameter list (names, types)
- [ ] Extract return type
- [ ] Detect async delegates (`Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`)
- [ ] Detect nullable parameters

### Route Pattern Parsing
- [ ] Extract parameter names from pattern: `{name}`, `{name?}`, `{name:type}`
- [ ] Extract option names from pattern: `--option`, `--option,-o`
- [ ] Identify catch-all: `{*args}`
- [ ] Simple regex-based extraction (no need for full PatternParser)

### DI Parameter Detection
- [ ] Compare delegate parameters to route parameters
- [ ] Parameters not in route = DI parameters
- [ ] Store DI parameter info for handler generation

### Data Model
- [ ] Create `DelegateRouteInfo` record to hold extracted data
- [ ] Include: pattern, delegate signature, route params, DI params, return type, isAsync

## Notes

This task sets up the infrastructure. Actual code generation happens in Tasks 194-196.

### Example Detection

```csharp
// Generator should detect this:
app.Map("deploy {env} --force")
    .WithHandler((string env, bool force, ILogger logger) => { ... })
    .AsCommand()
    .Done();

// And extract:
// - Pattern: "deploy {env} --force"
// - Route params: [env (string), force (bool)]
// - DI params: [logger (ILogger)]
// - Return type: void
// - IsAsync: false
```
