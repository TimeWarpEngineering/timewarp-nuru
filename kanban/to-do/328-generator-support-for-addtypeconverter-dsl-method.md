# Generator Support For AddTypeConverter DSL Method

## Description

The DSL interpreter in the source generator throws an error when encountering `AddTypeConverter()`:

```
error CS8785: Generator 'NuruGenerator' failed to generate source.
Exception: 'Unrecognized DSL method: AddTypeConverter. Location: ...'
```

This blocks the `samples/10-type-converters/02-custom-type-converters.cs` sample from working.

The generator needs to recognize `AddTypeConverter` as a valid DSL method. Since custom type converters
are a runtime feature (the converter instance is used at runtime for parsing), the generator likely
just needs to pass through this call without modifying the IR model.

## Related Files

- `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs` - Line 316, switch statement
- `source/timewarp-nuru-core/builders/nuru-core-app-builder/nuru-core-app-builder.routes.cs` - Line 83, runtime method
- `samples/10-type-converters/02-custom-type-converters.cs` - Blocked sample

## Checklist

- [ ] Add `"AddTypeConverter"` case to `DispatchMethodCall` switch in `dsl-interpreter.cs`
- [ ] Implement `DispatchAddTypeConverter` that returns the receiver unchanged (passthrough)
- [ ] Verify `02-custom-type-converters.cs` sample works
- [ ] Add generator test for `AddTypeConverter` DSL method

## Notes

The `AddTypeConverter` method at runtime is a simple passthrough that stores the converter in the
builder. The source generator comment says "Custom converters are registered via attributes or DSL
analysis" but this DSL analysis wasn't implemented.

For now, a simple passthrough is sufficient since custom type converters work at runtime - the
generator just needs to not fail when it sees this method call.

Discovered during #312 sample migration work.
