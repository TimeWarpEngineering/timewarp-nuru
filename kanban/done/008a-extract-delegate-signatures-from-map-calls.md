# Extract Delegate Signatures from Map() Calls

## Description

Extend NuruRouteAnalyzer to extract delegate parameter types and return types from Map() invocations. This provides the foundational data model needed for generating typed invokers.

## Parent

008-implement-source-generators-for-reflection-free-routing

## Requirements

- Extract the lambda/delegate from the second argument of Map() calls
- Determine parameter types (string, int, bool, arrays, etc.)
- Determine return type (void, int, Task, Task<int>)
- Create a model to represent the extracted signature
- Handle edge cases (generic types, nullable types, array types)

## Checklist

### Implementation
- [x] Analyze Map() invocation syntax tree structure
- [x] Extract delegate expression from second argument
- [x] Parse parameter types from delegate signature
- [x] Parse return type from delegate signature
- [x] Create `DelegateSignature` model class
- [x] Handle Action<> vs Func<> delegates
- [x] Handle async delegates (Task/Task<T> returns)
- [x] Support catch-all array parameters

### Testing
- [x] Add analyzer tests for various delegate signatures
- [x] Test with void, int, Task, Task<int> return types
- [x] Test with string, int, bool, double parameters
- [x] Test with array parameters
- [x] Test with nullable parameters

## Notes

Reference the martinothamar/Mediator source generator for analysis patterns:
- `Implementation/Analysis/CompilationAnalyzer.cs` - Code analysis patterns
- `Implementation/Models/` - Data models for analyzed code

The signature model should capture:
```csharp
public record DelegateSignature(
    IReadOnlyList<ParameterInfo> Parameters,
    TypeInfo ReturnType,
    bool IsAsync,
    string UniqueIdentifier);

public record ParameterInfo(
    string Name,
    TypeInfo Type,
    bool IsArray,
    bool IsNullable);
```

## Results

### Implementation Complete

Created the following files in `source/timewarp-nuru-analyzers/`:

1. **models/delegate-signature.cs** - Contains:
   - `DelegateSignature` - Main record representing an extracted delegate signature
   - `DelegateParameterInfo` - Parameter information (name, type, array/nullable flags)
   - `DelegateTypeInfo` - Type information with support for Task/ValueTask detection
   - `RouteWithSignature` - Extended route info including extracted signature

2. **analyzers/delegate-signature-extractor.cs** - Contains:
   - `DelegateSignatureExtractor.ExtractSignature()` - Main extraction method
   - Handles lambda expressions, method groups, and delegate creations
   - Extracts parameter types, return types, and async detection

### Key Design Decisions

- Named `DelegateTypeInfo` to avoid conflict with `Microsoft.CodeAnalysis.TypeInfo`
- Types are internal since they're source generator implementation details
- Short names for identifiers map special types (int -> "Int", string -> "String")
- Unique identifiers format: `{Param1Type}_{Param2Type}_Returns_{ReturnType}`

### Testing

Created integration tests in `tests/timewarp-nuru-analyzers-tests/delegate-signature-01-models.cs`:
- 16 tests covering various delegate signatures (all passing)
- Tests verify compilation and syntax tree extraction
- Covers: void, int, Task, Task<int>, string[], double, Guid, nullable params
