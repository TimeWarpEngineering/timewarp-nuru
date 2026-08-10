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
- [x] Verify full CI tests pass (or document outcome in Results)
- [x] Update completion/REPL support if needed — no product change; EnumInfoExtractor unwrap covers capabilities metadata
- [x] Phase 4b review under `review/` (effort 1, disposition clean)

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
- Review: grok general round-1 (2026-08-10) — no issues; disposition clean
- Orchestrator: grok (2026-08-10) — Phase 4b + Results + done

## Results

### What was implemented

Repeated options with enum element types now convert via `EnumTypeConverter<T>` with the same invalid-value UX as single enum options.

- Handler extraction: `IsEnumBindableType` unwraps `Nullable`, arrays, and `IEnumerable`/`IList`/`ICollection`; `IsEnumType` means the element uses the enum converter
- Service classification: collection interfaces are DI only when the **element** is service-like (`IEnumerable<MyEnum>` is route-bound)
- Emitter: pass `route` into repeated conversion; for-loop + `TryConvert` + `GetValidValuesMessage` into `ElementType[]`
- Endpoints: `FromOption` sets `isArray` / `isEnumType`
- Metadata: `EnumInfoExtractor.Normalize` strips `[]` and collection wrappers (capabilities AllowedValues)
- Tests: `routing-32-enum-repeated-options.cs` (11 cases)

### Files changed

| File | Change |
|------|--------|
| `source/.../extractors/handler-extractor.cs` | Enum bindable types + collection-aware IsServiceType |
| `source/.../extractors/endpoint-extractor.cs` | FromOption isArray / isEnumType |
| `source/.../emitters/route-matcher-emitter.cs` | Route-aware repeated enum conversion |
| `source/.../extractors/enum-info-extractor.cs` | Normalize unwrap |
| `source/.../models/parameter-binding.cs` | IsEnumType doc |
| `tests/.../routing/routing-32-enum-repeated-options.cs` | New (11 tests) |
| `kanban/.../440-.../` | Plan, review/, Results |

### Key decisions / deviations

- For-loop + TryConvert (not Select+throw) for parity with single-enum error messages
- Always emit `ElementType[]` local (assignable to `IEnumerable<MyEnum>`)
- No dedicated capabilities-emitter edit (uses Normalize already)
- Completion/REPL product surface unchanged; metadata unwrap covers AllowedValues
- Custom converter arrays remain out of scope

### Test outcomes

| Command | Result |
|---------|--------|
| `routing-32-enum-repeated-options.cs` | 11/11 pass |
| `routing-06-repeated-options.cs` | 6/6 pass |
| `routing-24-enum-option-parameters.cs` | 9 pass, 1 pre-existing skip |
| `routing-29-enum-undefined-values.cs` | 15/15 pass |
| `dotnet run tests/ci-tests/run-ci-tests.cs` | exit 0 |

### Phase 4b review

- **Effort:** 1 (general only)
- **Rounds:** 1
- **Final counts:** 0 open (0 bug / 0 suggestion / 0 nit)
- **Disposition:** `clean`
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`

### How to validate

**Automated**
```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/routing/routing-32-enum-repeated-options.cs
# expect: Total 11, Passed 11

dotnet run tests/timewarp-nuru-tests/routing/routing-06-repeated-options.cs
dotnet run tests/timewarp-nuru-tests/routing/routing-24-enum-option-parameters.cs
dotnet run tests/timewarp-nuru-tests/routing/routing-29-enum-undefined-values.cs

dotnet run tests/ci-tests/run-ci-tests.cs
# expect: exit 0
```

**Smoke**
```bash
# In a Nuru Map app: Map("deploy --env {e}*").WithHandler((Environment[] e) => { ... })
# Args: deploy --env Dev --env Staging
# expect: handler receives Environment[] { Dev, Staging }, exit 0
# Args: deploy --env bad
# expect: exit 1; Error line lists valid enum values via GetValidValuesMessage()
```

**Not in scope:** custom type converters for arrays; catch-all positional `MyEnum[]`; full shell option-value completion overhaul.
