# Emit inlined pipeline middleware

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Generate inlined middleware/pipeline code instead of using runtime `IPipelineBehavior` dispatch. The generator sees all behavior registrations in source code and emits the chain directly.

## Requirements

- Detect middleware/behavior registrations in user code
- Emit inlined pre/post processing code
- No virtual dispatch or behavior resolution at runtime
- Maintain correct execution order (outside-in for pre, inside-out for post)

## Checklist

- [ ] Analyze how behaviors are currently registered
- [ ] Add behavior detection to source generator
- [ ] Design inlined pipeline code structure
- [ ] Emit pre-processor calls before handler
- [ ] Emit post-processor calls after handler
- [ ] Handle exception behaviors (try/catch wrapping)
- [ ] Test pipeline order matches runtime behavior
- [ ] Verify logging/timing behaviors work correctly

## Notes

### Current Runtime Approach

```csharp
// User registers
builder.AddLoggingBehavior();
builder.AddValidationBehavior();

// Runtime resolves and chains behaviors
foreach (var behavior in behaviors)
    result = await behavior.Handle(request, next);
```

### Generated Approach

```csharp
// Generator emits
static async Task<object?> ExecuteWithPipeline(int idx, object[] args)
{
    // Logging pre-processor (inlined)
    var sw = Stopwatch.StartNew();
    Log.Debug("Starting command {Idx}", idx);
    
    try
    {
        // Validation pre-processor (inlined)
        Validate(idx, args);
        
        // Handler
        var result = await Handlers[idx](args);
        
        // Logging post-processor (inlined)
        Log.Debug("Completed in {Ms}ms", sw.ElapsedMilliseconds);
        
        return result;
    }
    catch (Exception ex)
    {
        // Exception behavior (inlined)
        Log.Error(ex, "Failed");
        throw;
    }
}
```

### Benefits

- No behavior resolution overhead
- No virtual dispatch
- JIT can inline everything
- Smaller code if behaviors are simple
