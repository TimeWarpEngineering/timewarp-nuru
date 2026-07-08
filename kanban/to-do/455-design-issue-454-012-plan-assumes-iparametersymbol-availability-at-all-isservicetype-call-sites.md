# Design issue: 454-012 plan assumes IParameterSymbol availability at all IsServiceType call sites

## Description

The implementation plan for 454-012 (M8) assumes all `IsServiceType` call sites have `IParameterSymbol`/`ITypeSymbol` available. This is false — the lambda extraction path (`handler-extractor.cs:103,263`) only has `RoslynParameterSyntax` nodes and derives `typeName` via string operations. Replacing the string heuristic requires first binding lambda parameters to symbols via `SemanticModel.GetSymbolInfo`/`GetTypeInfo` — a non-trivial refactor, not a simple signature change. This is an architectural mismatch between the plan's assumption and the actual dual-path design (syntax-based lambda extraction vs symbol-based method extraction).

## Checklist

- [ ] Document root cause and affected call sites
- [ ] Identify required refactor scope for lambda binding

## Notes

Created as blocker for 454-012. See handler-extractor.cs lines 103 and 263 for the syntax-only lambda path.
