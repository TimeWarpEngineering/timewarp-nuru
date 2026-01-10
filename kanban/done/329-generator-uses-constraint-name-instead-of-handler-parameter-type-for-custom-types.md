# Generator Uses Constraint Name Instead Of Handler Parameter Type For Custom Types

## Summary

Implemented custom type converter support in the source generator, allowing routes to use custom types like `{to:EmailAddress}` with proper code generation for converter instantiation and type conversion.

## Results

### What Was Implemented

1. **Interface Change:** Renamed `IRouteTypeConverter.ConstraintName` to `ConstraintAlias` (nullable)
   - Type name is now the primary constraint
   - Alias is optional for users who want shorter names (e.g., `email` for `EmailAddress`)

2. **Generator Changes:**
   - `RouteMatcherEmitter` generates converter instantiation and `TryConvert` calls
   - Supports both positional parameters (`{to:EmailAddress}`) and typed options (`--notify {email:EmailAddress?}`)
   - Convention-based lookup: `EmailAddressConverter` → `EmailAddress` target type
   - Error handling with user-friendly messages

3. **Case-Insensitive Matching (Feature):**
   - Built-in types match case-insensitively: `int`, `Int32`, `INT` all work
   - Lowercase C# primitives are preferred: `{x:int}` not `{x:Int32}`
   - No sample updates needed - existing code continues to work

### Files Modified

- `source/timewarp-nuru-core/type-conversion/iroute-type-converter.cs` - Renamed property
- `source/timewarp-nuru-core/type-conversion/converters/*.cs` - All 9 built-in converters
- `source/timewarp-nuru-analyzers/generators/models/custom-converter-definition.cs` - New model
- `source/timewarp-nuru-analyzers/generators/models/app-model.cs` - Added CustomConverters
- `source/timewarp-nuru-analyzers/generators/ir-builders/` - Threading converter info
- `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs` - Extract converter info
- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` - Code generation
- `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` - Pass converters
- `samples/10-type-converters/02-custom-type-converters.cs` - Updated sample
- `tests/timewarp-nuru-analyzers-tests/interpreter/dsl-interpreter-methods-test.cs` - Updated test

### Verified Working

```bash
# All commands work correctly:
./02-custom-type-converters.cs send-email user@example.com "Hello"
./02-custom-type-converters.cs set-theme "#FF5733" "#3498DB"
./02-custom-type-converters.cs release 2.1.3-beta
./02-custom-type-converters.cs deploy 1.2.3 production --notify ops@company.com

# Error handling works:
./02-custom-type-converters.cs send-email invalid-email "Test"
# Output: Error: Invalid EmailAddress value for parameter 'to': 'invalid-email'
```

## Original Description

When a route pattern uses a custom type constraint (e.g., `{to:email}`), the generator incorrectly
used the constraint name (`email`) as the type name in the generated command class, instead of
looking up the actual handler parameter type (`EmailAddress`).

This caused compilation errors like:
```
error CS0400: The type or namespace name 'email' could not be found in the global namespace
```

## Design Decisions

| Decision | Value |
|----------|-------|
| Primary constraint | Type name (EmailAddress, DateTime, int) |
| Alias property | Optional (`string?`), enables short names |
| Both work? | Yes - `{to:EmailAddress}` AND `{to:email}` if alias="email" |
| Primitive keywords | Preferred (`int` not `Int32`) |
| Case sensitivity | Case-insensitive matching (feature) |
| Convention | `XxxConverter` class → `Xxx` target type |
| Runtime registry | Not needed - compile-time converter instantiation |

## Generated Code Example

```csharp
// Pattern: {to:EmailAddress} or {to:email} (if alias defined)
var __converter_to_0 = new EmailAddressConverter();
if (!__converter_to_0.TryConvert(__to_0, out object? __temp_to_0))
{
  app.Terminal.WriteLine($"Error: Invalid EmailAddress value for parameter 'to': '{__to_0}'");
  return 1;
}
EmailAddress to = (EmailAddress)__temp_to_0!;
```

## Related Tasks

- #328 - Added `AddTypeConverter` DSL method support (prerequisite)
- #330 - Remove Mediator dependency (unrelated build issue discovered during this work)
