# Split nuru-invoker-generator.cs into partial files

## Description

The `nuru-invoker-generator.cs` file (541 lines) generates AOT-compatible invokers. It handles signature extraction and code generation as distinct concerns that should be separated into partial files.

**Location:** `source/timewarp-nuru-analyzers/analyzers/nuru-invoker-generator.cs`

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

### File Creation
- [ ] Create `nuru-invoker-generator.extraction.cs` - Signature extraction methods
- [ ] Create `nuru-invoker-generator.codegen.cs` - Code generation methods

### Documentation
- [ ] Add `<remarks>` to main file listing all partial files
- [ ] Add XML summary to each new partial file

### Verification
- [ ] All analyzer tests pass
- [ ] Source generation works correctly
- [ ] Build succeeds

## Notes

### Proposed Split

| New File | ~Lines | Content |
|----------|--------|---------|
| `.extraction.cs` | ~130 | `ExtractSignatureFromHandler()`, `ExtractFromLambda()`, `ExtractFromMethodGroup()`, `ExtractFromDelegateCreation()`, `ExtractFromExpression()`, `CreateSignatureFromMethod()` |
| `.codegen.cs` | ~230 | `GenerateInvokersClass()`, `GenerateModuleInitializer()`, `GenerateInvokerMethod()`, `GenerateLookupDictionary()`, helper methods |
| Main file | ~180 | `Initialize()`, `IsMapInvocation()`, `GetRouteWithSignature()`, `FindPatternFromFluentChain()`, detection logic |

### Benefits

- Signature extraction logic is complex and testable independently
- Code generation is self-contained with StringBuilder building
- Detection logic in main file is the entry point orchestration

### Reference Pattern

Follow established partial class conventions with XML documentation.
