# Fix Enum Completion Overflow For Wide Underlying Types

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M21).

## Description

`source/timewarp-nuru/completion/completion/sources/enum-completion-source.cs:81` — the
`[Description]`-less fallback calls `Convert.ToInt32(value, ...)`. An enum with a wide
underlying type (`enum : long/ulong`) whose member value exceeds Int32 range (e.g. a
[Flags] bit `0x80000000`) throws OverflowException while GetCompletions enumerates
values — crashing the `__complete` request for that command.

## Checklist

- [ ] Use Convert.ToInt64/UInt64 or format via the enum's underlying type
- [ ] Test: completion over `enum : ulong` with a > Int32.MaxValue member
- [ ] Run completion test suite
