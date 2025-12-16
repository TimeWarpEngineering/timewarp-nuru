# Apply IBuilder to KeyBindingBuilder

## Description

Make `KeyBindingBuilder` implement `IBuilder<ReplOptions>` to enable fluent configuration of REPL key bindings nested within REPL options.

## Parent

160-unify-builders-with-ibuilder-pattern

## Current API

```csharp
// Current: separate configuration
app.AddReplSupport(options =>
{
    options.Prompt = "nuru> ";
    options.HistorySize = 100;
});

// KeyBindingBuilder used separately
var bindings = new KeyBindingBuilder()
    .Bind(ConsoleKey.F1, ShowHelp)
    .Bind(ConsoleKey.F5, Refresh)
    .Build();
```

## Target API

```csharp
// Nested fluent configuration
app.AddReplSupport(options => options
    .WithPrompt("nuru> ")
    .WithHistorySize(100)
    .WithKeyBindings(kb => kb           // KeyBindingBuilder<ReplOptions>
        .Bind(ConsoleKey.F1, ShowHelp)
        .Bind(ConsoleKey.F5, Refresh)
        .Bind(ConsoleKey.Escape, Cancel)
        .Done())                         // Returns to ReplOptions
    .WithCompletionStyle(CompletionStyle.Inline));

// Or chained
app.AddReplSupport()
    .WithPrompt("nuru> ")
    .WithKeyBindings(kb => kb
        .Bind(ConsoleKey.F1, ShowHelp)
        .Done())
    .Done()                              // Returns to app builder
    .AddAutoHelp()
    .Build();
```

## Checklist

### KeyBindingBuilder
- [ ] Create `KeyBindingBuilder<TParent>` implementing `IBuilder<TParent>`
- [ ] Keep non-generic `KeyBindingBuilder` for standalone usage
- [ ] Add `Done()` method that applies bindings and returns parent
- [ ] Ensure `Bind()` returns `KeyBindingBuilder<TParent>` for chaining

### ReplOptions
- [ ] Make `ReplOptions` fluent with `WithX()` methods
- [ ] Add `WithKeyBindings(Action<KeyBindingBuilder<ReplOptions>>)` method
- [ ] Consider making `ReplOptions` implement `IBuilder<TBuilder>` for nested REPL config
- [ ] Add `WithPrompt()`, `WithHistorySize()`, etc. fluent methods

### Integration
- [ ] Update `AddReplSupport()` to support fluent `ReplOptions`
- [ ] Ensure backward compatibility with action-based configuration
- [ ] Consider `AddReplSupport()` returning `ReplOptionsBuilder<TBuilder>` for full fluent chain

### Testing
- [ ] Test standalone `KeyBindingBuilder` still works
- [ ] Test nested key binding configuration
- [ ] Test full fluent chain from app builder through REPL options
- [ ] Test key bindings are properly applied at runtime

### Documentation
- [ ] Update XML docs
- [ ] Add examples showing fluent REPL configuration

## Notes

### Pattern: Mutating

`KeyBindingBuilder` uses the **Mutating** pattern:
- `Bind()` methods accumulate bindings and return `this`
- `Done()` applies bindings to parent and returns parent

### ReplOptions Fluent Design

Consider making `ReplOptions` itself a builder:

```csharp
public class ReplOptionsBuilder<TBuilder> : IBuilder<TBuilder>
    where TBuilder : NuruCoreAppBuilder
{
    public ReplOptionsBuilder<TBuilder> WithPrompt(string prompt) { ... }
    public ReplOptionsBuilder<TBuilder> WithHistorySize(int size) { ... }
    public ReplOptionsBuilder<TBuilder> WithKeyBindings(Action<KeyBindingBuilder<ReplOptionsBuilder<TBuilder>>> configure) { ... }
    public TBuilder Done() { ... }
}
```

This enables:
```csharp
app.ConfigureRepl()                      // Returns ReplOptionsBuilder<NuruAppBuilder>
    .WithPrompt("nuru> ")
    .WithKeyBindings(kb => kb.Bind(...).Done())
    .Done()                              // Returns NuruAppBuilder
    .AddAutoHelp();
```

### Key Binding Actions

Key bindings bind `ConsoleKey` (with modifiers) to actions:

```csharp
kb.Bind(ConsoleKey.F1, ShowHelp)                           // Simple
kb.Bind(ConsoleKey.S, ctrl: true, SaveCommand)             // Ctrl+S
kb.Bind(ConsoleKey.Tab, CompleteCommand)                   // Tab completion override
kb.BindSequence("gc", GitCommit)                           // Multi-key sequence (vim-style)
```

### Backward Compatibility

Keep supporting the current action-based API:

```csharp
// Still works
app.AddReplSupport(options =>
{
    options.Prompt = "nuru> ";
});

// New fluent API (preferred)
app.AddReplSupport()
    .WithPrompt("nuru> ")
    .Done();
```
