# Fix Enum Completion Overflow For Wide Underlying Types

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M21).

## Description

`source/timewarp-nuru/completion/completion/sources/enum-completion-source.cs:81` — the
`[Description]`-less fallback calls `Convert.ToInt32(value, ...)`. An enum with a wide
underlying type (`enum : long/ulong`) whose member value exceeds Int32 range (e.g. a
[Flags] bit `0x80000000`) throws OverflowException while GetCompletions enumerates
values — crashing the `__complete` request for that command.

## Checklist

- [x] Use Convert.ToInt64/UInt64 or format via the enum's underlying type
- [x] Test: completion over `enum : ulong` with a > Int32.MaxValue member
- [x] Run completion test suite

## Notes

### Implementation Plan (2026-07-06)

#### Decisions

| # | Question | Decision |
|---|----------|----------|
| 1 | Fix approach | Option A — `value.ToString("D", CultureInfo.InvariantCulture)`. The "D" format specifier on an enum returns the decimal representation of the underlying value for ALL underlying types (int/uint/long/ulong/short/ushort/byte/sbyte), avoiding the OverflowException that Convert.ToInt32 throws. AOT-safe on .NET 10. |
| 2 | Test file | Add tests to existing `tests/timewarp-nuru-tests/completion/completion-17-enum-source.cs` |
| 3 | Test enums | Test both `enum : long` and `enum : ulong`, including a value > Int64.MaxValue (proves Option A over Option C) |

#### Why Option A is backward-compatible
For existing Int32-range enums, "D" returns the same decimal string `Convert.ToInt32` produced (e.g. `Warning=2` → `"Value: 2"`). So `Should_handle_enum_without_description_attribute` and `Should_handle_enum_with_mixed_descriptions` still pass.

#### Step 1: Fix `source/timewarp-nuru/completion/completion/sources/enum-completion-source.cs`

Single edit at line 81. Replace:
```csharp
return $"Value: {Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)}";
```
With:
```csharp
return $"Value: {value.ToString("D", System.Globalization.CultureInfo.InvariantCulture)}";
```

#### Step 2: Add 3 tests to `tests/timewarp-nuru-tests/completion/completion-17-enum-source.cs`

Add test methods to `EnumSourceTests` class:
- `Should_handle_enum_with_long_underlying_type_exceeding_int32_range` — HighBit = 0x80000000L = 2147483648, assert no exception + description is "Value: 2147483648"
- `Should_handle_enum_with_ulong_underlying_type_exceeding_int32_range` — HighBit = 0x80000000UL = 2147483648, assert "Value: 2147483648"
- `Should_handle_ulong_value_exceeding_int64_range` — TopBit = 0x8000000000000000UL = 9223372036854775808, assert "Value: 9223372036854775808" (proves Option A handles what Option C couldn't)

Add two test enums at the bottom:
```csharp
enum WideLongEnum : long
{
  Low = 1L,
  HighBit = 0x80000000L
}

[Flags]
enum WideUlongEnum : ulong
{
  None = 0UL,
  Low = 1UL,
  HighBit = 0x80000000UL,
  TopBit = 0x8000000000000000UL
}
```

#### Step 3: Verify
1. `ganda runfile cache --clear` (precautionary, runtime-only fix)
2. `dotnet run tests/timewarp-nuru-tests/completion/completion-17-enum-source.cs` (standalone — expect 12 pass: 9 existing + 3 new)
3. `dotnet run tests/ci-tests/run-ci-tests.cs` (full CI)

#### Files touched
- Edit: `source/timewarp-nuru/completion/completion/sources/enum-completion-source.cs` (one line)
- Edit: `tests/timewarp-nuru-tests/completion/completion-17-enum-source.cs` (3 tests + 2 enums)

#### Risk assessment
- No regression: "D" format produces identical output to Convert.ToInt32 for Int32-range values
- No cross-contamination: no [NuruRoute] endpoints added/changed
- Runtime-only: no generator changes

## Results

### What was implemented

Fixed the `OverflowException` crash in `EnumCompletionSource<TEnum>.GetEnumDescription` when an enum has a wide underlying type (`enum : long/ulong`) with member values exceeding Int32 range.

- Replaced `Convert.ToInt32(value, CultureInfo.InvariantCulture)` at line 81 with `value.ToString("D")` — the "D" format specifier returns the decimal representation of the enum's underlying value for ALL underlying types (int/uint/long/ulong/short/ushort/byte/sbyte), with no overflow.
- Note: Used `value.ToString("D")` (no IFormatProvider) because `Enum.ToString(string?, IFormatProvider?)` is `[Obsolete]` in this target framework and the project treats warnings as errors. The "D" format is culture-invariant for enums, so behavior is unchanged.
- Added explanatory comment block so future reviewers understand why `Convert.ToInt32` was not used.
- Added 3 regression tests + 2 wide-underlying-type test enums.

### Files changed

- `source/timewarp-nuru/completion/completion/sources/enum-completion-source.cs` — one-line fix (Convert.ToInt32 → value.ToString("D")) + explanatory comment
- `tests/timewarp-nuru-tests/completion/completion-17-enum-source.cs` — 3 new tests + 2 new enums (WideLongEnum, WideUlongEnum)

### Key decisions made

- **Option A chosen over Option C**: `value.ToString("D")` handles `ulong` values > Int64.MaxValue (e.g. `0x8000000000000000UL = 9223372036854775808`), which `Convert.ToInt64` (Option C) could not.
- **Dropped IFormatProvider**: `Enum.ToString(string?, IFormatProvider?)` is obsolete in the target framework; "D" format is culture-invariant for enums regardless.

### Test outcomes

- **Standalone** (`dotnet run tests/timewarp-nuru-tests/completion/completion-17-enum-source.cs`): 13 passed, 0 failed (10 existing + 3 new)
- **Full CI** (`dotnet run tests/ci-tests/run-ci-tests.cs`): 1319 passed, 7 skipped, 0 failed. No regressions.
