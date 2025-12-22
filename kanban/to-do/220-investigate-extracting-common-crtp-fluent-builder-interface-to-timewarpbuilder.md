# Investigate extracting common CRTP fluent builder interface to TimeWarp.Builder

## Description

TimeWarp.Builder should capture all builder pattern variations used in the codebase. Currently, multiple builders use the CRTP (Curiously Recurring Template Pattern) for fluent method chaining, but there's no common interface for this pattern in TimeWarp.Builder.

This task investigates:
1. What CRTP-style builders exist in the codebase
2. Whether a common `IFluentBuilder<TSelf>` interface makes sense
3. How to apply a unified pattern across all builders

## Checklist

### Investigation
- [ ] Audit all builders in the codebase using CRTP pattern
- [ ] Identify common fluent methods that could be abstracted
- [ ] Determine if interface or abstract base class is more appropriate
- [ ] Review how `NuruCoreAppBuilder<TSelf>` uses CRTP (inheritance-based)
- [ ] Review how `IKeyBindingBuilder<TSelf>` uses CRTP (interface-based)

### Design
- [ ] Design common interface(s) for TimeWarp.Builder
- [ ] Consider naming: `IFluentBuilder<TSelf>`, `ISelfBuilder<TSelf>`, etc.
- [ ] Document when to use interface vs base class approach

### Implementation (if approved)
- [ ] Add interface(s) to TimeWarp.Builder
- [ ] Refactor existing builders to use common interface
- [ ] Update documentation

## Notes

### Current State

**TimeWarp.Builder has:**
- `IBuilder<TBuilt>` - For `Build()` method (returns built object)
- `INestedBuilder<TParent>` - For `Done()` method (returns parent builder)

**Missing:**
- Interface for CRTP-style fluent method chaining where methods return `TSelf`

### Known CRTP Usages

1. **`NuruCoreAppBuilder<TSelf>`** (base class)
   - Uses inheritance: `class NuruAppBuilder : NuruCoreAppBuilder<NuruAppBuilder>`
   - Methods return `TSelf` for fluent chaining

2. **`IKeyBindingBuilder<TSelf>`** (interface, created in task 209)
   - Uses composition: `NestedKeyBindingBuilder<TParent>` wraps `KeyBindingBuilder`
   - Both implement the interface for consistent API

### Design Considerations

**Interface approach benefits:**
- Works with composition (wrapping inner builder)
- Multiple inheritance possible
- More flexible

**Base class approach benefits:**
- Can share implementation code
- Simpler for pure inheritance hierarchies
- Can have protected members

### Potential Interface Design

```csharp
/// <summary>
/// Base interface for fluent builders using CRTP pattern.
/// </summary>
/// <typeparam name="TSelf">The concrete builder type for fluent chaining.</typeparam>
public interface IFluentBuilder<TSelf> where TSelf : IFluentBuilder<TSelf>
{
    // Marker interface - concrete builders define their own fluent methods
    // that return TSelf
}
```

Or with common operations:

```csharp
public interface IFluentBuilder<TSelf> where TSelf : IFluentBuilder<TSelf>
{
    /// <summary>
    /// Resets the builder to its initial state.
    /// </summary>
    TSelf Clear();
}
```
