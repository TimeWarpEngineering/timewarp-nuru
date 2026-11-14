# Custom converter ConstraintAlias in a route pattern generates uncompilable code

## Description

`IRouteTypeConverter.ConstraintAlias` is public API ("email" for `{to:email}`), but the generator never reads it:
`dsl-interpreter.behaviors.cs` builds every `CustomConverterDefinition` with `ConstraintAlias: null`. A route
that uses the alias compiles to broken code with no diagnostic. Found by task 529's consumer lane
(`generator-75-consumer-surface-no-ivt`):

```csharp
.AddTypeConverter(new PointConverter())          // TargetType = typeof(Point), ConstraintAlias => "point"
.Map("point {at:point}").WithHandler((Point at) => at.X).AsQuery().Done()
```

Generated output:

- `int result = __handler_8(at);`: CS0103, `at` is never declared on the positional path
- `at = __ar_at!;` / `at = __raw_at!;` in the JSON/option path: CS0029, string to `Point`

`{at:Point}` (the type name) works.

## Checklist

- [ ] Decide: read `ConstraintAlias` at compile time (a literal-returning property body), or report a diagnostic for an unknown constraint instead of emitting broken code
- [ ] Unknown constraint names never produce uncompilable output (diagnostic instead)
- [ ] Regression test (alias form, and an unknown constraint)
- [ ] `ganda runfile cache --clear`, then `./bin/dev test` green

## Notes

- The samples define aliases (`email`, `semver`) but route only by type name, so nothing exercised the alias form.
- Filed from task 529 (2026-11-14).

## Session

- Created: 1411579 (2026-11-14)
