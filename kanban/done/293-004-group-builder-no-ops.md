# #293-004: Convert GroupBuilder Methods to No-Ops

## Parent

#293 Make DSL Builder Methods No-Ops

## Dependencies

- #293-002 (GroupEndpointBuilder constructor change)

## Description

Convert `GroupBuilder<TParent>` methods from doing runtime work (pattern parsing with prefix, endpoint creation) to no-ops.

## File

`source/timewarp-nuru-core/builders/group-builder.cs`

## Checklist

### Constructor Changes
- [ ] Simplify constructor - remove `Action<Endpoint> registerEndpoint` parameter
- [ ] Remove `_registerEndpoint` field
- [ ] Remove `_loggerFactory` field (only used for pattern parsing)
- [ ] Keep `_parent` for `Done()` navigation
- [ ] Keep `_prefix` (may be useful for debugging, or can remove)

### Method Conversions
- [ ] `Map(string pattern)` → return shell `GroupEndpointBuilder` (line 67-81)
- [ ] `WithGroupPrefix(string prefix)` → return shell nested `GroupBuilder` (line 100-105)
- [ ] `Done()` → keep as-is (already just returns `_parent`)

### Cleanup
- [ ] Remove `PatternParser.Parse()` calls
- [ ] Remove prefix concatenation (source gen handles at compile time)
- [ ] Add comments explaining source gen handles actual work

## Current Implementation

```csharp
public class GroupBuilder<TParent>(
  TParent parent,
  string prefix,
  Action<Endpoint> registerEndpoint,
  ILoggerFactory? loggerFactory = null)
  where TParent : class
{
  private readonly TParent _parent = parent;
  private readonly string _prefix = prefix;
  private readonly Action<Endpoint> _registerEndpoint = registerEndpoint;
  private readonly ILoggerFactory? _loggerFactory = loggerFactory;

  public GroupEndpointBuilder<TParent> Map(string pattern)
  {
    ArgumentNullException.ThrowIfNull(pattern);
    string fullPattern = $"{_prefix} {pattern}";

    Endpoint endpoint = new()
    {
      RoutePattern = fullPattern,
      CompiledRoute = PatternParser.Parse(fullPattern, _loggerFactory)
    };

    _registerEndpoint(endpoint);
    return new GroupEndpointBuilder<TParent>(this, endpoint);
  }

  public GroupBuilder<GroupBuilder<TParent>> WithGroupPrefix(string prefix)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
    string nestedPrefix = $"{_prefix} {prefix}";
    return new GroupBuilder<GroupBuilder<TParent>>(this, nestedPrefix, _registerEndpoint, _loggerFactory);
  }

  public TParent Done() => _parent;
}
```

## Target Implementation

```csharp
/// <summary>
/// Fluent builder for creating route groups with shared prefixes.
/// This is a compile-time DSL shell - the source generator extracts configuration
/// at build time. Runtime methods are no-ops that maintain the fluent chain.
/// </summary>
public class GroupBuilder<TParent>(TParent parent)
  where TParent : class
{
  private readonly TParent _parent = parent;

  public GroupEndpointBuilder<TParent> Map(string pattern)
  {
    // Source generator extracts grouped pattern at compile time
    _ = pattern;
    return new GroupEndpointBuilder<TParent>(this);
  }

  public GroupBuilder<GroupBuilder<TParent>> WithGroupPrefix(string prefix)
  {
    // Source generator handles nested prefix at compile time
    _ = prefix;
    return new GroupBuilder<GroupBuilder<TParent>>(this);
  }

  public TParent Done() => _parent;
}
```

## Breaking Changes

Constructor signature changes significantly:
- Old: `GroupBuilder(TParent, string, Action<Endpoint>, ILoggerFactory?)`
- New: `GroupBuilder(TParent)`

**Impact:** Only `NuruCoreAppBuilder.WithGroupPrefix()` creates `GroupBuilder` instances. Updated in #293-003.

## Temporary Compatibility

If needed for incremental work, add backward-compatible constructor:
```csharp
[Obsolete("Use GroupBuilder(TParent) - other parameters ignored")]
public GroupBuilder(TParent parent, string _, Action<Endpoint> __, ILoggerFactory? ___ = null) 
  : this(parent) { }
```

## Notes

- After this change, `_prefix` storage is no longer needed at runtime
- The source generator reconstructs the full pattern from the DSL at compile time
- Nested group support (`WithGroupPrefix` returning `GroupBuilder<GroupBuilder<T>>`) still works for fluent API
