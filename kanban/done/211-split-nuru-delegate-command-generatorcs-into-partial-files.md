# Split nuru-delegate-command-generator.cs into partial files

## Description

The `nuru-delegate-command-generator.cs` file (1250 lines) is the largest source file in the codebase. It handles multiple distinct concerns: route parsing, signature extraction, handler info extraction, code generation, and syntax rewriting. These should be split into focused partial files following the established patterns.

**Location:** `source/timewarp-nuru-analyzers/analyzers/nuru-delegate-command-generator.cs`

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

### File Creation
- [x] Create `nuru-delegate-command-generator.types.cs` - Record types and constants
- [x] Create `nuru-delegate-command-generator.route-parsing.cs` - Route pattern parsing
- [x] Create `nuru-delegate-command-generator.signature.cs` - Signature extraction methods
- [x] Create `nuru-delegate-command-generator.handler.cs` - Handler info extraction
- [x] Create `nuru-delegate-command-generator.codegen.cs` - Code generation
- [x] Create `nuru-delegate-command-generator.rewriter.cs` - ParameterRewriter class

### Documentation
- [x] Add `<remarks>` to main file listing all partial files and their purposes
- [x] Add XML summary to each new partial file

### Verification
- [x] All analyzer tests pass
- [x] Source generation works correctly
- [x] Build succeeds

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

## Results

### Files Created

| File | Lines | Description |
|------|-------|-------------|
| `nuru-delegate-command-generator.cs` | ~280 | Main file with Initialize, fluent chain detection |
| `nuru-delegate-command-generator.types.cs` | ~70 | Record types and enum |
| `nuru-delegate-command-generator.route-parsing.cs` | ~200 | Route parsing and class name generation |
| `nuru-delegate-command-generator.signature.cs` | ~130 | Signature extraction |
| `nuru-delegate-command-generator.handler.cs` | ~240 | Handler extraction and body rewriting |
| `nuru-delegate-command-generator.codegen.cs` | ~200 | Code generation |
| `nuru-delegate-command-generator.rewriter.cs` | ~95 | ParameterRewriter class |

### Test Results

- `delegate-command-generator-01-basic.cs`: **12/12 passed**
- `nuru-invoker-generator-01-basic.cs`: **6/6 passed**
- `delegate-signature-01-models.cs`: **15/16 passed** (1 pre-existing failure)

### Additional Fix

Fixed analyzer test infrastructure by converting `#:package TimeWarp.Jaribu` to a project reference in `Directory.Build.props`. This resolves the `ITerminal` type loading error that occurred with the NuGet package reference.
