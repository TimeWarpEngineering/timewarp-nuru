# Generator Uses Constraint Name Instead Of Handler Parameter Type For Custom Types

## Description

When a route pattern uses a custom type constraint (e.g., `{to:email}`), the generator incorrectly
uses the constraint name (`email`) as the type name in the generated command class, instead of
looking up the actual handler parameter type (`EmailAddress`).

This causes compilation errors like:
```
error CS0400: The type or namespace name 'email' could not be found in the global namespace
```

## Example

Route pattern:
```csharp
builder.Map("send-email {to:email} {subject}")
  .WithHandler((EmailAddress to, string subject) => ...)
```

Generated code (incorrect):
```csharp
private sealed class __Route_0_Command
{
  public global::email To { get; init; }  // Wrong! Should be EmailAddress
  public string Subject { get; init; }
}
```

Expected generated code:
```csharp
private sealed class __Route_0_Command
{
  public global::EmailAddress To { get; init; }  // Correct type from handler
  public string Subject { get; init; }
}
```

## Related Files

- `samples/10-type-converters/02-custom-type-converters.cs` - Blocked by this issue
- Generator code that maps constraint names to types (needs investigation)

## Checklist

- [ ] Investigate how the generator resolves parameter types
- [ ] Fix type resolution to use handler parameter types instead of constraint names
- [ ] Verify `02-custom-type-converters.cs` sample works
- [ ] Add test for custom type constraint type resolution

## Notes

For built-in types, the constraint name matches the type name (e.g., `int`, `DateTime`, `FileInfo`),
so this bug doesn't manifest. It only affects custom type converters where the constraint name
differs from the type name.

Discovered after fixing #328 (AddTypeConverter DSL method support).
