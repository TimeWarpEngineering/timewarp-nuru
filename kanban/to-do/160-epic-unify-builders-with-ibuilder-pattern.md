# Unify Builders with IBuilder Pattern

## Description

Apply the `IBuilder<TParent>` pattern consistently across all Nuru builders to create a unified, composable fluent API. This enables nested builder chains with `Done()` to return to the parent context.

## Background

Task 158 introduced `IBuilder<TParent>` and `RouteConfigurator<TBuilder>` to fix fluent API chaining. This pattern should be extended to all builders in the codebase for consistency and composability.

## Current Builders

| Builder | Current State | Target Pattern |
|---------|---------------|----------------|
| `RouteConfigurator<T>` | Has `IBuilder<T>` | Done (Task 158) |
| `CompiledRouteBuilder` | Standalone `Build()` | `IBuilder<TBuilder>` (Factory) |
| `TableBuilder` | Standalone | `IBuilder<ITerminal>` (Mutating) |
| `PanelBuilder` | Standalone | `IBuilder<ITerminal>` (Mutating) |
| `RuleBuilder` | Standalone | `IBuilder<ITerminal>` (Mutating) |
| `KeyBindingBuilder` | Standalone | `IBuilder<ReplOptions>` (Mutating) |
| `RouteGroupBuilder` | Not yet exists | `IBuilder<TBuilder>` (Task 153) |

## Subtasks

This is broken into focused subtasks:

- **Task 161**: Apply IBuilder to CompiledRouteBuilder
- **Task 162**: Apply IBuilder to Widget Builders (Table, Panel, Rule)
- **Task 163**: Apply IBuilder to KeyBindingBuilder

## Benefits

1. **Consistent API** - Every nested builder has `Done()` to return to parent
2. **IntelliSense** - Clear navigation in IDE, discoverable API
3. **Composability** - Builders can nest arbitrarily deep
4. **Type Safety** - Generic `TParent` preserves types through chain
5. **Generator Support** - `fluent-builder` CLI can generate boilerplate

## Example: Unified Fluent API

```csharp
// App configuration with nested builders
NuruApp.CreateBuilder()
    .Map("status", () => "OK").AsQuery()
    .MapRoute(r => r                          // CompiledRouteBuilder
        .WithLiteral("deploy")
        .WithParameter("env")
        .WithOption("force", "f")
        .Done())                              // Returns to app builder
    .AddReplSupport(options => options
        .WithPrompt("nuru> ")
        .WithKeyBindings(kb => kb             // KeyBindingBuilder
            .Bind(ConsoleKey.F1, ShowHelp)
            .Done())                          // Returns to ReplOptions
        .WithHistory(100))
    .Build();

// Terminal output with nested builders
terminal
    .Panel(p => p                             // PanelBuilder
        .WithTitle("Results")
        .WithTable(t => t                     // TableBuilder nested in Panel
            .AddColumn("Name")
            .AddRow("foo", "bar")
            .Done())                          // Returns to PanelBuilder
        .Done())                              // Returns to terminal
    .Rule(r => r.WithTitle("End").Done());
```

## Checklist

- [ ] Create subtask 161: CompiledRouteBuilder
- [ ] Create subtask 162: Widget Builders
- [ ] Create subtask 163: KeyBindingBuilder
- [ ] Update documentation with unified pattern examples
- [ ] Consider using `fluent-builder` CLI to generate boilerplate

## Notes

### Design Decisions

- Builders that create new objects use **Factory** pattern (accumulate state, `Build()` creates instance)
- Builders that configure existing objects use **Mutating** pattern (`WithX()` modifies and returns `this`)
- All nested builders implement `IBuilder<TParent>` with `Done()` returning to parent
- Standalone usage still works (backward compatible)

### Generator Consideration

The `timewarp-fluent-builder-cli` tool supports both patterns:
- `BuilderPattern.Factory` - for `CompiledRouteBuilder`
- `BuilderPattern.Mutating` - for widget and config builders

Could potentially use `[FluentBuilder]` attribute to generate boilerplate, keeping custom methods in partial classes.

## Parent

148-nuru-3-unified-route-pipeline
