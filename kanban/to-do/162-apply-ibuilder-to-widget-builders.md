# Apply IBuilder Pattern to Widget Builders

## Description

Apply the two-interface builder pattern to widget builders (`TableBuilder`, `PanelBuilder`, `RuleBuilder`):

- Add `IBuilder<T>` to standalone builders
- Create `Nested{Widget}Builder<TParent>` wrappers implementing `INestedBuilder<TParent>`

This enables composable terminal output with nested fluent chaining.

## Parent

160-unify-builders-with-ibuilder-pattern

## Dependencies

- Task 161: Must be complete (establishes `IBuilder<T>` and `INestedBuilder<T>` interfaces)

## Current API

```csharp
// Standalone builders with Build()
var table = new TableBuilder()
    .AddColumn("Name")
    .AddColumn("Value")
    .AddRow("foo", "bar")
    .Build();

var panel = new PanelBuilder()
    .Header("Status")
    .Content("OK")
    .Build();

var rule = new RuleBuilder()
    .Title("Section")
    .Build();

// Terminal writes standalone widgets
terminal.WriteTable(table);
terminal.WritePanel(panel);
terminal.WriteRule(rule);
```

## Target API

```csharp
// Standalone (unchanged, but now with IBuilder<T> interface)
Table table = new TableBuilder()
    .AddColumn("Name")
    .Build();  // IBuilder<Table>.Build()

// Chainable terminal output via nested builders
terminal
    .Panel(p => p
        .Header("Results")
        .Done())                                // NestedPanelBuilder<ITerminal>
    .Rule(r => r
        .Title("Details")
        .Done())                                // NestedRuleBuilder<ITerminal>
    .Table(t => t
        .AddColumn("Name")
        .AddRow("foo", "bar")
        .Done());                               // NestedTableBuilder<ITerminal>

// Nested builders (Table inside Panel)
terminal.Panel(p => p
    .Header("Summary")
    .WithTable(t => t                           // NestedTableBuilder<NestedPanelBuilder<ITerminal>>
        .AddColumn("Metric")
        .AddColumn("Value")
        .AddRow("CPU", "45%")
        .Done())                                // Returns to NestedPanelBuilder
    .Content("Additional notes")
    .Done());                                   // Returns to terminal
```

## Design

### Composition Pattern

Each nested builder wraps its standalone counterpart:

```csharp
// Standalone - full implementation
public sealed class TableBuilder : IBuilder<Table>
{
    private readonly Table _table = new();
    
    public TableBuilder AddColumn(string header) { ... return this; }
    public TableBuilder AddRow(params string[] cells) { ... return this; }
    public Table Build() => _table;
}

// Nested - wraps standalone
public sealed class NestedTableBuilder<TParent> : INestedBuilder<TParent>
    where TParent : class
{
    private readonly TableBuilder _inner = new();
    private readonly TParent _parent;
    private readonly Action<Table> _onBuild;

    internal NestedTableBuilder(TParent parent, Action<Table> onBuild)
    {
        _parent = parent;
        _onBuild = onBuild;
    }

    public NestedTableBuilder<TParent> AddColumn(string header)
    {
        _inner.AddColumn(header);
        return this;
    }

    public NestedTableBuilder<TParent> AddRow(params string[] cells)
    {
        _inner.AddRow(cells);
        return this;
    }

    public TParent Done()
    {
        Table table = _inner.Build();
        _onBuild(table);
        return _parent;
    }
}
```

## Checklist

### TableBuilder
- [ ] Add `IBuilder<Table>` to existing `TableBuilder`
- [ ] Create `NestedTableBuilder<TParent>` implementing `INestedBuilder<TParent>`
- [ ] Use composition - wrap `TableBuilder` internally
- [ ] Add wrapper methods for all fluent methods

### PanelBuilder
- [ ] Add `IBuilder<Panel>` to existing `PanelBuilder`
- [ ] Create `NestedPanelBuilder<TParent>` implementing `INestedBuilder<TParent>`
- [ ] Use composition - wrap `PanelBuilder` internally
- [ ] Add `WithTable()` method for nested table building
- [ ] Add wrapper methods for all fluent methods

### RuleBuilder
- [ ] Add `IBuilder<Rule>` to existing `RuleBuilder`
- [ ] Create `NestedRuleBuilder<TParent>` implementing `INestedBuilder<TParent>`
- [ ] Use composition - wrap `RuleBuilder` internally
- [ ] Add wrapper methods for all fluent methods

### ITerminal Extensions
- [ ] Add `Panel(Func<NestedPanelBuilder<ITerminal>, ITerminal>)` extension
- [ ] Add `Table(Func<NestedTableBuilder<ITerminal>, ITerminal>)` extension
- [ ] Add `Rule(Func<NestedRuleBuilder<ITerminal>, ITerminal>)` extension
- [ ] Ensure chaining works: `terminal.Panel(...).Rule(...).Table(...)`

### Testing
- [ ] Test standalone builders still work with `Build()`
- [ ] Test terminal chaining: `terminal.Panel().Rule().Table()`
- [ ] Test nested builders: `Panel` containing `Table`
- [ ] Test `Done()` properly renders output

### Documentation
- [ ] Update XML docs for all builders
- [ ] Add examples showing nested widget composition

## Notes

### Pattern: Factory

Widget builders use the **Factory** pattern:
- Accumulate state via `AddX()`/`WithX()` methods
- `Build()` or `Done()` creates the widget

### Render on Done()

For terminal extensions, `Done()` should:
1. Build the widget via `_inner.Build()`
2. Render it to the terminal via callback
3. Return the parent for continued chaining

```csharp
public TParent Done()
{
    Table table = _inner.Build();
    _onBuild(table);  // Callback renders to terminal
    return _parent;
}
```

### Nesting Flow

```
terminal.Panel(p => p              // NestedPanelBuilder<ITerminal>
    .Header("Summary")
    .WithTable(t => t              // NestedTableBuilder<NestedPanelBuilder<ITerminal>>
        .AddColumn("X")
        .Done())                   // Returns NestedPanelBuilder<ITerminal>
    .Done())                       // Renders panel (with table), returns ITerminal
```
