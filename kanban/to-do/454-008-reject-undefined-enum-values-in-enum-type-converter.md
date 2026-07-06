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
