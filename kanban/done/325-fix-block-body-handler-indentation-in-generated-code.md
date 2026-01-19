# Fix block body handler indentation in generated code

## Status: COMPLETE ✅

## Summary

When handlers use block body syntax (multi-line lambdas with `{ }`), the generated local function had incorrect indentation because the lambda body was captured with its original source indentation.

## Solution

Added `ReindentBlockBody()` helper method that:
1. Splits the block body into lines
2. Finds the minimum indentation across all non-empty lines
3. Strips that base indentation from each line
4. Re-applies the target indentation for the generated code context

## Checklist

- [x] Find where block body handlers are emitted
- [x] Fix indentation logic for multi-line handler bodies
- [x] Ensure opening/closing braces align correctly
- [x] Test with various multi-line handlers
- [x] Verify `01-builtin-types.cs` sample works

## Files Modified

- `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs`

## Test Results

All block-body handlers in `01-builtin-types.cs` now generate with correct indentation:
- `read {path:FileInfo}` ✅
- `list {path:DirectoryInfo}` ✅
- `ping {address:ipaddress}` ✅
- `report {date:DateOnly}` ✅
- And others with multi-line bodies
