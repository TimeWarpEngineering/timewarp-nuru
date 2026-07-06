# Align Runtime Type Converters With Invariant Culture Path

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M2, M3 + related LOW dead code).

## Description

The generated fast path parses with `InvariantCulture`
(`route-matcher-emitter.cs:1159-1167`) and
`timewarp-nuru-analyzers/generators/emitters/type-conversion-map.cs:5` claims it "mirrors
the runtime DefaultTypeConverters.cs to ensure parity" — but parity is broken:

- **M2**: `source/timewarp-nuru/type-conversion/default-type-converters.cs:21-146`
  (int/float/double/decimal/long/DateTime/TimeSpan/...) and the public converter classes
  (`int-type-converter.cs:17`, `double-type-converter.cs:13`, decimal/long/date-time/
  time-span/guid converters) all call `TryParse(value, out ...)` with NO CultureInfo —
  current-culture parsing. Under de-DE, `double.TryParse("3.14")` fails/misparses;
  `DateTime.TryParse("01/02/2024")` differs by locale. Any consumer of the public
  `ITypeConverterRegistry.TryConvert` or these converters gets locale-dependent results.
- **M3**: `type-conversion/converters/bool-type-converter.cs:5-38` advertises
  `true/false, yes/no, 1/0, on/off, enabled/disabled`, but the real binding path
  (generated code + default-type-converters.cs:91-98) uses `bool.TryParse` — only
  `true/false` work. Misleading public API.
- **Related LOW**: `builders/nuru-app-builder/nuru-app-builder.cs:9` —
  `TypeConverterRegistry` field is instantiated but never read (`AddTypeConverter` is a
  silent no-op at runtime); the individual public converter classes are never
  instantiated anywhere (only EnumTypeConverter is) and duplicate DefaultTypeConverters
  with divergent culture behavior — drift hazard. Decide: delete, internalize, or wire up.

## Requirements

- Use CultureInfo.InvariantCulture in all runtime TryParse calls to restore the
  documented parity.
- Resolve the bool-converter divergence: either support the extended spellings in the
  real binding path (both generated + runtime) or remove them.
- Resolve the dead converter classes / dead registry field (delete or wire up; if
  AddTypeConverter cannot work, it should be a diagnostic, not a silent no-op).

## Checklist

- [ ] Invariant culture in default-type-converters.cs and all public converters
- [ ] Bool spelling decision implemented consistently in both paths
- [ ] Dead TypeConverterRegistry field + unused converter classes resolved
- [ ] Tests under a non-invariant culture (e.g. de-DE) for double/DateTime parsing
- [ ] `ganda runfile cache --clear` + run CI tests

## Notes

### Implementation Plan (2026-07-06)

#### Decisions

| # | Question | Decision |
|---|----------|----------|
| Q1 | Enum parity gap | Fix it — align runtime enum branch in default-type-converters.cs with 454-008 IsDefined/Flags behavior |
| Q2 | Bool spellings (M3) | Extend the real path (both runtime + generator) to accept yes/no/1/0/on/off/enabled/disabled via a new public static BooleanConverter |
| Q3 | Dead converter classes | Delete all 7 + bool converter (int/double/decimal/long/dateTime/timeSpan/guid/bool); keep EnumTypeConverter (used by generator) |
| Q4 | Dead registry field + no-op | Remove dead TypeConverterRegistry field from nuru-app-builder.cs; keep AddTypeConverter as no-op (compile-time DSL hook); TypeConverterRegistry CLASS stays public |
| Q5 | Culture test approach | Manual set/restore CultureInfo.CurrentCulture in try/finally |
| Q6 | Scope | All three (M2 culture + M3 bool + LOW dead code) in one pass |

#### Phase 1: Create `source/timewarp-nuru/type-conversion/boolean-converter.cs`
New public static class `BooleanConverter` with `TryParse(string, out bool)` and `Parse(string)` — accepts true/false, yes/no, 1/0, on/off, enabled/disabled (case-insensitive). Single source of truth for bool parsing across generated + runtime paths.

#### Phase 2: Fix `source/timewarp-nuru/type-conversion/default-type-converters.cs`
- 2a: Add InvariantCulture to all 15 numeric/date TryParse calls (matching generator's type-conversion-map.cs exactly: NumberStyles.Integer for integers, NumberStyles.Float|AllowThousands for float/double, NumberStyles.Number for decimal, InvariantCulture+DateTimeStyles.None for DateTime/DateOnly/TimeOnly, InvariantCulture for TimeSpan)
- 2b: Replace bool branch with `BooleanConverter.TryParse(value, out bool boolValue)`
- 2c: Fix enum branch (lines 205-217) to match 454-008: Enum.Parse then IsDefined gate (non-Flags) or IsAllNamedEnumParts gate (Flags)
- 2d: Add private `IsAllNamedEnumParts(string value, Type enumType)` helper (non-generic mirror of EnumTypeConverter<TEnum>.IsAllNamedParts)

#### Phase 3: Update generator `source/timewarp-nuru-analyzers/generators/emitters/type-conversion-map.cs`
- Bool entry (line 41): `bool.TryParse(...)` → `global::TimeWarp.Nuru.BooleanConverter.TryParse(...)`

#### Phase 4: Update generator `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
- GetParseExpression bool entry (line 1166): `bool.Parse(...)` → `global::TimeWarp.Nuru.BooleanConverter.Parse(...)`

#### Phase 5: Delete dead converter class files (8 files)
- int-type-converter.cs, double-type-converter.cs, decimal-type-converter.cs, long-type-converter.cs, date-time-type-converter.cs, time-span-type-converter.cs, guid-type-converter.cs, bool-type-converter.cs
- KEEP enum-type-converter.cs (used by generator)

#### Phase 6: Remove dead TypeConverterRegistry field
- `source/timewarp-nuru/builders/nuru-app-builder/nuru-app-builder.cs` line 9: remove `private protected readonly TypeConverterRegistry TypeConverterRegistry = new();`
- Keep AddTypeConverter as no-op in nuru-app-builder.routes.cs (compile-time DSL hook)
- TypeConverterRegistry CLASS stays public (external consumers may use it)

#### Phase 7: Tests
- New file `tests/timewarp-nuru-tests/routing/routing-30-invariant-culture-binding.cs` — generated path: 4 culture tests (de-DE double/decimal/DateTime) + 11 bool spelling tests = 15 tests
- New file `tests/timewarp-nuru-tests/type-conversion/type-conversion-02-runtime-parity.cs` — runtime path via TypeConverterRegistry: 5 enum parity + 3 culture + 3 bool = 11 tests

#### Phase 8: Verify
1. `ganda runfile cache --clear` (generator code changed)
2. Run new test files standalone
3. `dotnet run tests/ci-tests/run-ci-tests.cs` (full CI)

#### Files touched
- Create: boolean-converter.cs, routing-30-invariant-culture-binding.cs, type-conversion-02-runtime-parity.cs
- Edit: default-type-converters.cs, type-conversion-map.cs, route-matcher-emitter.cs, nuru-app-builder.cs
- Delete: 8 dead converter class files (int/double/decimal/long/dateTime/timeSpan/guid/bool)
- No change: enum-type-converter.cs, type-converter-registry.cs, nuru-app-builder.routes.cs

#### Risk assessment
- Bool change is additive (accepts MORE values, rejects nothing that worked before)
- Culture change makes runtime MATCH generated path (which was already invariant)
- Enum change is behavior tightening (rejects undefined values) — matches generated path already fixed in 454-008
- Dead code deletion: 7+1 classes never instantiated anywhere
- MUST clear runfile cache (generator code changed)
