# Apply IBuilder to Widget Builders

## Description

Make `TableBuilder`, `PanelBuilder`, and `RuleBuilder` implement `IBuilder<TParent>` to enable composable terminal output with nested fluent chaining.

## Parent

160-unify-builders-with-ibuilder-pattern

## Current API

```csharp
// TableBuilder - standalone
terminal.WriteTable(t => t
    .AddColumn("Name")
    .AddColumn("Value")
    .AddRow("foo", "bar"));

// PanelBuilder - standalone
terminal.WritePanel(p => p
    .WithTitle("Status")
    .WithContent("OK"));

// RuleBuilder - standalone
terminal.WriteRule(r => r
    .WithTitle("Section"));
```

## Target API

```csharp
// Chainable terminal output
terminal
    .Panel(p => p
        .WithTitle("Results")
        .Done())
    .Rule(r => r
        .WithTitle("Details")
        .Done())
    .Table(t => t
        .AddColumn("Name")
        .AddRow("foo", "bar")
        .Done());

// Nested builders (Table inside Panel)
terminal.Panel(p => p
    .WithTitle("Summary")
    .WithTable(t => t                    // TableBuilder nested in PanelBuilder
        .AddColumn("Metric")
        .AddColumn("Value")
        .AddRow("CPU", "45%")
        .Done())                          // Returns to PanelBuilder
    .WithContent("Additional notes")
    .Done());                             // Returns to terminal
```

## Checklist

### TableBuilder
- [ ] Create `TableBuilder<TParent>` implementing `IBuilder<TParent>`
- [ ] Keep non-generic `TableBuilder` for standalone usage
- [ ] Add `Done()` method that renders and returns parent
- [ ] Update `ITerminal` with chainable `Table()` method

### PanelBuilder
- [ ] Create `PanelBuilder<TParent>` implementing `IBuilder<TParent>`
- [ ] Keep non-generic `PanelBuilder` for standalone usage
- [ ] Add `Done()` method that renders and returns parent
- [ ] Add `WithTable(Action<TableBuilder<PanelBuilder<TParent>>>)` for nesting
- [ ] Update `ITerminal` with chainable `Panel()` method

### RuleBuilder
- [ ] Create `RuleBuilder<TParent>` implementing `IBuilder<TParent>`
- [ ] Keep non-generic `RuleBuilder` for standalone usage
- [ ] Add `Done()` method that renders and returns parent
- [ ] Update `ITerminal` with chainable `Rule()` method

### ITerminal Extensions
- [ ] Add `Panel<T>()` extension returning `PanelBuilder<T>`
- [ ] Add `Table<T>()` extension returning `TableBuilder<T>`
- [ ] Add `Rule<T>()` extension returning `RuleBuilder<T>`
- [ ] Ensure chaining works: `terminal.Panel(...).Rule(...).Table(...)`

### Testing
- [ ] Test standalone builders still work
- [ ] Test terminal chaining: `terminal.Panel().Rule().Table()`
- [ ] Test nested builders: `Panel` containing `Table`
- [ ] Test `Done()` properly renders output

### Documentation
- [ ] Update XML docs
- [ ] Add examples showing nested widget composition

## Notes

### Pattern: Mutating

Widget builders use the **Mutating** pattern:
- `WithX()` methods modify internal state and return `this`
- `Done()` renders the widget and returns to parent

### Render on Done()

The `Done()` method should:
1. Build the widget (table/panel/rule)
2. Render it to the terminal
3. Return the parent for continued chaining

```csharp
public TParent Done()
{
    Render();  // Output to terminal
    return _parent;
}
```

### Nesting Complexity

Nested builders (Table inside Panel) require:
- Parent reference stored in child builder
- Child `Done()` returns to parent builder (not terminal)
- Content accumulation in parent

Example flow:
```
terminal.Panel(p => p           →  PanelBuilder<ITerminal>
    .WithTable(t => t           →  TableBuilder<PanelBuilder<ITerminal>>
        .AddColumn("X")
        .Done())                →  PanelBuilder<ITerminal>  (table content stored)
    .Done())                    →  ITerminal (panel rendered with table inside)
```
