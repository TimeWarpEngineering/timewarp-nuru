# Split key-binding-builder.cs into separate files and reduce duplication

## Description

The `key-binding-builder.cs` file (585 lines) contains 3 separate classes that should each have their own file per .NET conventions. Additionally, `NestedKeyBindingBuilder<TParent>` duplicates all methods from `KeyBindingBuilder` via delegation, which should be refactored to reduce code duplication.

**Location:** `source/timewarp-nuru-repl/key-bindings/key-binding-builder.cs`

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

### File Splitting
- [ ] Extract `KeyBindingResult` to `key-binding-result.cs` (~38 lines)
- [ ] Keep `KeyBindingBuilder` in `key-binding-builder.cs` (~308 lines)
- [ ] Extract `NestedKeyBindingBuilder<TParent>` to `nested-key-binding-builder.cs` (~235 lines)

### Duplication Reduction
- [ ] Create `IKeyBindingBuilder` interface with common methods
- [ ] Have `KeyBindingBuilder` implement the interface
- [ ] Refactor `NestedKeyBindingBuilder<TParent>` to expose the interface or use composition
- [ ] Consider CRTP pattern with `KeyBindingBuilderBase<TSelf>` if appropriate

### Verification
- [ ] All existing tests pass
- [ ] Build succeeds
- [ ] No breaking API changes

## Notes

### Current Structure

```
key-binding-builder.cs (585 lines)
├── KeyBindingResult (lines 1-38) - Result container with deconstruction
├── KeyBindingBuilder (lines 40-348) - Main builder class
└── NestedKeyBindingBuilder<TParent> (lines 350-584) - Generic nested builder
```

### Duplication Problem

`NestedKeyBindingBuilder<TParent>` contains methods like:
```csharp
public NestedKeyBindingBuilder<TParent> Bind(ConsoleKeyInfo key, Func<ReplConsoleReader, Task> handler)
{
    _inner.Bind(key, handler);  // Delegates to inner
    return this;
}
```

This pattern is repeated for every method, creating ~200 lines of boilerplate delegation.

### Refactoring Options

1. **Interface approach**: Extract `IKeyBindingBuilder` and have nested builder return `this` cast to interface
2. **Base class with CRTP**: `KeyBindingBuilderBase<TSelf>` where methods return `TSelf`
3. **Extension methods**: Move common operations to extensions on an interface

### Reference Pattern

See `endpoint-resolver.cs` family for established partial class conventions in this codebase.
