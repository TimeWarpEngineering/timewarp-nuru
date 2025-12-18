# Unify Builders with IBuilder Pattern

## Description

Apply the `IBuilder<TParent>` pattern consistently across all Nuru builders to create a unified, composable fluent API. This enables nested builder chains with `Done()` to return to the parent context.

**No backward compatibility concerns** - we're in beta, prioritize the best API.

## Background

Task 158 introduced `IBuilder<TParent>` and `EndpointBuilder<TBuilder>` (was `RouteConfigurator`) to fix fluent API chaining. Task 164 renamed builders to match what they build. This pattern should be extended to all builders in the codebase for consistency and composability.

## Current Builders

| Builder | Current State | Target Pattern |
|---------|---------------|----------------|
| `EndpointBuilder<T>` | Has `IBuilder<T>` | Done (Task 158, 164) |
| `RouteBuilder` | Standalone `Build()` | `IBuilder<TParent>` (Factory) - Task 161 |
| `TableBuilder` | Standalone | `IBuilder<TParent>` (Mutating) - Task 162 |
| `PanelBuilder` | Standalone | `IBuilder<TParent>` (Mutating) - Task 162 |
| `RuleBuilder` | Standalone | `IBuilder<TParent>` (Mutating) - Task 162 |
| `KeyBindingBuilder` | Standalone | `IBuilder<TParent>` (Mutating) - Task 163 |
| `RouteGroupBuilder` | Not yet exists | `IBuilder<TBuilder>` (Task 153) |

## Subtasks

- **Task 161**: Apply IBuilder to RouteBuilder (in-progress)
- **Task 162**: Apply IBuilder to Widget Builders (Table, Panel, Rule)
- **Task 163**: Apply IBuilder to KeyBindingBuilder

## Checklist

- [x] Task 158: Introduce `IBuilder<TParent>` pattern
- [x] Task 164: Rename builders to match what they build
- [ ] Task 161: Apply IBuilder to RouteBuilder
- [ ] Task 162: Apply IBuilder to Widget Builders
- [ ] Task 163: Apply IBuilder to KeyBindingBuilder
- [ ] Update documentation with unified pattern examples

## Benefits

1. **Consistent API** - Every nested builder has `Done()` to return to parent
2. **IntelliSense** - Clear navigation in IDE, discoverable API
3. **Composability** - Builders can nest arbitrarily deep
4. **Type Safety** - Generic `TParent` preserves types through chain

## Example: Unified Fluent API

```csharp
// App configuration with nested builders
NuruApp.CreateBuilder()
    .Map("status", () => "OK").AsQuery().Done()
    .Map(r => r                               // RouteBuilder<EndpointBuilder>
        .WithLiteral("deploy")
        .WithParameter("env")
        .WithOption("force", "f")
        .Done()                               // Returns EndpointBuilder
    )
    .WithHandler(handler)
    .AsCommand()
    .Done()                                   // Returns app builder
    .AddReplSupport(options => options
        .WithPrompt("nuru> ")
        .WithKeyBindings(kb => kb
            .Bind(ConsoleKey.F1, ShowHelp))
        .WithHistory(100))
    .Build();

// Terminal output with nested builders
terminal
    .Panel(p => p
        .WithTitle("Results")
        .WithTable(t => t
            .AddColumn("Name")
            .AddRow("foo", "bar")))
    .Rule(r => r.WithTitle("End"));
```

## Notes

### Design Decisions

- Builders that create new objects use **Factory** pattern (accumulate state, `Done()` builds and returns parent)
- Builders that configure existing objects use **Mutating** pattern (`WithX()` modifies and returns `this`)
- All nested builders implement `IBuilder<TParent>` with `Done()` returning to parent
- **No standalone builders** - use `IBuilder<TParent>` pattern exclusively (beta, no backward compat)

### Pattern from timewarp-fluent-builder

```csharp
public interface IBuilder<out TParent> where TParent : class
{
    TParent Done();
}

// Nested builder takes parent reference and callback
public sealed class RouteBuilder<TParent> : IBuilder<TParent>
{
    private readonly TParent _parent;
    private readonly Action<CompiledRoute> _onBuild;
    
    public TParent Done()
    {
        var route = BuildInternal();
        _onBuild(route);
        return _parent;
    }
}
```

## Parent

148-nuru-3-unified-route-pipeline
