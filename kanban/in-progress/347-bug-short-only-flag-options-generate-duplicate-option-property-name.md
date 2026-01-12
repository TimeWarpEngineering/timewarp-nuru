# Bug: Short-only flag options generate duplicate Option property name

## Description

When the source generator processes route patterns with multiple short-only flag options (e.g., `-i -t`), it generates command classes where all short flags get the same property name `Option`, causing a CS0102 duplicate definition error.

## Reproduction

**File:** `tests/timewarp-nuru-core-tests/routing/routing-09-complex-integration.cs` line 22

**Pattern:**
```csharp
.Map("docker run -i -t --env {e}* -- {*cmd}")
  .WithHandler((bool i, bool t, string[] e, string[] cmd) => ...)
```

**Generated code (broken):**
```csharp
private sealed class __Route_119_Command
{
  public string[] Cmd { get; init; }
  public bool Option { get; init; }  // from -i
  public bool Option { get; init; }  // from -t (DUPLICATE!)
  public string[] Env { get; init; }
}
```

**Error:**
```
error CS0102: The type 'GeneratedInterceptor.__Route_119_Command' already contains a definition for 'Option'
```

## Expected Behavior

The generator should use the short option letter as the property name:
```csharp
private sealed class __Route_119_Command
{
  public string[] Cmd { get; init; }
  public bool I { get; init; }  // from -i
  public bool T { get; init; }  // from -t
  public string[] Env { get; init; }
}
```

## Checklist

- [ ] Investigate generator code that creates property names for short-only options
- [ ] Fix to use the short option letter (uppercase) as property name
- [ ] Add test case for multiple short-only flags in same pattern
- [ ] Verify routing-09-complex-integration.cs compiles and passes

## Notes

- Related to Bug #287/#288 which fixed short-only option matching, but not property naming
- Affects any route pattern with 2+ short-only flag options like `-v -f -r`
- The handler parameters `(bool i, bool t, ...)` show the expected names

## Files to Investigate

- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/models/segment-definition.cs`
