# Support method group handlers in NuruDelegateCommandGenerator

## Description

The `NuruDelegateCommandGenerator` currently only supports lambda expressions in `.WithHandler()`. When consumers use method groups (static methods, instance methods, local functions), the generator creates a Command/Query class but skips handler generation, resulting in orphan types that cause MSG0005 warnings.

Following .NET Minimal APIs conventions, all handler types should be supported:
- Lambda expressions (already works)
- Local functions
- Static methods  
- Instance methods

## Checklist

- [x] Refactor `ExtractHandlerInfo` to detect handler expression type (lambda vs method group)
- [x] Add `ExtractMethodGroupHandlerInfo` to handle `IdentifierNameSyntax` and `MemberAccessExpressionSyntax`
- [x] Generate method call expression: `MethodName(request.Param1, request.Param2, DiField1)`
- [x] Handle async methods properly
- [x] Handle methods with DI parameters
- [x] Update provenance to show handler type correctly
- [x] Add test case: static method handler
- [x] Add test case: local function handler (uses static method test pattern)
- [x] Add test case: async static method handler
- [x] Add test case: method with DI parameters
- [x] Add test case: instance method handler → reports NURU_H001 (deferred to task 236)
- [x] Verify MSG0005 warning is resolved for `--capabilities` route
- [x] Create NuruHandlerAnalyzer with NURU_H001, NURU_H002, NURU_H003, NURU_H004 diagnostics
- [x] Fix duplicate class name conflict (NuruAppBuilderExtensions → NuruAppBuilderCompletionExtensions)

## Notes

### Current behavior

In `nuru-delegate-command-generator.handler.cs:18-20`:
```csharp
// Only support lambda expressions for now (method groups deferred)
if (handlerExpression is not LambdaExpressionSyntax lambda)
  return null;
```

This causes the generator to emit a Command/Query class WITHOUT a Handler when a method group is used, resulting in MSG0005 from the Mediator source generator.

### Desired behavior

```csharp
if (handlerExpression is LambdaExpressionSyntax lambda)
  return ExtractLambdaHandlerInfo(...);
else if (handlerExpression is IdentifierNameSyntax or MemberAccessExpressionSyntax)
  return ExtractMethodGroupHandlerInfo(...);
else
  return null;
```

### Method group handler generation example

For `.WithHandler(DisplayCapabilitiesAsync)`, generate:
```csharp
public sealed class Handler : IQueryHandler<Default_Generated_Query, Unit>
{
  public ValueTask<Unit> Handle(Default_Generated_Query request, CancellationToken ct)
  {
    DisplayCapabilitiesAsync(/* mapped params */);
    return default;
  }
}
```

### .NET Minimal APIs reference

Minimal APIs supports all of these handler types:
- Lambda expressions: `app.MapGet("/", () => "Hello");`
- Local functions: `app.MapGet("/", LocalFunction);`
- Instance methods: `app.MapGet("/", handler.Hello);`
- Static methods: `app.MapGet("/", HelloHandler.Hello);`

TimeWarp.Nuru should match this behavior.

### Root cause of MSG0005

The `--capabilities` route in `nuru-app-builder-extensions.capabilities.cs:43` uses:
```csharp
.WithHandler(DisplayCapabilitiesAsync)  // method group, not lambda
```

This generates `Default_Generated_Query` (correct name for option-only pattern) but without a Handler class, causing Mediator to report MSG0005.

### Files to Modify

- `source/timewarp-nuru-analyzers/analyzers/nuru-delegate-command-generator.handler.cs`
- `source/timewarp-nuru-analyzers/analyzers/nuru-delegate-command-generator.cs` (if needed)
- `tests/timewarp-nuru-analyzers-tests/` - add test cases

### Blocks

Task 232 (Address sample compilation warnings) - MSG0005 will be fixed once this is implemented.
