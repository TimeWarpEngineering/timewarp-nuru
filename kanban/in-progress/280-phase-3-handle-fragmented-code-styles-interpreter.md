# Phase 3: Handle Fragmented Code Styles (Interpreter)

## Description

Enhance the interpreter to handle fragmented code styles where consumers use temporary variables and separate fluent chains across multiple statements. This phase focuses specifically on fragmented style support - the block-based infrastructure is handled in #283 and #284.

## Parent

#277 Epic: Semantic DSL Interpreter with Mirrored IR Builders

## Depends On

- #278 Phase 1: POC - Minimal Fluent Case ✅ (completed)
- #279 Phase 2: Add Group Support with CRTP ✅ (completed)
- #284 Phase 2a: Verify Group Support with Block Interpreter

## Scope

Support these fragmented code styles (beyond the pure fluent style already supported):

```csharp
// Style 2: Mixed - builder in variable
var builder = NuruApp.CreateBuilder([]);
builder.Map("ping").WithHandler(() => "pong").Done();
builder.Build();

// Style 3: Fully fragmented
var builder = NuruApp.CreateBuilder([]);
var endpoint = builder.Map("ping");
endpoint.WithHandler(() => "pong");
endpoint.Done();
builder.Build();

// Style 4: With non-builder code interleaved
var builder = NuruApp.CreateBuilder([]);
var junk = "Hi mom";
Console.WriteLine(junk);
builder.Map("ping").WithHandler(() => "pong").Done();
builder.Build();
```

## Checklist

### Ignore Non-Builder Statements

- [ ] Add `IsBuilderType(ITypeSymbol?)` helper method
- [ ] For statements that don't involve our builders, skip them
- [ ] Check if the expression involves any tracked symbols
- [ ] Examples to ignore:
  - `var junk = "Hi mom";` (string, not a builder)
  - `Console.WriteLine(junk);` (not on a tracked builder)
  - `int x = 5;` (primitive, not a builder)

### Handle Non-Assignment Method Calls

For code like:
```csharp
endpoint.WithHandler(() => "pong");  // No assignment, but modifies state
```

- [ ] Detect method calls on tracked builders that return `this` (fluent)
- [ ] Update the builder state in place
- [ ] Note: Our IR builders are mutable and return `this`, so this should work naturally

### Add Tests

- [ ] Create test file: `tests/timewarp-nuru-analyzers-tests/interpreter/dsl-interpreter-fragmented-test.cs`
- [ ] Test: Style 2 - Builder in variable
  ```csharp
  var builder = NuruApp.CreateBuilder([]);
  builder.Map("ping").WithHandler(() => "pong").Done();
  builder.Build();
  ```
- [ ] Test: Style 3 - Fully fragmented
  ```csharp
  var builder = NuruApp.CreateBuilder([]);
  var endpoint = builder.Map("ping");
  endpoint.WithHandler(() => "pong");
  endpoint.Done();
  builder.Build();
  ```
- [ ] Test: Style 4 - Non-builder code interleaved
  ```csharp
  var builder = NuruApp.CreateBuilder([]);
  var junk = "Hi mom";
  Console.WriteLine(junk);
  builder.Map("ping").WithHandler(() => "pong").Done();
  builder.Build();
  ```
- [ ] Test: Mixed group and fragmented
  ```csharp
  var builder = NuruApp.CreateBuilder([]);
  var admin = builder.WithGroupPrefix("admin");
  admin.Map("status").WithHandler(() => "ok").Done();
  admin.Done();
  builder.Build();
  ```

## Files to Modify

| File | Change |
|------|--------|
| `generators/interpreter/dsl-interpreter.cs` | Add `IsBuilderType()`, enhance statement handling |

## Files to Create

| File | Purpose |
|------|---------|
| `tests/.../interpreter/dsl-interpreter-fragmented-test.cs` | Fragmented style tests |

## Technical Notes

### Determining If Type Is Builder

```csharp
private bool IsBuilderType(ITypeSymbol? type)
{
  if (type is null) return false;
  
  string typeName = type.Name;
  return typeName is "NuruCoreAppBuilder" or "NuruAppBuilder"
      or "EndpointBuilder" or "GroupBuilder" or "GroupEndpointBuilder"
      or "NestedCompiledRouteBuilder";
}
```

### Handling Fluent Returns Without Assignment

```csharp
// For: endpoint.WithHandler(() => "pong");  // No assignment
// The IR builder is mutable and WithHandler returns 'this'
// So we don't need to update VariableState - the object is modified in place

// But for Done():
// endpoint.Done();  // Returns parent builder
// We should probably track that 'endpoint' is now "consumed"
// and the parent builder is the active one
```

## Success Criteria

1. All fragmented style tests pass
2. Non-builder statements are ignored without error
3. Fluent calls on variables work correctly
4. All existing tests still pass (10 from Phase 1/2 + 6 from Phase 2a = 16 total)
5. Mixed fragmented + group styles work

---

## Lessons Learned from Phase 2

### Marker Interfaces Enable Polymorphic Dispatch

Phase 2 introduced marker interfaces that should be leveraged here:

| Interface | Purpose | Use in Phase 3 |
|-----------|---------|----------------|
| `IIrRouteSource` | Can create routes/groups (`Map`, `WithGroupPrefix`) | Check if variable holds a route source |
| `IIrAppBuilder` | App-level builder | Check for app builder variables |
| `IIrGroupBuilder` | Group builder with `Done()` | Check for group builder variables |
| `IIrRouteBuilder` | Route builder with `Done()` | Check for route builder variables |

Instead of checking concrete types, use:
```csharp
if (value is IIrRouteSource or IIrRouteBuilder or IIrGroupBuilder)
{
  VariableState[symbol] = value;
}
```

### Interface-Based Dispatch Pattern

Phase 2 dispatch methods work well and should be extended:
```csharp
// Works for any route source (app or group builder)
if (currentReceiver is IIrRouteSource source)
  return source.Map(pattern);

// Works for any nested builder
return currentReceiver switch
{
  IIrRouteBuilder rb => rb.Done(),
  IIrGroupBuilder gb => gb.Done(),
  _ => throw ...
};
```

### Test File Naming Convention

Use `dsl-interpreter-*.cs` pattern:
- `dsl-interpreter-test.cs` - Phase 1 tests
- `dsl-interpreter-group-test.cs` - Phase 2 tests
- `dsl-interpreter-fragmented-test.cs` - Phase 3 tests

### Test Infrastructure

Tests are runfiles with shebang `#!/usr/bin/dotnet run`. The `Directory.Build.props` in `tests/timewarp-nuru-analyzers-tests/interpreter/` already:
- References the analyzer as a regular assembly (not analyzer output)
- Enables access to `DslInterpreter` and IR builder types

### Current Code Locations

| Component | Location |
|-----------|----------|
| IR Builder Interfaces | `generators/ir-builders/abstractions/` |
| IR Builder Classes | `generators/ir-builders/` |
| Interpreter | `generators/interpreter/dsl-interpreter.cs` |
| Tests | `tests/timewarp-nuru-analyzers-tests/interpreter/`|

### Expected Test Count After Phase 3

- Phase 1: 4 tests (minimal fluent case)
- Phase 2: 6 tests (group support)
- Phase 3: 4 tests (fragmented styles)
- **Total: 14 tests** - all must pass
