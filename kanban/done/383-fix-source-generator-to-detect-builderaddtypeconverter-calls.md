# Fix Source Generator to Detect builder.AddTypeConverter() Calls

**Status:** DONE (fixed as part of #382)

## Problem

The source generator fails to find custom type converters registered via `builder.AddTypeConverter()` method calls.

## Resolution

This task was based on a misdiagnosis. The `builder.AddTypeConverter()` calls **were already being detected** by `DispatchAddTypeConverter()` in `dsl-interpreter.cs`. 

The actual bug was in `route-matcher-emitter.cs`:
1. `GetSimpleTypeName()` didn't handle `global::` prefix, causing converter lookup to fail
2. `EmitCustomTypeConversion()` used empty `TargetTypeName` for non-generic converters

Both issues were fixed in commit `be303ff0` as part of task #382.

## Verification

```bash
$ dotnet run samples/10-type-converters/02-custom-type-converters.cs send-email test@example.com "Test"
📧 Sending Email:
   To: test@example.com
   Local part: test
   Domain: example.com
   Subject: Test
```

Debug output confirms detection is working:
```
warning NURU_DEBUG_CONV1: AddTypeConverter called: ConverterTypeName=global::EmailAddressConverter, TargetTypeName=
```

## Checklist

- [x] ~~Add extraction for `builder.AddTypeConverter()` calls~~ (already worked)
- [x] Test that samples/10-type-converters/02-custom-type-converters.cs builds
- [x] Verify samples/10-type-converters/01-builtin-types.cs still works
- [x] Run CI tests to confirm all pass (984 tests, 978 passed, 6 skipped)
