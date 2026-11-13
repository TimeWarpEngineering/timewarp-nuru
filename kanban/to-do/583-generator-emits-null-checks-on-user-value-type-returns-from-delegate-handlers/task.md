# Generator emits null checks on user value-type returns from delegate handlers

## Description

A fluent delegate handler that returns a user value type (an enum, or a user struct) generates code that does
not compile in any assembly. Found by task 529's consumer lane (`generator-75-consumer-surface-no-ivt`):

```csharp
.Map("mode {shade:Shade}").WithHandler((Shade shade) => shade).AsQuery().Done()
public enum Shade { Light, Dark }
```

Generated output:

- `if (result is not null)`: CS0037, cannot convert null to the non-nullable `Shade`
- `result?.ToString() ?? ""` in the JSON fallback: CS0023, `?` on a value type

Cause: `HandlerInvokerEmitter.IsKnownValueType` and `GetOutputStrategy`
(`source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.output.cs`) decide from the type
*name*, so only BCL value types are known. A user value type is treated as a reference type and goes to
`OutputStrategy.Json` with a null guard.

## Checklist

- [ ] Carry the return type's `IsValueType` (and nullable-ness) from the semantic model into the handler model
- [ ] Skip the null guard and `?.` for non-nullable value types; decide whether enums print raw (`ToString()`) rather than JSON
- [ ] Add a regression case (enum and user struct return) to generator-75 or its own generator test
- [ ] `ganda runfile cache --clear`, then `./bin/dev test` green

## Notes

- Workaround in 529's lane: the handler returns `shade.ToString()`.
- Filed from task 529 (2026-11-14).

## Session

- Created: 1411579 (2026-11-13)
