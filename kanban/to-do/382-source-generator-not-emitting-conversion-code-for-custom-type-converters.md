# Source generator not emitting conversion code for custom type converters

**Priority:** High

## Description

`samples/10-type-converters/02-custom-type-converters.cs` fails to build with CS0103 errors. The source generator is not emitting type conversion code for custom type converters like `EmailAddress`, `HexColor`, and `SemanticVersion`.

**Build errors:**
```
error CS0103: The name 'recipient' does not exist in the current context
error CS0103: The name 'color' does not exist in the current context
error CS0103: The name 'version' does not exist in the current context
error CS0103: The name 'to' does not exist in the current context
error CS0103: The name 'primary' does not exist in the current context
error CS0103: The name 'secondary' does not exist in the current context
```

**Generated code shows the bug:**
```csharp
if (routeArgs is ["notify", var __recipient_3, var __color_3, var __message_3])
{
  string message = __message_3;
  // WARNING: No converter found for type constraint 'EmailAddress'
  // Register a converter with: builder.AddTypeConverter<YourConverter>();
  // WARNING: No converter found for type constraint 'HexColor'
  // Register a converter with: builder.AddTypeConverter<YourConverter>();
  void __handler_3(global::EmailAddress recipient, global::HexColor color, string message)
  {
    // handler body
  }
  __handler_3(recipient, color, message);  // CS0103: 'recipient' and 'color' undefined!
  return 0;
}
```

The custom type converters defined in the sample (EmailAddressConverter, HexColorConverter, SemanticVersionConverter) are not being discovered by the source generator, so no conversion code is emitted - only a warning comment.

## Checklist

- [ ] Investigate why custom type converters are not being discovered by the source generator
- [ ] Fix the converter lookup logic in route-matcher-emitter.cs (likely around line 748)
- [ ] Ensure generated code properly converts string parameters to custom types
- [ ] Verify `02-custom-type-converters.cs` sample builds and runs correctly
- [ ] Add test coverage for custom type converter code generation

## Notes

**Root cause location:** Likely in `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` in the custom converter lookup logic around line 748.

**Related:** This is the same pattern as issue #381 (Uri/FileInfo/DirectoryInfo) but for custom type converters instead of built-in types.
