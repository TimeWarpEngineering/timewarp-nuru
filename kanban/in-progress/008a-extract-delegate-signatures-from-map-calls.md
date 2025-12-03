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
- [ ] Analyze Map() invocation syntax tree structure
- [ ] Extract delegate expression from second argument
- [ ] Parse parameter types from delegate signature
- [ ] Parse return type from delegate signature
- [ ] Create `DelegateSignature` model class
- [ ] Handle Action<> vs Func<> delegates
- [ ] Handle async delegates (Task/Task<T> returns)
- [ ] Support catch-all array parameters

### Testing
- [ ] Add analyzer tests for various delegate signatures
- [ ] Test with void, int, Task, Task<int> return types
- [ ] Test with string, int, bool, double parameters
- [ ] Test with array parameters
- [ ] Test with nullable parameters

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
