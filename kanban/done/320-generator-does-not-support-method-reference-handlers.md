# Generator Does Not Support Method Reference Handlers

## Description

`.WithHandler(MethodName)` with a method reference fails at runtime with unhelpful error:
```
System.NotSupportedException: Handler code was not captured at compile time.
```

Generator should either support method references or provide actionable error message.

## Checklist

- [x] Create regression test `generator-12-method-reference-handlers.cs`
- [x] Debug why `semanticModel.GetSymbolInfo()` fails in `HandlerExtractor.ExtractFromMethodGroup()`
- [x] Fix handler extraction to properly resolve method symbols
- [x] Verify `samples/08-testing/runfile-test-harness/real-app.cs` works
- [x] Run full test suite to verify no regressions

## Notes

- Root cause: For method groups, `GetSymbolInfo().Symbol` is null but method is in `CandidateSymbols`
- Fix: Check `CandidateSymbols[0]` when `Symbol` is null in both `ExtractFromMethodGroup()` and `ExtractFromMemberAccess()`
- Hyphenated options (`--dry-run`) already work correctly - generates `bool dryRun` (camelCase)
