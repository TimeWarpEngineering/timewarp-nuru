# Split key-binding-builder.cs into separate files and reduce duplication

## Description

The `key-binding-builder.cs` file (585 lines) contains 3 separate classes that should each have their own file per .NET conventions. Additionally, `NestedKeyBindingBuilder<TParent>` duplicates all methods from `KeyBindingBuilder` via delegation, which should be refactored to reduce code duplication.

**Location:** `source/timewarp-nuru-repl/key-bindings/key-binding-builder.cs`

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

### File Splitting
- [x] Extract `KeyBindingResult` to `key-binding-result.cs` (~38 lines)
- [x] Keep `KeyBindingBuilder` in `key-binding-builder.cs` (~308 lines)
- [x] Extract `NestedKeyBindingBuilder<TParent>` to `nested-key-binding-builder.cs` (~235 lines)

### Duplication Reduction
- [x] Create `IKeyBindingBuilder` interface with common methods
- [x] Have `KeyBindingBuilder` implement the interface
- [x] Refactor `NestedKeyBindingBuilder<TParent>` to expose the interface or use composition
- [x] Consider CRTP pattern with `KeyBindingBuilderBase<TSelf>` if appropriate

### Verification
- [x] All existing tests pass
- [x] Build succeeds
- [x] No breaking API changes

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

## Results

### Files Created/Modified

| File | Lines | Description |
|------|-------|-------------|
| `key-binding-result.cs` | 38 | New file - `KeyBindingResult` class |
| `ikey-binding-builder.cs` | 137 | New file - `IKeyBindingBuilder<TSelf>` interface with CRTP |
| `key-binding-builder.cs` | 186 | Modified - implements interface, uses `<inheritdoc />` |
| `nested-key-binding-builder.cs` | 149 | New file - implements same interface |

### Approach Taken

Used **CRTP (Curiously Recurring Template Pattern)** via `IKeyBindingBuilder<TSelf>`:
- Interface defines all common methods with `TSelf` return type
- Both `KeyBindingBuilder` and `NestedKeyBindingBuilder<TParent>` implement the interface
- Documentation duplication reduced via `<inheritdoc />` on implementing methods

### Line Count Reduction

- **Before:** 585 lines in single file
- **After:** 510 lines total across 4 files (186 + 137 + 38 + 149)
- **Net reduction:** 75 lines (13% reduction)
- **Documentation reuse:** Interface XML docs inherited by both implementations

### Test Results

All 26 key binding tests pass:
- `KeyBindingBuilder` tests: 14 passed
- `CustomKeyBindingProfile` tests: 12 passed
