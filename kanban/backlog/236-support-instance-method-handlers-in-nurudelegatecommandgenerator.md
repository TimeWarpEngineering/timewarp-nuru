# Support instance method handlers in NuruDelegateCommandGenerator

## Description

Follow-up to task 235 (support for static methods and local functions). Instance methods (e.g., `myService.DoWork`) require capturing the instance, which is similar to closure behavior but with an important difference: the instance can potentially come from DI.

For instance method handlers like `.WithHandler(myHandler.Process)` or `.WithHandler(_service.Execute)`, the generator would need to:
1. Detect that this is an instance method (not static or local function)
2. Determine the instance type
3. Generate a handler that injects the instance type from DI and calls the method

This is more complex than static methods because the instance must be resolvable from the DI container at runtime.

## Checklist

- [ ] Detect instance method patterns in `MemberAccessExpressionSyntax`
- [ ] Extract the instance type from the expression
- [ ] Determine if the instance can be resolved from DI (by type)
- [ ] Generate handler that injects the instance type and calls the method
- [ ] Handle async instance methods properly
- [ ] Add test case: instance method on DI-resolved service
- [ ] Add test case: instance method with additional DI parameters

## Notes

### Blocked by

Task 235 (Support method group handlers in NuruDelegateCommandGenerator) - static method and local function support must be implemented first. **COMPLETED**

### Scope Limitations

- Instance methods on local variables (closures) will NOT be supported - these cannot be resolved from DI
- Only instance methods where the instance type can be resolved via DI should work
- If the instance cannot be resolved from DI, NURU_H001 error is already reported

### Example Generation

For `.WithHandler(_myService.ProcessAsync)` where `MyService` is registered in DI:

```csharp
public sealed class Handler : IQueryHandler<SomeQuery, Unit>
{
  private readonly MyService _myService;
  
  public Handler(MyService myService) => _myService = myService;
  
  public ValueTask<Unit> Handle(SomeQuery request, CancellationToken ct)
  {
    return _myService.ProcessAsync(request.Param1, request.Param2, ct);
  }
}
```

### Current Behavior

Instance methods currently trigger NURU_H001 error from `NuruHandlerAnalyzer`:
```
error NURU_H001: Instance method 'handler.Process' cannot be used as handler. Use a lambda, static method, or local function instead.
```

### Files to Modify

- `source/timewarp-nuru-analyzers/analyzers/nuru-handler-analyzer.cs` - Update to allow DI-resolvable instance methods
- `source/timewarp-nuru-analyzers/analyzers/nuru-delegate-command-generator.handler.cs` - Add instance method support
- `tests/timewarp-nuru-analyzers-tests/` - Add test cases
