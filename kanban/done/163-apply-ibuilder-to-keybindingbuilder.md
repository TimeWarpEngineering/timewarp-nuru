# Apply IBuilder Pattern to KeyBindingBuilder

## Description

Apply the two-interface builder pattern to `KeyBindingBuilder`:

- Add `IBuilder<KeyBindingResult>` to standalone builder
- Create `NestedKeyBindingBuilder<TParent>` wrapper implementing `INestedBuilder<TParent>`

This enables fluent configuration of REPL key bindings nested within REPL options.

## Parent

160-unify-builders-with-ibuilder-pattern

## Dependencies

- Task 161: Must be complete (establishes `IBuilder<T>` and `INestedBuilder<T>` interfaces)

## Current API

```csharp
// Standalone builder
var (bindings, exitKeys) = new KeyBindingBuilder()
    .Bind(ConsoleKey.F1, ShowHelp)
    .Bind(ConsoleKey.F5, Refresh)
    .BindExit(ConsoleKey.Enter, Submit)
    .Build();

// REPL options configured separately
app.AddReplSupport(options =>
{
    options.Prompt = "nuru> ";
    options.HistorySize = 100;
});
```

## Target API

```csharp
// Standalone (unchanged, but now with IBuilder interface)
var result = new KeyBindingBuilder()
    .Bind(ConsoleKey.F1, ShowHelp)
    .Build();  // IBuilder<KeyBindingResult>.Build()

// Nested fluent configuration
app.AddReplSupport(options => options
    .WithPrompt("nuru> ")
    .WithHistorySize(100)
    .WithKeyBindings(kb => kb                   // NestedKeyBindingBuilder<ReplOptions>
        .Bind(ConsoleKey.F1, ShowHelp)
        .Bind(ConsoleKey.F5, Refresh)
        .BindExit(ConsoleKey.Escape, Cancel)
        .Done())                                 // Returns to ReplOptions
    .WithCompletionStyle(CompletionStyle.Inline));
```

## Design

### Result Type

Define a proper result type instead of tuple:

```csharp
public sealed class KeyBindingResult
{
    public Dictionary<(ConsoleKey, ConsoleModifiers), Action> Bindings { get; init; } = [];
    public HashSet<(ConsoleKey, ConsoleModifiers)> ExitKeys { get; init; } = [];
}
```

### Composition Pattern

```csharp
// Standalone - full implementation
public sealed class KeyBindingBuilder : IBuilder<KeyBindingResult>
{
    private readonly Dictionary<(ConsoleKey, ConsoleModifiers), Action> _bindings = [];
    private readonly HashSet<(ConsoleKey, ConsoleModifiers)> _exitKeys = [];

    public KeyBindingBuilder Bind(ConsoleKey key, Action action) { ... return this; }
    public KeyBindingBuilder Bind(ConsoleKey key, ConsoleModifiers modifiers, Action action) { ... return this; }
    public KeyBindingBuilder BindExit(ConsoleKey key, Action action) { ... return this; }
    public KeyBindingBuilder Remove(ConsoleKey key) { ... return this; }
    
    public KeyBindingResult Build() => new()
    {
        Bindings = new(_bindings),
        ExitKeys = [.. _exitKeys]
    };
}

// Nested - wraps standalone
public sealed class NestedKeyBindingBuilder<TParent> : INestedBuilder<TParent>
    where TParent : class
{
    private readonly KeyBindingBuilder _inner = new();
    private readonly TParent _parent;
    private readonly Action<KeyBindingResult> _onBuild;

    internal NestedKeyBindingBuilder(TParent parent, Action<KeyBindingResult> onBuild)
    {
        _parent = parent;
        _onBuild = onBuild;
    }

    public NestedKeyBindingBuilder<TParent> Bind(ConsoleKey key, Action action)
    {
        _inner.Bind(key, action);
        return this;
    }

    public NestedKeyBindingBuilder<TParent> BindExit(ConsoleKey key, Action action)
    {
        _inner.BindExit(key, action);
        return this;
    }

    public TParent Done()
    {
        KeyBindingResult result = _inner.Build();
        _onBuild(result);
        return _parent;
    }
}
```

## Checklist

### KeyBindingResult Type
- [x] Create `KeyBindingResult` class to replace tuple return
- [x] Migrate `Build()` to return `KeyBindingResult`

### KeyBindingBuilder (Standalone)
- [x] Add `IBuilder<KeyBindingResult>` interface
- [x] Update `Build()` return type to `KeyBindingResult`
- [x] Keep backward-compatible tuple deconstruction if needed

### NestedKeyBindingBuilder (New)
- [x] Create `NestedKeyBindingBuilder<TParent>` implementing `INestedBuilder<TParent>`
- [x] Use composition - wrap `KeyBindingBuilder` internally
- [x] Add wrapper methods for all `Bind*()` methods
- [x] Add wrapper methods for `Remove()`, `Clear()`, `LoadFrom()`, etc.

### ReplOptions Integration
- [ ] Make `ReplOptions` fluent with `WithX()` methods if not already (follow-up task)
- [ ] Add `WithKeyBindings(Func<NestedKeyBindingBuilder<ReplOptions>, ReplOptions>)` method (follow-up task)
- [ ] Ensure `Done()` applies bindings to REPL options (follow-up task)

### Testing
- [ ] Test standalone `KeyBindingBuilder` with `Build()` (existing tests cover this via deconstruction)
- [ ] Test nested key binding configuration in REPL options (follow-up task)
- [ ] Test key bindings are properly applied at runtime (existing tests cover this)

### Documentation
- [x] Update XML docs for both builder classes
- [ ] Add examples showing fluent REPL configuration (follow-up task)

## Notes

### Pattern: Factory

`KeyBindingBuilder` uses the **Factory** pattern:
- `Bind()` methods accumulate bindings
- `Build()` or `Done()` creates the result

### Backward Compatibility

Consider keeping tuple deconstruction support:

```csharp
public sealed class KeyBindingResult
{
    public Dictionary<...> Bindings { get; init; }
    public HashSet<...> ExitKeys { get; init; }
    
    // Enable tuple deconstruction for backward compat
    public void Deconstruct(
        out Dictionary<...> bindings,
        out HashSet<...> exitKeys)
    {
        bindings = Bindings;
        exitKeys = ExitKeys;
    }
}

// Still works:
var (bindings, exitKeys) = new KeyBindingBuilder().Bind(...).Build();
```

### ReplOptions Fluent Design

Consider making `ReplOptions` itself implement `INestedBuilder<TBuilder>`:

```csharp
public sealed class ReplOptionsBuilder<TBuilder> : INestedBuilder<TBuilder>
    where TBuilder : NuruCoreAppBuilder
{
    public ReplOptionsBuilder<TBuilder> WithPrompt(string prompt) { ... }
    public ReplOptionsBuilder<TBuilder> WithHistorySize(int size) { ... }
    public ReplOptionsBuilder<TBuilder> WithKeyBindings(
        Func<NestedKeyBindingBuilder<ReplOptionsBuilder<TBuilder>>, ReplOptionsBuilder<TBuilder>> configure) { ... }
    public TBuilder Done() { ... }
}
```

This enables:
```csharp
app.ConfigureRepl()                              // Returns ReplOptionsBuilder<NuruAppBuilder>
    .WithPrompt("nuru> ")
    .WithKeyBindings(kb => kb.Bind(...).Done())
    .Done()                                      // Returns NuruAppBuilder
    .AddAutoHelp();
```
