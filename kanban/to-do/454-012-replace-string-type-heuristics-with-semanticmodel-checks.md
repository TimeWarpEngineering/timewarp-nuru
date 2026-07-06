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
