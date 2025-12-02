# Fix unified-middleware sample false pipeline distinction

## Description

The `samples/unified-middleware/unified-middleware.cs` sample creates a confusing and incorrect mental model by suggesting there are TWO separate pipelines:
- `[DELEGATE PIPELINE]` behaviors
- `[MEDIATOR PIPELINE]` behaviors

This is **wrong**. The architecture shows that delegate routes are wrapped in `DelegateRequest` and sent through `IMediator.Send()`, meaning **everything flows through the same Mediator pipeline**.

The redundant `DelegateLoggingBehavior` and `DelegatePerformanceBehavior` classes confuse users into thinking they need separate behaviors for delegate vs mediator routes, when open generic behaviors (`LoggingBehavior<,>`) already apply to ALL requests including `DelegateRequest`.

## Requirements

- Remove the false distinction between "delegate pipeline" and "mediator pipeline"
- Demonstrate that ONE set of open generic pipeline behaviors applies to ALL routes
- Show that `DelegateRequest` appears in behavior logs for delegate routes
- Show that specific command types (e.g., `EchoCommand`) appear for mediator routes
- Update comments to explain the unified pipeline architecture correctly

## Checklist

### Implementation
- [ ] Remove `DelegateLoggingBehavior` class
- [ ] Remove `DelegatePerformanceBehavior` class
- [ ] Update `ConfigureServices` to only register open generic behaviors
- [ ] Update header comments to explain unified pipeline correctly
- [ ] Update `TRY THESE COMMANDS` section to show unified logging output
- [ ] Verify sample compiles and runs correctly

### Documentation
- [ ] Ensure comments explain that `DelegateRequest` is just another `IRequest<T>`
- [ ] Clarify that open generics catch ALL request types

## Notes

Key code showing the unified architecture (from `nuru-core-app.cs` line 341):
```csharp
DelegateResponse response = await mediator.Send(request, CancellationToken.None).ConfigureAwait(false);
```

And from `delegate-request.cs`:
```csharp
public sealed class DelegateRequest : IRequest<DelegateResponse>
```

The sample should demonstrate:
```csharp
services.AddMediator(options =>
{
  options.PipelineBehaviors =
  [
    typeof(LoggingBehavior<,>),      // Applies to ALL requests including DelegateRequest
    typeof(PerformanceBehavior<,>),  // Applies to ALL requests including DelegateRequest
  ];
});
```

Expected output should show:
- `[PIPELINE] Handling DelegateRequest` for delegate routes
- `[PIPELINE] Handling EchoCommand` for mediator routes
- Same behavior class, different request types
