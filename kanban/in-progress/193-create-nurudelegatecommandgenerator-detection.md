# Create NuruDelegateCommandGenerator (Detection + Pattern Parsing)

## Description

Update NuruInvokerGenerator to detect `Map(pattern).WithHandler(delegate)` chains instead of the old `Map(pattern, handler)` API. Delete unused `DelegateSignatureExtractor`.

## Parent

151-implement-delegate-generation-phase-2

## Dependencies

- Task 192: API cleanup must be complete (old overloads removed) ✅

## Implementation Notes

**Approach taken:** Option A - Updated existing `NuruInvokerGenerator` rather than creating new generator.

### Changes Made

1. **Deleted unused `DelegateSignatureExtractor`**
   - Was never used in any generator
   - Built for old API pattern

2. **Updated `NuruInvokerGenerator`**
   - Changed `IsMapInvocation()` to detect `WithHandler()` calls
   - Added `FindPatternFromFluentChain()` to walk back syntax tree from `WithHandler()` to `Map(pattern)`
   - Updated `GetRouteWithSignature()` to extract handler from `WithHandler(handler)` argument

3. **Updated analyzer tests**
   - Converted all tests in `nuru-invoker-generator-01-basic.cs` to use new fluent API
   - Updated test method names to reflect new API (e.g., `Should_detect_default_route_invocations`)

## Checklist

### Generator Setup
- [x] ~~Create new generator~~ (Updated existing `NuruInvokerGenerator` instead)
- [x] Register syntax provider for `WithHandler` invocations

### Detection
- [x] Find `WithHandler(delegate)` calls
- [x] Walk syntax tree back to find `Map(pattern)` call
- [x] Extract pattern string from `Map()` argument
- [x] Handle lambda expressions and method groups

### Delegate Signature Extraction
- [x] Extract parameter list (names, types) - existing code reused
- [x] Extract return type - existing code reused
- [x] Detect async delegates - existing code reused
- [x] Detect nullable parameters - existing code reused

### Cleanup
- [x] Delete unused `DelegateSignatureExtractor`
- [x] Update analyzer tests to use new fluent API

## Files Modified

- `source/timewarp-nuru-analyzers/analyzers/nuru-invoker-generator.cs` - Updated detection logic
- `source/timewarp-nuru-analyzers/analyzers/delegate-signature-extractor.cs` - Deleted
- `tests/timewarp-nuru-analyzers-tests/auto/nuru-invoker-generator-01-basic.cs` - Updated to new API
