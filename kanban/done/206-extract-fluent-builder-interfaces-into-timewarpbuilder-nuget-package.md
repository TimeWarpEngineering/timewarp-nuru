# Extract fluent builder interfaces into TimeWarp.Builder NuGet package

## Description

Extract the generic fluent builder interfaces from `timewarp-nuru-core/fluent/` into a new standalone NuGet package `TimeWarp.Builder`. This enables other packages (TimeWarp.Terminal, etc.) to use these interfaces without depending on the full Nuru core.

**Prerequisite for:** Task 159 (Extract TimeWarp.Terminal)

## Checklist

### Project Setup
- [ ] Create `source/timewarp-builder/timewarp-builder.csproj`
- [ ] Configure NuGet package metadata (TimeWarp.Builder)
- [ ] Add to solution file `timewarp-nuru.slnx`
- [ ] Create `GlobalUsings.cs` with standard usings

### Move Files
- [ ] Move `fluent/i-builder.cs` (IBuilder<TBuilt> interface)
- [ ] Move `fluent/i-nested-builder.cs` (INestedBuilder<TParent> interface)
- [ ] Move `fluent/scope-extensions.cs` (scope helper extensions)

### Update Dependencies
- [ ] Add `timewarp-builder` as dependency to `timewarp-nuru-core`
- [ ] Update any internal references in timewarp-nuru-core
- [ ] Verify all builders still compile (CompiledRouteBuilder, EndpointBuilder, etc.)

### Testing
- [ ] Verify all existing tests still pass
- [ ] No new test project needed (interfaces only, tested via consuming code)

## Notes

### Files to Extract from `source/timewarp-nuru-core/fluent/`

- `i-builder.cs` - `IBuilder<TBuilt>` interface for standalone builders
- `i-nested-builder.cs` - `INestedBuilder<TParent>` interface for nested/fluent builders
- `scope-extensions.cs` - Extension methods for scoped building patterns

### Types that implement these interfaces (remain in their packages)

**In timewarp-nuru-core:**
- `CompiledRouteBuilder : IBuilder<CompiledRoute>`
- `NestedCompiledRouteBuilder<TParent> : INestedBuilder<TParent>`
- `EndpointBuilder<TBuilder> : INestedBuilder<TBuilder>`

**In timewarp-nuru-core/io/widgets (will move to TimeWarp.Terminal):**
- `PanelBuilder : IBuilder<Panel>`
- `RuleBuilder : IBuilder<Rule>`
- `TableBuilder : IBuilder<Table>`
- `NestedPanelBuilder<TParent> : INestedBuilder<TParent>`
- `NestedRuleBuilder<TParent> : INestedBuilder<TParent>`
- `NestedTableBuilder<TParent> : INestedBuilder<TParent>`

**In timewarp-nuru-repl:**
- `KeyBindingBuilder : IBuilder<KeyBindingResult>`
- `NestedKeyBindingBuilder<TParent> : INestedBuilder<TParent>`

### Namespace

Keep `TimeWarp.Nuru` namespace initially to minimize breaking changes. Can refactor to `TimeWarp.Builder` namespace in a separate task after extraction is stable.

### Dependency Graph After Extraction

```
TimeWarp.Builder (new)
    ^
    |
TimeWarp.Nuru.Core
    ^
    |
TimeWarp.Nuru.Repl
```
