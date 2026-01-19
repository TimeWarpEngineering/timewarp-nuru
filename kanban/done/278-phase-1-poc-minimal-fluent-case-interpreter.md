# Phase 1: POC - Minimal Fluent Case (Interpreter)

## Description

Create the foundational IR builders and DSL interpreter to handle the minimal fluent case. This proves the concept works before expanding to more complex scenarios.

## Parent

#277 Epic: Semantic DSL Interpreter with Mirrored IR Builders

## Scope

Support this minimal fluent case:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder([])
  .Map("ping")
    .WithHandler(() => "pong")
    .AsQuery()
    .Done()
  .Build();

await app.RunAsync(["ping"]);
```

## Checklist

### 1.1 Create `IrAppBuilder<TSelf>`

- [ ] Create file: `source/timewarp-nuru-analyzers/generators/ir-builders/ir-app-builder.cs`
- [ ] Implement CRTP pattern matching `NuruCoreAppBuilder<TSelf>`
- [ ] State fields: Name, Description, Routes, InterceptSites
- [ ] Method: `Map(pattern, segments)` → returns `IrRouteBuilder<TSelf>`
- [ ] Method: `WithName(string)` → returns `TSelf`
- [ ] Method: `WithDescription(string)` → returns `TSelf`
- [ ] Method: `AddInterceptSite(InterceptSiteModel)` → returns `TSelf`
- [ ] Method: `Build()` → returns `AppModel`
- [ ] Create non-generic convenience type `IrAppBuilder : IrAppBuilder<IrAppBuilder>`

### 1.2 Create `IrRouteBuilder<TParent>`

- [ ] Create file: `source/timewarp-nuru-analyzers/generators/ir-builders/ir-route-builder.cs`
- [ ] Constructor takes: parent, registerRoute callback
- [ ] Internal `RouteDefinitionBuilder` for accumulating route state
- [ ] Method: `WithHandler(HandlerDefinition)` → returns `IrRouteBuilder<TParent>`
- [ ] Method: `WithDescription(string)` → returns `IrRouteBuilder<TParent>`
- [ ] Method: `AsQuery()` → returns `IrRouteBuilder<TParent>`
- [ ] Method: `AsCommand()` → returns `IrRouteBuilder<TParent>`
- [ ] Method: `AsIdempotentCommand()` → returns `IrRouteBuilder<TParent>`
- [ ] Method: `Done()` → registers route with parent, returns `TParent`

### 1.3 Create `DslInterpreter`

- [ ] Create file: `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`
- [ ] Constructor takes: `SemanticModel`, `CancellationToken`
- [ ] Field: `Dictionary<ISymbol, object> VariableState` for tracking builder instances
- [ ] Method: `Interpret(InvocationExpressionSyntax createBuilderCall)` → returns `AppModel`
- [ ] Internal: Find containing block/statements from `CreateBuilder()` call
- [ ] Internal: `StepThroughStatements(IEnumerable<StatementSyntax>)`
- [ ] Internal: `EvaluateExpression(ExpressionSyntax)` → returns IR builder or null
- [ ] Internal: `DispatchMethodCall(InvocationExpressionSyntax, object? receiver)` → returns new builder state
- [ ] Handle `LocalDeclarationStatementSyntax` - evaluate initializer, store in VariableState
- [ ] Handle `ExpressionStatementSyntax` - evaluate expression
- [ ] Handle fluent chains - walk `MemberAccessExpressionSyntax` to unroll
- [ ] Use `SemanticModel.GetSymbolInfo()` to get `IMethodSymbol` for each call
- [ ] Dispatch to IR builder methods based on method name
- [ ] Throw `InvalidOperationException` on unrecognized DSL methods (fail fast)

### 1.4 Method Dispatching

Dispatcher logic for Phase 1 methods:

| DSL Method | Receiver Type | Action | Returns |
|------------|---------------|--------|---------|
| `CreateBuilder` | static | Create `IrAppBuilder` | `IrAppBuilder` |
| `Map` | `IrAppBuilder` | Call `Map()`, extract pattern via `PatternStringExtractor` | `IrRouteBuilder` |
| `WithHandler` | `IrRouteBuilder` | Extract handler via `HandlerExtractor`, call `WithHandler()` | `IrRouteBuilder` |
| `WithDescription` | `IrRouteBuilder` | Extract string arg, call `WithDescription()` | `IrRouteBuilder` |
| `AsQuery` | `IrRouteBuilder` | Call `AsQuery()` | `IrRouteBuilder` |
| `AsCommand` | `IrRouteBuilder` | Call `AsCommand()` | `IrRouteBuilder` |
| `AsIdempotentCommand` | `IrRouteBuilder` | Call `AsIdempotentCommand()` | `IrRouteBuilder` |
| `Done` | `IrRouteBuilder` | Call `Done()` | parent (`IrAppBuilder`) |
| `Build` | `IrAppBuilder` | Mark app as built, continue for `RunAsync` | `NuruCoreApp` marker |
| `RunAsync` | `NuruCoreApp` marker | Extract intercept site, call `AddInterceptSite()` | int (ignore) |

### 1.5 Create Test

- [ ] Create file: `tests/timewarp-nuru-analyzers-tests/interpreter/temp-interpreter-poc-test.cs`
- [ ] Test: Minimal fluent chain produces correct `AppModel`
- [ ] Assert: `AppModel` has exactly one route
- [ ] Assert: Route pattern is "ping"
- [ ] Assert: Route message type is "Query"
- [ ] Assert: Handler is captured correctly
- [ ] Assert: `AppModel` has exactly one intercept site

### 1.6 Verify Build

- [ ] Build `timewarp-nuru-analyzers` without errors
- [ ] Run POC test, verify it passes

## Files to Create

| File | Purpose |
|------|---------|
| `generators/ir-builders/ir-app-builder.cs` | CRTP app builder mirroring DSL |
| `generators/ir-builders/ir-route-builder.cs` | Route builder mirroring DSL |
| `generators/interpreter/dsl-interpreter.cs` | Semantic DSL interpreter |
| `tests/.../interpreter/temp-interpreter-poc-test.cs` | POC test |

## Technical Notes

### Statement Navigation

Use `BlockSyntax.Statements` to iterate through statements in order:

```csharp
BlockSyntax block = GetContainingBlock(createBuilderCall);
foreach (StatementSyntax statement in block.Statements)
{
  Step(statement);
}
```

### Getting Method Symbol

```csharp
SymbolInfo symbolInfo = SemanticModel.GetSymbolInfo(invocation, CancellationToken);
if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
{
  string methodName = methodSymbol.Name;
  ITypeSymbol? receiverType = methodSymbol.ReceiverType;
  // Dispatch based on methodName and receiverType
}
```

### Fluent Chain Unrolling

Fluent chains are nested in syntax. To process `a.B().C().D()`:

```csharp
// D() contains C() contains B() contains a
// Walk down via MemberAccessExpression.Expression to unroll
List<InvocationExpressionSyntax> calls = UnrollFluentChain(invocation);
// calls = [a.B(), a.B().C(), a.B().C().D()] in execution order
```

### Variable Tracking

```csharp
// When we see: var builder = NuruApp.CreateBuilder([]);
LocalDeclarationStatementSyntax localDecl = ...;
VariableDeclaratorSyntax declarator = localDecl.Declaration.Variables[0];
ISymbol? symbol = SemanticModel.GetDeclaredSymbol(declarator);
object? value = EvaluateExpression(declarator.Initializer.Value);
VariableState[symbol] = value;
```

## Success Criteria

1. POC test passes
2. `IrAppBuilder` correctly accumulates routes
3. `IrRouteBuilder` correctly builds `RouteDefinition`
4. Interpreter correctly walks fluent chain
5. Handler is extracted and included in route
6. Intercept site is captured from `RunAsync()`
