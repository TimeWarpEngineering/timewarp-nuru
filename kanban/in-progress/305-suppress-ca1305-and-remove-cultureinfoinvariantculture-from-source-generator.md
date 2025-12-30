# Suppress CA1305 and remove CultureInfo.InvariantCulture from source generator

## Description

Source generators emit C# code strings, not user-facing localized content. The CA1305 analyzer rule (Specify IFormatProvider) adds unnecessary verbosity with `CultureInfo.InvariantCulture` on every `StringBuilder.AppendLine()` call. Since there is NEVER any other culture for generating source code, suppress the rule and clean up the code.

## Checklist

- [ ] Add `CA1305` to `<NoWarn>` in `source/timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj` with explanatory comment
- [ ] Remove `CultureInfo.InvariantCulture` from `generators/emitters/configuration-emitter.cs` (39 occurrences)
- [ ] Remove `CultureInfo.InvariantCulture` from `generators/emitters/handler-invoker-emitter.cs` (30 occurrences)
- [ ] Remove `CultureInfo.InvariantCulture` from `generators/emitters/route-matcher-emitter.cs` (22 occurrences)
- [ ] Remove `CultureInfo.InvariantCulture` from `generators/emitters/capabilities-emitter.cs` (19 occurrences)
- [ ] Remove `CultureInfo.InvariantCulture` from `generators/emitters/help-emitter.cs` (9 occurrences)
- [ ] Remove `CultureInfo.InvariantCulture` from `generators/emitters/service-resolver-emitter.cs` (6 occurrences)
- [ ] Remove `CultureInfo.InvariantCulture` from `generators/emitters/interceptor-emitter.cs` (2 occurrences)
- [ ] Remove `CultureInfo.InvariantCulture` from `generators/emitters/version-emitter.cs` (1 occurrence)
- [ ] Remove `global using System.Globalization;` from `source/timewarp-nuru-analyzers/global-usings.cs`
- [ ] Build and verify no errors/warnings

## Notes

### Why suppress CA1305?

- CA1305 warns when `IFormatProvider` is not specified for culture-sensitive operations
- `AnalysisLevel=latest-all` + `TreatWarningsAsErrors=true` in `Directory.Build.props` enables this rule as an error
- For source generators, all output is C# code - never localized, culture-independent
- The interpolated strings contain only string concatenation, no numeric/date formatting that would be affected by culture

### Scope exclusions

- `reference-only/` files (~68 occurrences) are NOT modified - they are archived/reference code

### Transform pattern

```csharp
// Before:
sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}// comment");

// After:
sb.AppendLine($"{indent}// comment");
```

### Total cleanup

~128 occurrences across 8 active emitter files, plus 1 global using removal.
