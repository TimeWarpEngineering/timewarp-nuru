# Phase 4: Additional DSL Methods (Interpreter)

## Description

Add support for all remaining DSL methods that aren't yet handled by the interpreter. This includes app-level configuration methods and any additional route-level methods.

## Parent

#277 Epic: Semantic DSL Interpreter with Mirrored IR Builders

## Depends On

#278 Phase 1: POC - Minimal Fluent Case

## Scope

Support all DSL methods used in `dsl-example.cs`:

```csharp
NuruApp.CreateBuilder(args)
  .AddConfiguration()
  .ConfigureServices(services => services
    .AddLogging(builder => builder.AddConsole())
    .AddSingleton<MyService>())
  .AddBehavior(typeof(TelemetryBehavior<,>))
  .AddBehavior(typeof(ValidationBehavior<,>))
  .UseTerminal(terminal)
  .AddHelp(options => { options.ShowPerCommandHelpRoutes = false; })
  .AddRepl(options => { options.Prompt = "my-app> "; })
  .WithName("my app")
  .WithDescription("Does Cool Things")
  .WithAiPrompt("Use queries before commands.")
  .Map("status")
    .WithHandler(...)
    .WithDescription("Check application status")
    .AsQuery()
    .Done()
  .Build();
```

## Checklist

### 4.1 App-Level Metadata Methods

Add to `IrAppBuilder`:

- [ ] `WithAiPrompt(string)` → sets AiPrompt, returns `TSelf`
- [ ] Update `Build()` to include AiPrompt in `AppModel`

Add dispatching:

- [ ] `WithAiPrompt` on `IrAppBuilder`

### 4.2 Help Configuration

Add to `IrAppBuilder`:

- [ ] Field: `HasHelp`, `HelpOptions`
- [ ] `AddHelp()` → sets HasHelp=true, uses defaults, returns `TSelf`
- [ ] `AddHelp(Action<HelpOptions>)` → sets HasHelp=true, applies options, returns `TSelf`
- [ ] Update `Build()` to include help config in `AppModel`

Add dispatching:

- [ ] `AddHelp` on `IrAppBuilder` (both overloads)
- [ ] Extract options lambda if present

### 4.3 REPL Configuration

Add to `IrAppBuilder`:

- [ ] Field: `HasRepl`, `ReplOptions`
- [ ] `AddRepl()` → sets HasRepl=true, uses defaults, returns `TSelf`
- [ ] `AddRepl(Action<ReplOptions>)` → sets HasRepl=true, applies options, returns `TSelf`
- [ ] Update `Build()` to include REPL config in `AppModel`

Add dispatching:

- [ ] `AddRepl` on `IrAppBuilder` (both overloads)
- [ ] Extract options lambda if present

### 4.4 Configuration

Add to `IrAppBuilder`:

- [ ] Field: `HasConfiguration`
- [ ] `AddConfiguration()` → sets HasConfiguration=true, returns `TSelf`
- [ ] Update `Build()` to include in `AppModel`

Add dispatching:

- [ ] `AddConfiguration` on `IrAppBuilder`

### 4.5 Service Registration (ConfigureServices)

Add to `IrAppBuilder`:

- [ ] Field: `Services` (collection of `ServiceDefinition`)
- [ ] `ConfigureServices(Action<IServiceCollection>)` → extracts services, returns `TSelf`
- [ ] Use existing `ServiceExtractor` to analyze the lambda body
- [ ] Update `Build()` to include services in `AppModel`

Add dispatching:

- [ ] `ConfigureServices` on `IrAppBuilder`
- [ ] Pass lambda to `ServiceExtractor`

### 4.6 Behaviors (Pipeline Middleware)

Add to `IrAppBuilder`:

- [ ] Field: `Behaviors` (collection of `BehaviorDefinition`)
- [ ] `AddBehavior(Type)` → extracts behavior type, returns `TSelf`
- [ ] Update `Build()` to include behaviors in `AppModel`

Add dispatching:

- [ ] `AddBehavior` on `IrAppBuilder`
- [ ] Extract the type argument (often `typeof(SomeBehavior<,>)`)

### 4.7 Terminal (Skip/Ignore)

- [ ] `UseTerminal(ITerminal)` → skip, runtime only
- [ ] Add to dispatcher as no-op, returns same builder

### 4.8 Route-Level Methods

Add if not already present:

- [ ] `WithAlias(string)` on `IrRouteBuilder` and `IrGroupRouteBuilder`
- [ ] Update `RouteDefinition` to include aliases

### 4.9 Update Tests

- [ ] Add tests for each new method
- [ ] Test: App with all metadata (name, description, aiPrompt)
- [ ] Test: App with help enabled
- [ ] Test: App with REPL enabled
- [ ] Test: App with configuration
- [ ] Test: App with services registered
- [ ] Test: App with behaviors
- [ ] Test: Full `dsl-example.cs` pattern

## Method Dispatching Updates

| DSL Method | Receiver Type | Action | Returns |
|------------|---------------|--------|---------|
| `WithAiPrompt` | `IrAppBuilder` | Set AiPrompt | `IrAppBuilder` |
| `AddHelp` | `IrAppBuilder` | Set HasHelp, extract options | `IrAppBuilder` |
| `AddRepl` | `IrAppBuilder` | Set HasRepl, extract options | `IrAppBuilder` |
| `AddConfiguration` | `IrAppBuilder` | Set HasConfiguration | `IrAppBuilder` |
| `ConfigureServices` | `IrAppBuilder` | Extract services via `ServiceExtractor` | `IrAppBuilder` |
| `AddBehavior` | `IrAppBuilder` | Extract behavior type | `IrAppBuilder` |
| `UseTerminal` | `IrAppBuilder` | No-op (runtime only) | `IrAppBuilder` |
| `WithAlias` | `IrRouteBuilder` | Add alias to route | `IrRouteBuilder` |

## Files to Modify

| File | Change |
|------|--------|
| `generators/ir-builders/ir-app-builder.cs` | Add all new methods and fields |
| `generators/ir-builders/ir-route-builder.cs` | Add `WithAlias()` if not present |
| `generators/ir-builders/ir-group-route-builder.cs` | Add `WithAlias()` if not present |
| `generators/interpreter/dsl-interpreter.cs` | Add dispatching for all new methods |

## Technical Notes

### Extracting Action Lambda Options

For `AddHelp(options => { options.ShowPerCommandHelpRoutes = false; })`:

```csharp
// Get the lambda argument
ArgumentSyntax arg = invocation.ArgumentList.Arguments[0];
if (arg.Expression is SimpleLambdaExpressionSyntax lambda)
{
  // Analyze lambda body for property assignments
  // This is for IR capture - we may just capture the lambda syntax
  // and let the emitter regenerate it
}
```

Alternatively, we can just capture that help/repl is enabled and generate default behavior. The specific options might be a Phase 5+ enhancement.

### Extracting typeof() Argument

For `AddBehavior(typeof(TelemetryBehavior<,>))`:

```csharp
ArgumentSyntax arg = invocation.ArgumentList.Arguments[0];
if (arg.Expression is TypeOfExpressionSyntax typeofExpr)
{
  TypeInfo typeInfo = SemanticModel.GetTypeInfo(typeofExpr.Type);
  ITypeSymbol? behaviorType = typeInfo.Type;
  string typeName = behaviorType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
```

### Using ServiceExtractor

The existing `ServiceExtractor` can analyze `ConfigureServices` lambdas:

```csharp
// Get the lambda argument
ArgumentSyntax arg = invocation.ArgumentList.Arguments[0];
if (arg.Expression is LambdaExpressionSyntax lambda)
{
  ImmutableArray<ServiceDefinition> services = 
    ServiceExtractor.ExtractFromLambda(lambda, SemanticModel, CancellationToken);
  
  foreach (ServiceDefinition service in services)
  {
    irAppBuilder.AddService(service);
  }
}
```

## Success Criteria

1. All additional method tests pass
2. `dsl-example.cs` pattern can be fully interpreted
3. `AppModel` includes all metadata (name, description, aiPrompt)
4. `AppModel` includes help and REPL configuration
5. `AppModel` includes services and behaviors
6. Existing Phase 1-3 tests still pass
