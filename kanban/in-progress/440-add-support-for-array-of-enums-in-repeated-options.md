# Add support for array of enums in repeated options

## Description

Implement support for `MyEnum[]` or `IEnumerable<MyEnum>` in repeated options (e.g., `--env Dev --env Staging`).

Currently, single enum parameters work correctly via `EnumTypeConverter<T>`, but repeated options with enum element types silently degrade to `string[]` instead of the typed enum array.

## Checklist

- [x] Investigate `EmitRepeatedOptionTypeConversion` in route-matcher-emitter.cs (~line 1104)
- [x] Add `RouteDefinition route` parameter usage (currently discarded with `_ = route;`)
- [x] Check `route.Handler.Parameters` for `IsEnumType` matching the option name
- [x] Emit enum conversion for enum arrays (for-loop + TryConvert + GetValidValuesMessage; not Select+throw)
- [x] Handle error cases (invalid enum values in array)
- [x] Add unit tests for `MyEnum[]` in option position
- [x] Add unit tests for `IEnumerable<MyEnum>` in option position
- [x] Add unit tests for nullable enum arrays (`MyEnum[]?`)
- [x] Add unit tests for error messages showing valid enum values
- [x] Verify related routing tests pass (clear runfile cache first)
- [ ] Verify full CI tests pass
- [ ] Update completion/REPL support if needed
- [ ] Mark done / commit (orchestrator)

## Notes

# Implementation Plan: Task 440 — Array of Enums in Repeated Options

## Summary

**Problem.** Single enum options work via `EnumTypeConverter<T>` and `ParameterBinding.IsEnumType`. Repeated options (`--env Dev --env Staging`) collect values into `List<string>`, then `EmitRepeatedOptionTypeConversion` only converts built-in primitives from `option.TypeConstraint`. Enum element types fall through to `string[]`.

**Root causes:**
1. `IsEnumOrNullableEnum` does not unwrap arrays/collections — `MyEnum[]` gets `IsEnumType = false`
2. `EmitRepeatedOptionTypeConversion` ignores handler types — only uses TypeConstraint + built-ins; route discarded
3. Endpoint option bindings never set `IsEnumType` / `IsArray`
4. `IEnumerable<MyEnum>` is classified as a DI service today

**Approach.** Fix enum-element detection at extraction; use binding in repeated-option emission with same error UX as single enum options; unwrap array/collection type names for metadata. Prefer `MyEnum[]` primary; support `IEnumerable<MyEnum>` via service classification fix + assign `MyEnum[]` into handler param.

## Implementation steps

### A — Detect enum element types (handler-extractor.cs)
- Extend `IsEnumOrNullableEnum` → `IsEnumBindableType`: unwrap Nullable, arrays, IEnumerable/IList/ICollection; true if element is enum
- `IsServiceType`: collection interface is service only if element is service-like
- Update `ParameterBinding.IsEnumType` docs: means element uses EnumTypeConverter

### B — Endpoint option bindings (endpoint-extractor.cs)
- At FromOption sites: set `isArray = IsRepeatedOptionType`, `isEnumType` from enum-element helper

### C — Emit enum conversion (route-matcher-emitter.cs)
- Pass route into `EmitRepeatedOptionTypeConversion`; remove `_ = route;`
- After built-in TypeConstraint path, lookup handler param with `IsEnumType` for option
- New `EmitRepeatedOptionEnumTypeConversion`: for-loop + TryConvert + GetValidValuesMessage (match single-enum UX, NOT Select+throw with weak catch message)
- Emit `ElementType[]` local always; `IEnumerable<MyEnum>` accepts `MyEnum[]`

### D — Enum metadata unwrap
- `enum-info-extractor.cs` + `capabilities-emitter.cs`: strip `[]` and collection wrappers for AllowedValues

### E — Tests
New: `tests/timewarp-nuru-tests/routing/routing-32-enum-repeated-options.cs`
Cases: `MyEnum[]` multi/single/empty, case-insensitive, invalid+valid list, invalid among valid, short alias, `IEnumerable<MyEnum>`, `MyEnum[]?`, mixed options, optional endpoint stretch

### Out of scope
Custom converters for arrays; bare `{id}*` without constraint; catch-all `MyEnum[]` positionals; full option-value shell completion overhaul

## Validation
```
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/routing/routing-32-enum-repeated-options.cs
dotnet run tests/timewarp-nuru-tests/routing/routing-06-repeated-options.cs
dotnet run tests/timewarp-nuru-tests/routing/routing-24-enum-option-parameters.cs
dotnet run tests/ci-tests/run-ci-tests.cs
```

## Sequence
1. Failing tests first
2. IsEnum detection + service fix
3. Emitter
4. Endpoint flags
5. Metadata unwrap
6. IEnumerable + nullable tests
7. Clear cache + full CI

## Session

- Orchestrator: grok (2026-08-10) — Phase 2 plan finalized
- Implementer: grok (2026-08-10) — Implemented A–E:
  - `IsEnumBindableType` + collection-aware `IsServiceType` in handler-extractor
  - Endpoint FromOption sets `isArray`/`isEnumType`
  - `EmitRepeatedOptionEnumTypeConversion` (for-loop + TryConvert + GetValidValuesMessage)
  - `EnumInfoExtractor.Normalize` unwraps `[]`/collections (capabilities via Normalize)
  - Tests: routing-32 (11/11 pass); routing-06, 24, 29 pass
  - Full CI not run by implementer; leave dirty tree for orchestrator commit
