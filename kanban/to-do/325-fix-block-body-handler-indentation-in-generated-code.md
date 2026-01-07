# Fix block body handler indentation in generated code

## Summary

When handlers use block body syntax (multi-line lambdas with `{ }`), the generated local function has incorrect indentation, causing the code structure to break.

## Example

**Handler:**
```csharp
.WithHandler((string feature, bool state) =>
{
  Console.WriteLine($"Feature '{feature}' is {(state ? "enabled" : "disabled")}");
  return 0;
})
```

**Generated (broken):**
```csharp
int __handler_4()
  {                          // Wrong indent
Console.WriteLine(...);      // No indent
return 0;
}                            // Wrong indent - closes at wrong level
```

**Should generate:**
```csharp
int __handler_4()
{
  Console.WriteLine(...);
  return 0;
}
```

## Affected Sample

- `samples/10-type-converters/01-builtin-types.cs`

## Blocks

- #313 - Fix generator type resolution for built-in types

## Checklist

- [ ] Find where block body handlers are emitted
- [ ] Fix indentation logic for multi-line handler bodies
- [ ] Ensure opening/closing braces align correctly
- [ ] Test with various multi-line handlers
- [ ] Verify `01-builtin-types.cs` sample progresses

## Key Files to Investigate

- `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs`

## Notes

Expression-body handlers (single-line with `=>`) likely work fine. The issue is specific to block body handlers.
