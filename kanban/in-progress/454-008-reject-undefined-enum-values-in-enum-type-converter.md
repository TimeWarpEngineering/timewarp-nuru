# Reject Undefined Enum Values In Enum Type Converter

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M1).

## Description

`source/timewarp-nuru/type-conversion/converters/enum-type-converter.cs:23` uses
`Enum.TryParse<TEnum>(value, ignoreCase: true, out ...)`, which returns true for ANY
numeric string — including values with no defined member (`"999"` → `(TEnum)999`) and
comma-separated flag combos. The generator emits `new EnumTypeConverter<T>()` for every
enum parameter/option (see
`source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:1040,1052`),
so this IS the real runtime/AOT binding path.

Failure: `mycli set-level 999` matches a `{level:LogLevel}` route and invokes the handler
with an invalid enum value instead of failing route matching.

## Requirements

- After TryParse succeeds, reject values not defined for the enum (Enum.IsDefined or
  equivalent AOT-safe check) for non-[Flags] enums; decide policy for [Flags] enums and
  numeric input, and document it.

## Checklist

- [ ] Add defined-value check for non-Flags enums
- [ ] Decide + document [Flags]/numeric policy
- [ ] Tests: "999" rejected, valid name accepted case-insensitively, Flags behavior
- [ ] `ganda runfile cache --clear` + run CI tests

## Notes

### Implementation Plan (2026-07-06)

#### Decisions

| Question | Decision |
|---|---|
| Non-Flags numerics | Reject only undefined — `Enum.IsDefined<TEnum>` gate. `"999"` rejected, `"10"` accepted if it maps to a defined member. |
| [Flags] policy | Accept named members + comma-separated name combos (TryParse validates these); reject raw numerics (`"12"`, `"1,2"`). |
| Test file | New file `tests/timewarp-nuru-tests/routing/routing-29-enum-undefined-values.cs` |
| GetValidValuesMessage | Keep current — `Enum.GetNames<TEnum>()` comma-joined, same for Flags. |
| AOT | Confirmed safe — `Enum.IsDefined<TEnum>` + `GetCustomAttribute<FlagsAttribute>` are AOT-safe on .NET 10. |

#### Step 1: Modify `source/timewarp-nuru/type-conversion/converters/enum-type-converter.cs`

- Add `static readonly bool IsFlagsEnum` (cached per TEnum via `GetCustomAttribute<FlagsAttribute>()`)
- Add `static readonly HashSet<string> NameSet` (case-insensitive, for Flags validation)
- In `TryConvert`: after TryParse succeeds:
  - Non-Flags: gate with `Enum.IsDefined<TEnum>(enumValue)` — reject if not defined
  - Flags: gate with `IsAllNamedParts(value)` — every comma-separated part must be a defined name (rejects raw numerics; C# enum names can't be purely numeric)
- Document both policies in the `TryConvert` XML doc comment
- Keep `GetValidValuesMessage()` unchanged
- No new `using` directives needed (`System.Reflection` is a global using)

#### Step 2: Create `tests/timewarp-nuru-tests/routing/routing-29-enum-undefined-values.cs`

15 test methods, inline `.Map().WithHandler()` fluent style (each builds own NuruApp — no DiscoverEndpoints cross-contamination).

Enums (nested in `EnumUndefinedValueTests` class):
- `Priority { Low=0, Normal=10, High=20, Critical=30 }` — non-Flags with non-consecutive values
- `[Flags] FilePermissions { None=0, Read=1, Write=2, Execute=4, ReadWrite=3 }`

Tests:
- Non-Flags: reject "999" (positional + option), reject "25" (between defined), accept "10" (maps to Normal), accept "High", accept "HIGH" case-insensitive, reject "invalid", verify error message shows valid values
- Flags: accept "Read", accept "Read,Write" → ReadWrite, accept "read,write" case-insensitive, accept "ReadWrite" combined member, reject "12" raw numeric, reject "1,2" comma numerics, reject "3" (numeric even though = ReadWrite)

#### Step 3: Verify

1. `ganda runfile cache --clear`
2. `dotnet run tests/timewarp-nuru-tests/routing/routing-29-enum-undefined-values.cs` (standalone)
3. `dotnet run tests/ci-tests/run-ci-tests.cs` (full CI — verifies no existing tests break; new file auto-included via glob)

#### Risk analysis

- Existing tests: very low risk — all use valid named members, no numeric strings to enum params.
- AOT/trimming: very low — only AOT-safe APIs used; static readonly caches per-type.
- CI multi-mode collision: none — inline style, nested enums, distinct route patterns (set-priority, grant).

#### Files touched

- `source/timewarp-nuru/type-conversion/converters/enum-type-converter.cs` (modify)
- `tests/timewarp-nuru-tests/routing/routing-29-enum-undefined-values.cs` (create)

No generator, interface, or GetValidValuesMessage changes.
