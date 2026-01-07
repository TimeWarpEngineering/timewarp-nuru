# Fix C# keyword escaping in generated parameter names

## Summary

When a route parameter uses a C# keyword as its name (e.g., `{event}`), the generator emits invalid code like `string event = ...` instead of `string @event = ...`.

## Example

**Route:** `schedule {event} {when:DateTime}`

**Generated (broken):**
```csharp
string event = __event_4;  // CS0065: 'event' is a C# keyword
```

**Should generate:**
```csharp
string @event = __event_4;  // Valid - @ prefix escapes keyword
```

## Affected Sample

- `samples/10-type-converters/01-builtin-types.cs`

## Blocks

- #313 - Fix generator type resolution for built-in types

## Checklist

- [ ] Identify all places where parameter names are emitted
- [ ] Create list of C# keywords that need escaping
- [ ] Add `@` prefix when parameter name is a keyword
- [ ] Test with `event`, `class`, `new`, `int`, etc.
- [ ] Verify `01-builtin-types.cs` sample progresses

## C# Keywords to Escape

Common ones that might appear as parameter names:
`event`, `class`, `new`, `int`, `string`, `bool`, `object`, `default`, `base`, `this`, `null`, `true`, `false`, `return`, `if`, `else`, `for`, `while`, `do`, `switch`, `case`, `break`, `continue`, `throw`, `try`, `catch`, `finally`, `lock`, `using`, `namespace`, `public`, `private`, `protected`, `internal`, `static`, `readonly`, `const`, `void`, `params`, `ref`, `out`, `in`

## Notes

The unique variable names (`__event_4`) are fine - only the handler-facing names need escaping.
