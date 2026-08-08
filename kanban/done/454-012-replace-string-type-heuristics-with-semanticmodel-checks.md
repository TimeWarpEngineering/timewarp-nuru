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

- [x] isRepeated decided via ITypeSymbol inspection (both endpoint-extractor sites) — `IsRepeatedOptionType(ITypeSymbol)` / `IsCollectionInterface` added, both call sites (endpoint-extractor.cs ~473-511, ~794-837) switched from `typeName.Contains(...)`.
- [x] IsServiceType uses TypeKind/semantic checks — `IsServiceType(ITypeSymbol)` + `IsLoggerOrServiceProvider` added; symbol call sites (handler-extractor.cs ExtractFromMethodSymbol/ExtractFromMethodSymbolAsMethod) now pass `param.Type`. String overload kept only for the two syntax-only fallback branches (unreachable for code that compiles), per 455's Option A.
- [x] AddTypeConverter fallback produces fully-qualified names or a diagnostic — `DispatchAddTypeConverter` now tries `GetSymbolInfo` then `GetTypeInfo`, treats `TypeKind.Error` as unresolved, and emits new `NURU_S009` when genuinely unresolvable instead of raw `.ToString()`.
- [x] Property defaults emitted fully-qualified — `ExtractPropertyDefaultValueFromSymbol` now resolves via `GetSymbolInfo`/`GetConstantValue` with a custom `FullyQualifiedMemberFormat` (member symbols need `IncludeContainingType`, which `SymbolDisplayFormat.FullyQualifiedFormat` does not set). The syntax-only `ExtractPropertyDefaultValue(PropertyDeclarationSyntax)` overload is dead code (never called); left as-is with a TODO comment.
- [x] Regression tests (e.g. IListManager type, IData parameter) — added generator-34 (IListManager not repeated), generator-35 (IData binds as service, string still binds as param), generator-36 (converter FQN emission + NURU_S009 for unresolvable type). All wired into run-ci-tests.cs standalone list + Directory.Build.props CiTestExcludes.
- [x] `ganda runfile cache --clear` + run CI tests — full CI green: multi-mode 1383 total / 1376 passed / 7 skipped / 0 failed; standalone phase (incl. generator-34/35/36) all passed, 0 failures, exit code 0.

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

## Reviewer Pre-Answers (reviewer, 2026-07-13)

The implementation plan above (added by another agent) already folds in two of the three
things I was going to pre-answer — the corrected `.../generators/extractors/...` paths and
the before/after ordering. Adding only what is genuinely missing or where I'd refine the
plan, so nothing here silently overrides the plan's decisions:

1. **#455 is the real gate, not these pre-answers.** 454-011 is done and 010 must follow
   012, but 012 cannot start until blocker **#455** is resolved: the plan's
   `IsServiceType(ITypeSymbol)` signature change assumes symbols are in hand at every call
   site, and they are NOT on the lambda-extraction path (`handler-extractor.cs:103,263`),
   which only has `RoslynParameterSyntax` and derives `typeName` via strings. That step
   needs a lambda-parameter→symbol binding refactor (`SemanticModel.GetSymbolInfo`/
   `GetTypeInfo` on the parameter's type syntax) BEFORE the string heuristic can be
   replaced. Scope that in #455 first; it is the largest and riskiest part of M8.

2. **Concurrency constraint (missing from the plan).** Do NOT run 454-012 concurrently
   with 454-010 OR 454-028 — all three touch analyzer/generator internals and would
   collide. The plan states the ordering but not this mutual-exclusion; it matters because
   other agents are working in parallel.

3. **AddTypeConverter fallback — I'd refine the plan's "prefer diagnostic path" (§4).**
   The repo convention (nuru-specific.md) is to try harder semantically before giving up:
   `GetSymbolInfo().Symbol` resolves external/referenced-project types even when
   `GetTypeInfo().Type` returns null, and vice versa. So the order should be: try
   `GetSymbolInfo` → then `GetTypeInfo().Type?.ToDisplayString(FullyQualifiedFormat)` →
   and only emit the diagnostic when BOTH genuinely fail (truly unresolvable type). Emit a
   fully-qualified name whenever one resolves; reserve the diagnostic for the actually-
   broken case. This is a refinement, not a reversal — the plan's diagnostic is still the
   last resort. (If you prefer the plan's stricter "diagnostic on any GetSymbolInfo miss,"
   that is a defensible call too; flagging the choice, not forcing it.)

4. **Diagnostic IDs.** The plan proposes NURU_S011/S012. Next free NURU_S id is **S009**
   (only S001–S008 + S999 are in use — same finding as the 454-011 review). Use S009, then
   S010, in order; do not skip to S011/S012. Add each to `AnalyzerReleases.Unshipped.md`.
   Also check whether an EXISTING descriptor already fits before minting a new one (454-011
   found NURU_H005 was defined-but-never-emitted for exactly its case).
