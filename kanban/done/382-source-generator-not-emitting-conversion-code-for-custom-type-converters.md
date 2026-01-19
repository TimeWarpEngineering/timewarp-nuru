# Source generator not emitting conversion code for custom type converters

**Priority:** High
**Status:** DONE

## Description

`samples/10-type-converters/02-custom-type-converters.cs` fails to build with CS0103 errors. The source generator is not emitting type conversion code for custom type converters like `EmailAddress`, `HexColor`, and `SemanticVersion`.

## Root Cause

Two bugs in `route-matcher-emitter.cs`:

1. **`GetSimpleTypeName()` didn't handle `global::` prefix**: The function expected dot-separated namespaces but `global::` uses `::`. This caused `GetSimpleTypeName("global::EmailAddressConverter")` to return the entire string instead of just `"EmailAddressConverter"`, breaking the convention-based converter lookup.

2. **`EmitCustomTypeConversion()` and `EmitOptionCustomTypeConversion()` used empty `TargetTypeName`**: For non-generic converters like `EmailAddressConverter`, the `TargetTypeName` field is empty (only populated for generic converters like `EnumTypeConverter<T>`). The emit functions used this empty value directly, generating invalid code like `() recipient = ()__temp!;` instead of `global::EmailAddress recipient = (global::EmailAddress)__temp!;`.

## Fix

1. Updated `GetSimpleTypeName()` to strip `global::` prefix before extracting the simple name
2. Updated `EmitCustomTypeConversion()` to derive target type from converter name when `TargetTypeName` is empty
3. Updated `EmitOptionCustomTypeConversion()` with the same fix for option parameters

## Files Changed

- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
  - `GetSimpleTypeName()` - Now handles `global::` prefix
  - `EmitCustomTypeConversion()` - Derives target type from converter name when empty
  - `EmitOptionCustomTypeConversion()` - Same fix for options
- `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`
  - Added debug diagnostic NURU_DEBUG_CONV1 (to be cleaned up in #365)
- `source/timewarp-nuru-analyzers/generators/extractors/app-extractor.cs`
  - Added CustomConverters count to debug output (to be cleaned up in #365)

## Verification

All sample commands now work:
```bash
./02-custom-type-converters.cs send-email user@example.com "Hello World"
./02-custom-type-converters.cs set-theme "#FF5733" "#3498DB"
./02-custom-type-converters.cs release 1.2.3-beta
./02-custom-type-converters.cs notify admin@example.com "#FF0000" "Critical Alert"
./02-custom-type-converters.cs deploy 2.0.0-beta staging --notify ops@company.com
```

## Checklist

- [x] Investigate why custom type converters are not being discovered by the source generator
- [x] Fix the converter lookup logic in route-matcher-emitter.cs (GetSimpleTypeName didn't handle global::)
- [x] Ensure generated code properly converts string parameters to custom types
- [x] Verify `02-custom-type-converters.cs` sample builds and runs correctly
- [ ] Add test coverage for custom type converter code generation
