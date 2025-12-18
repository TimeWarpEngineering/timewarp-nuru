# Unify Builders with IBuilder Pattern

## Description

Establish a two-interface builder pattern and apply it consistently across all Nuru builders:

- **`IBuilder<TBuilt>`** - Top-level builders that create objects via `Build()`
- **`INestedBuilder<TParent>`** - Nested builders that return to parent via `Done()`

Nested builders use **composition** - they wrap a standalone builder internally. This avoids code duplication while keeping clear, distinct API surfaces.

**No backward compatibility concerns** - we're in beta, prioritize the best API.

## Background

Task 158 introduced `IBuilder<TParent>` (now `INestedBuilder<TParent>`) and `EndpointBuilder<TBuilder>` to fix fluent API chaining. This pattern should be extended to all builders for consistency.

## Interface Design

```csharp
// Top-level builder - creates TBuilt
public interface IBuilder<out TBuilt>
{
    TBuilt Build();
}

// Nested builder - returns to TParent (wraps standalone builder internally)
public interface INestedBuilder<out TParent> where TParent : class
{
    TParent Done();  // Done() = Build() + pass to parent + return parent
}
```

## Builder Inventory

| Standalone Builder    | Builds         | Nested Builder                    | Interface(s)                                  | Task   |
|-----------------------|----------------|-----------------------------------|-----------------------------------------------|--------|
| `NuruCoreAppBuilder`  | `NuruCoreApp`  | N/A (is top-level)                | `IBuilder<NuruCoreApp>`                       | 161    |
| `NuruAppBuilder`      | `NuruApp`      | N/A (is top-level)                | `IBuilder<NuruApp>`                           | 161    |
| `CompiledRouteBuilder`| `CompiledRoute`| `NestedCompiledRouteBuilder<T>`   | `IBuilder<CompiledRoute>`, `INestedBuilder<T>`| 161    |
| `TableBuilder`        | `Table`        | `NestedTableBuilder<T>`           | `IBuilder<Table>`, `INestedBuilder<T>`        | 162    |
| `PanelBuilder`        | `Panel`        | `NestedPanelBuilder<T>`           | `IBuilder<Panel>`, `INestedBuilder<T>`        | 162    |
| `RuleBuilder`         | `Rule`         | `NestedRuleBuilder<T>`            | `IBuilder<Rule>`, `INestedBuilder<T>`         | 162    |
| `KeyBindingBuilder`   | bindings tuple | `NestedKeyBindingBuilder<T>`      | `IBuilder<...>`, `INestedBuilder<T>`          | 163    |
| N/A                   | -              | `EndpointBuilder<T>`              | `INestedBuilder<T>` (mutating, no standalone) | Done   |

## Subtasks

- **Task 161**: Establish interfaces + apply to CompiledRouteBuilder (in-progress)
- **Task 162**: Apply to Widget Builders (Table, Panel, Rule)
- **Task 163**: Apply to KeyBindingBuilder

## Checklist

- [x] Task 158: Introduce initial `IBuilder<TParent>` pattern (now `INestedBuilder`)
- [x] Task 164: Rename builders to match what they build
- [ ] Task 161: Create `IBuilder<TBuilt>` + rename to `INestedBuilder<TParent>` + apply to CompiledRouteBuilder
- [ ] Task 162: Apply to Widget Builders
- [ ] Task 163: Apply to KeyBindingBuilder
- [ ] Update documentation with unified pattern examples

## Benefits

1. **Clear separation** - `Build()` creates, `Done()` returns to parent
2. **No code duplication** - nested builders wrap standalone via composition
3. **Consistent API** - every builder follows same pattern
4. **Type safety** - generics preserve types through entire chain
5. **IntelliSense** - discoverable API, IDE shows available methods

## Naming Convention

| Type       | Pattern                              | Example                              |
|------------|--------------------------------------|--------------------------------------|
| Standalone | `{Thing}Builder`                     | `CompiledRouteBuilder`               |
| Nested     | `Nested{Thing}Builder<TParent>`      | `NestedCompiledRouteBuilder<TParent>`|

## Example: Unified Fluent API

```csharp
// App configuration with nested builders
NuruApp.CreateBuilder()
    .Map("status", () => "OK").AsQuery().Done()
    .Map(r => r                                    // NestedCompiledRouteBuilder<EndpointBuilder>
        .WithLiteral("deploy")
        .WithParameter("env")
        .WithOption("force", "f")
        .Done()                                    // Returns EndpointBuilder
    )
    .WithHandler(handler)
    .AsCommand()
    .Done()                                        // Returns app builder
    .Build();

// Standalone usage (tests, generators, static routes)
CompiledRoute route = new CompiledRouteBuilder()
    .WithLiteral("deploy")
    .WithParameter("env")
    .Build();
```

## Notes

### Composition Pattern

Nested builders wrap standalone builders internally:

```csharp
// Nested wraps standalone - no code duplication
public sealed class NestedCompiledRouteBuilder<TParent> : INestedBuilder<TParent>
{
    private readonly CompiledRouteBuilder _inner = new();  // Composition
    private readonly TParent _parent;
    private readonly Action<CompiledRoute> _onBuild;

    public NestedCompiledRouteBuilder<TParent> WithLiteral(string value)
    {
        _inner.WithLiteral(value);  // Delegate to inner
        return this;
    }

    public TParent Done()
    {
        CompiledRoute route = _inner.Build();  // Build via inner
        _onBuild(route);                        // Pass to parent
        return _parent;                         // Return to parent
    }
}
```

### Pattern Types

Both standalone and nested builders can be either:
- **Factory** - accumulates state, creates new object at `Build()`/`Done()`
- **Mutating** - configures existing object, returns to parent at `Done()`

| Builder             | Pattern  | What happens at terminal method                    |
|---------------------|----------|---------------------------------------------------|
| `CompiledRouteBuilder` | Factory  | `Build()` creates new `CompiledRoute`              |
| `NestedCompiledRouteBuilder<T>` | Factory | `Done()` creates route + returns to parent |
| `EndpointBuilder<T>` | Mutating | `Done()` returns to parent (endpoint already exists) |

## Parent

148-nuru-3-unified-route-pipeline
