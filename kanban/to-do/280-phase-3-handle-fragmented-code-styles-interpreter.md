# Phase 3: Handle Fragmented Code Styles (Interpreter)

## Description

Enhance the interpreter to handle code where the consumer uses temporary variables and separates fluent chains across multiple statements. This is important because consumers may write equivalent code in different styles, and the interpreter should handle all of them.

## Parent

#277 Epic: Semantic DSL Interpreter with Mirrored IR Builders

## Depends On

#278 Phase 1: POC - Minimal Fluent Case

## Scope

Support these equivalent code styles:

```csharp
// Style 1: Pure fluent (already works from Phase 1)
NuruApp.CreateBuilder([])
  .Map("ping").WithHandler(() => "pong").Done()
  .Build();

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

### 3.1 Enhance Variable State Tracking

- [ ] Ensure `VariableState` dictionary properly tracks:
  - `ISymbol` (from `SemanticModel.GetDeclaredSymbol()`) → IR builder instance
- [ ] When evaluating `IdentifierNameSyntax`, lookup in `VariableState`
- [ ] Handle re-assignment (though unlikely in typical DSL usage)

### 3.2 Handle `LocalDeclarationStatementSyntax`

- [ ] Extract variable declarator
- [ ] Get symbol via `SemanticModel.GetDeclaredSymbol(declarator)`
- [ ] Evaluate initializer expression
- [ ] If result is an IR builder, store in `VariableState`
- [ ] If result is not an IR builder (e.g., `"Hi mom"`), skip/ignore

### 3.3 Handle `ExpressionStatementSyntax`

- [ ] Evaluate the expression
- [ ] If it's an invocation on a tracked builder, process it
- [ ] If result is a new builder state, update `VariableState` if assigned

### 3.4 Handle Non-Assignment Method Calls

For code like:
```csharp
endpoint.WithHandler(() => "pong");  // No assignment, but modifies state
```

- [ ] Detect method calls on tracked builders that return `this` (fluent)
- [ ] Update the builder state in place
- [ ] Note: Our IR builders are mutable and return `this`, so this should work naturally

### 3.5 Handle Identifier Resolution

- [ ] When evaluating `endpoint.WithHandler(...)`:
  - Get symbol for `endpoint` via `SemanticModel.GetSymbolInfo()`
  - Lookup in `VariableState`
  - If found, use that IR builder as receiver
  - If not found, fail fast (untracked builder reference)

### 3.6 Ignore Non-Builder Statements

- [ ] For statements that don't involve our builders, skip them
- [ ] Check if the expression involves any tracked symbols
- [ ] Examples to ignore:
  - `var junk = "Hi mom";` (string, not a builder)
  - `Console.WriteLine(junk);` (not on a tracked builder)
  - `int x = 5;` (primitive, not a builder)

### 3.7 Add Tests

- [ ] Create/update test file: `tests/timewarp-nuru-analyzers-tests/interpreter/temp-interpreter-fragmented-test.cs`
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
| `generators/interpreter/dsl-interpreter.cs` | Enhance variable tracking and statement handling |

## Files to Create

| File | Purpose |
|------|---------|
| `tests/.../interpreter/temp-interpreter-fragmented-test.cs` | Fragmented style tests |

## Technical Notes

### Variable Symbol Resolution

```csharp
// For: var endpoint = builder.Map("ping");
LocalDeclarationStatementSyntax localDecl = ...;
VariableDeclaratorSyntax declarator = localDecl.Declaration.Variables[0];

// Get the symbol for 'endpoint'
ISymbol? symbol = SemanticModel.GetDeclaredSymbol(declarator);

// Evaluate the initializer (builder.Map("ping"))
object? value = EvaluateExpression(declarator.Initializer.Value);

// Store if it's a builder
if (value is IIrBuilder irBuilder)
{
  VariableState[symbol] = value;
}
```

### Looking Up Variables

```csharp
// For: endpoint.WithHandler(...)
if (expression is IdentifierNameSyntax identifier)
{
  SymbolInfo symbolInfo = SemanticModel.GetSymbolInfo(identifier);
  if (symbolInfo.Symbol is not null && 
      VariableState.TryGetValue(symbolInfo.Symbol, out object? builder))
  {
    return builder;
  }
  
  // Unknown variable - might be non-builder, check type
  TypeInfo typeInfo = SemanticModel.GetTypeInfo(identifier);
  if (IsBuilderType(typeInfo.Type))
  {
    throw new InvalidOperationException($"Untracked builder variable: {identifier}");
  }
  
  return null; // Non-builder variable, ignore
}
```

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
2. Variable tracking correctly maps symbols to IR builders
3. Non-builder statements are ignored without error
4. Fluent calls on variables work correctly
5. Existing Phase 1 and Phase 2 tests still pass
6. Mixed fragmented + group styles work
