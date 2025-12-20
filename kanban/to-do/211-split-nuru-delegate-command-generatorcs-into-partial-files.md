# Split nuru-delegate-command-generator.cs into partial files

## Description

The `nuru-delegate-command-generator.cs` file (1250 lines) is the largest source file in the codebase. It handles multiple distinct concerns: route parsing, signature extraction, handler info extraction, code generation, and syntax rewriting. These should be split into focused partial files following the established patterns.

**Location:** `source/timewarp-nuru-analyzers/analyzers/nuru-delegate-command-generator.cs`

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

### File Creation
- [ ] Create `nuru-delegate-command-generator.types.cs` - Record types and constants
- [ ] Create `nuru-delegate-command-generator.route-parsing.cs` - Route pattern parsing
- [ ] Create `nuru-delegate-command-generator.signature.cs` - Signature extraction methods
- [ ] Create `nuru-delegate-command-generator.handler.cs` - Handler info extraction
- [ ] Create `nuru-delegate-command-generator.codegen.cs` - Code generation
- [ ] Create `nuru-delegate-command-generator.rewriter.cs` - ParameterRewriter class

### Documentation
- [ ] Add `<remarks>` to main file listing all partial files and their purposes
- [ ] Add XML summary to each new partial file

### Verification
- [ ] All analyzer tests pass
- [ ] Source generation works correctly
- [ ] Build succeeds

## Notes

### Proposed Split

| New File | ~Lines | Content |
|----------|--------|---------|
| `.types.cs` | ~100 | `DelegateCommandInfo`, `HandlerInfo`, `ParameterClassification`, `FluentChainInfo`, `RouteParameterInfo`, `RouteParamMatch` records |
| `.route-parsing.cs` | ~120 | `ParseRoutePattern()`, `FindRouteParameter()`, `GenerateClassName()` |
| `.signature.cs` | ~130 | `ExtractSignatureFromHandler()`, `ExtractSignatureFromLambda()`, `ExtractSignatureFromMethodGroup()`, `CreateSignatureFromMethod()` |
| `.handler.cs` | ~200 | `ExtractHandlerInfo()`, `DetectClosures()`, `RewrittenBodyToString()`, `WrapReturnStatementsInValueTask()` |
| `.codegen.cs` | ~170 | `GenerateCommandClasses()`, `GenerateCommandClass()`, `GenerateHandlerClass()` |
| `.rewriter.cs` | ~90 | `ParameterRewriter` nested class (complete, self-contained unit) |
| Main file | ~440 | `Initialize()`, fluent chain detection, core orchestration |

### Dependencies to Consider

- Record types are used across all concerns - must be in shared types file or main file
- `ToPascalCase()` helper is used by multiple concerns
- FluentChainInfo needs to be accessible to both detection and extraction logic

### Reference Pattern

Follow `endpoint-resolver.cs` partial class organization:
```csharp
/// <summary>
/// Source generator for delegate-based commands.
/// </summary>
/// <remarks>
/// This class is split into partial classes for maintainability:
/// - nuru-delegate-command-generator.cs: Core initialization and chain detection
/// - nuru-delegate-command-generator.types.cs: Record definitions
/// - nuru-delegate-command-generator.route-parsing.cs: Route pattern parsing
/// - ...
/// </remarks>
```
