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
