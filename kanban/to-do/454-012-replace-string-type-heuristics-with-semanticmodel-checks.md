# Replace String Type Heuristics With SemanticModel Checks

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M8 + related LOW).

## Description

Several sites violate the repo convention "Always prefer SemanticModel over syntax string
manipulation for type resolution" (.agent/local/nuru-specific.md), with real
misclassification consequences:

- `source/timewarp-nuru-analyzers/extractors/endpoint-extractor.cs:506-508` and
  `:832-834` — `typeName.Contains("IEnumerable")` / `Contains("IList")` decides
  `isRepeated`; a user type like `MyApp.IListManager` is wrongly treated as a repeated
  option. The ITypeSymbol is already in hand — inspect it semantically
  (OriginalDefinition/AllInterfaces).
- `source/timewarp-nuru-analyzers/extractors/handler-extractor.cs:623-632` —
  `IsServiceType` uses `Contains("ILogger")` and "short name starts with I+uppercase"
  heuristics, over-matching user types like `IData` and mis-routing values between
  service injection and route binding; `TypeKind.Interface` (plus known-service set)
  answers correctly.
- `source/timewarp-nuru-analyzers/interpreter/dsl-interpreter.cs:1475` —
  `DispatchAddTypeConverter` falls back to `objectCreation.Type.ToString()` for
  ConverterTypeName when the symbol isn't resolved → unqualified, namespace-less name
  emitted into generated code.
- Related LOW: `endpoint-extractor.cs:880,896` — property default values captured as raw
  `initializerValue.ToString()` and emitted verbatim; symbols outside the generated
  file's using scope won't resolve. Use the semantic model / fully-qualified formatting.

## Checklist

- [ ] isRepeated decided via ITypeSymbol inspection (both endpoint-extractor sites)
- [ ] IsServiceType uses TypeKind/semantic checks
- [ ] AddTypeConverter fallback produces fully-qualified names or a diagnostic
- [ ] Property defaults emitted fully-qualified
- [ ] Regression tests (e.g. IListManager type, IData parameter)
- [ ] `ganda runfile cache --clear` + run CI tests

## Sequencing (reviewer, 2026-07-07)

SECOND of the analyzer trio: after 454-011, before 454-010. See 454-011 for rationale.

## Notes

**Blocked by #455**

**Implementation Plan: Replace String Type Heuristics With SemanticModel Checks (454-012 / M8)**

## Overview
Eliminate four string-heuristic type-resolution sites that violate the documented repo convention (prefer `SemanticModel`/`ITypeSymbol` inspection). Each site produces real misclassification or broken generated code for user types containing common substrings.

## Affected Files
- `source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs` (lines 506-508, 832-834, 880, 896)
- `source/timewarp-nuru-analyzers/generators/extractors/handler-extractor.cs` (lines 612-636)
- `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs` (line 1501)

## Step-by-Step Changes

### 1. endpoint-extractor.cs – Repeated Option Detection (Two Sites)
**Current (lines 506-508 and 832-834):**  
`typeName.EndsWith("[]") || typeName.Contains("IEnumerable") || typeName.Contains("IList")` decides `isRepeated`.

**Fix:** Replace both blocks with `ITypeSymbol` inspection:
- `property.Type` is already `ITypeSymbol`.
- Check:
  - `property.Type.TypeKind == TypeKind.Array`
  - `property.Type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T`
  - Or walk `property.Type.AllInterfaces` for `IList<T>` / `ICollection<T>` / `IEnumerable<T>` (using `SymbolEqualityComparer`).
- Remove the `typeName` string variable from the decision path for this check (keep it only for `TypeConstraint`/`ResolvedClrTypeName`).

**Impact:** `MyApp.IListManager` no longer falsely flagged as repeated.

### 2. endpoint-extractor.cs – Default Value Emission (Two Sites)
**Current (lines 880, 896):**  
`initializerValue.ToString()` emits raw syntax text.

**Fix:** Use semantic model on the initializer expression:
- `SemanticModel.GetConstantValue(initializerValue)` for primitives/enums.
- For complex expressions, emit via `initializerValueSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)` where `initializerValueSymbol` is obtained from `SemanticModel.GetSymbolInfo(...)`.
- If constant value unavailable, emit a diagnostic (NURU_S011 or similar) rather than emitting unresolvable syntax.

**Impact:** Types outside the generated file's using scope resolve correctly.

### 3. handler-extractor.cs – IsServiceType Replacement
**Current (lines 612-636):**  
String heuristics: `Contains("ILogger")`, `StartsWith("global::Microsoft.Extensions.")`, `shortName[0]=='I' && IsUpper(shortName[1])`, plus `IsBuiltInRouteBindableType` exclusion list.

**Fix:** Change callers (lines 103, 263, 386, 442) to pass the already-available `IParameterSymbol` / `ITypeSymbol` instead of `typeName` string. Replace `IsServiceType(string)` with:
```csharp
private static bool IsServiceType(ITypeSymbol typeSymbol, Compilation? compilation = null)
{
    if (typeSymbol.TypeKind == TypeKind.Interface)
        return !IsBuiltInRouteBindableInterface(typeSymbol); // e.g., IPAddress is a class, not interface

    // Known service base types (Microsoft.Extensions.*, ILogger, IServiceProvider, etc.)
    // Use OriginalDefinition + namespace checks, not string Contains
    return IsKnownServiceInterface(typeSymbol) || IsKnownServiceClass(typeSymbol);
}
```
- Remove or deprecate the string overload.
- Update `IsBuiltInRouteBindableType` similarly to operate on symbols.

**Impact:** `IData` (user interface) correctly treated as service; `IList<T>` correctly treated as bindable.

### 4. dsl-interpreter.cs – AddTypeConverter Fallback
**Current (line 1501):**  
`converterTypeName = objectCreation.Type.ToString();` when `GetSymbolInfo` fails.

**Fix:** Either:
- Emit a diagnostic (NURU_S012) and skip the converter registration, **or**
- Keep the fallback but qualify it via `SemanticModel.GetTypeInfo(objectCreation.Type).Type?.ToDisplayString(...)` (the comment at 1475 already prefers semantic model).

**Preferred:** Diagnostic path – an unresolved type at DSL-interpret time is a generator error, not something to silently emit broken code for.

## Regression Tests (GeneratorDriver Harness)
Create three new tests under `tests/timewarp-nuru-tests/generator/`:

1. `generator-34-m8-ilistmanager-repeated.cs`  
   Endpoint with property `IListManager Items { get; set; }` must **not** set `IsRepeated=true`.

2. `generator-35-m8-idata-service.cs`  
   Handler lambda `(IData svc) => ...` must bind as service injection, not route parameter.

3. `generator-36-m8-typeconverter-fqn.cs`  
   `AddTypeConverter(new MyApp.Converters.EmailConverter())` in a namespace without global using must emit fully-qualified converter type in generated code (or produce diagnostic).

Add the three `.cs` paths to:
- `tests/ci-tests/run-ci-tests.cs` standalone list.
- `<CiTestExcludes>` in `tests/ci-tests/Directory.Build.props` (CS0433 reason, same as generator-28/29/31/32).

## Execution Checklist
- [ ] `ganda runfile cache --clear` before CI run (source generator change).
- [ ] Run full CI: `dotnet run tests/ci-tests/run-ci-tests.cs`.
- [ ] Verify zero regressions on existing generator tests (especially 04, 13, 15, 16, 20, 26, 27 – service injection scenarios).
- [ ] Confirm `dotnet format` + warning-as-error build passes.

## Sequencing Note
This is the **second** of the analyzer trio (after 454-011, before 454-010) per reviewer guidance on 2026-07-07. No design questions remain; all sites and replacement strategies are fully specified.
