# Create Overview Documentation

## Description

Create comprehensive Overview.md documentation explaining pipeline middleware concepts, behavior ordering, and comparison with other CLI frameworks.

## Parent

076_Add-Pipeline-Middleware-Sample

## Checklist

- [x] Create `samples/pipeline-middleware/Overview.md`
- [x] Explain pipeline behavior concept
- [x] Document execution order (outermost to innermost)
- [x] Describe each behavior's purpose
- [x] Show registration order diagram
- [x] Compare with Cocona's attribute-based filters
- [x] Include usage examples
- [x] Document marker interface pattern

## Results

Created comprehensive `samples/pipeline-middleware/Overview.md` with:

1. **Pipeline concept explanation** - Onion-layer ASCII diagram showing behavior wrapping
2. **Execution order documentation** - Clear explanation of outermost/innermost semantics
3. **Behavior descriptions** - Code examples for all 5 behaviors:
   - LoggingBehavior
   - PerformanceBehavior
   - AuthorizationBehavior
   - RetryBehavior
   - ExceptionHandlingBehavior
4. **Registration order** - With code example showing proper ordering
5. **Cocona comparison table** - Attribute-based filters vs pipeline behaviors
6. **Usage examples** - Complete CLI commands for all sample commands
7. **Marker interface pattern** - Explanation with IRequireAuthorization and IRetryable examples
8. **AOT considerations** - Guidance on explicit vs open generic registration
9. **Key benefits** - Separation of concerns, composability, testability

## Notes

Documentation should cover:

### Pipeline Execution Order
```
Request → Telemetry → Performance → Logging → Auth → Validation → Retry → Exception → Handler
Response ← Telemetry ← Performance ← Logging ← Auth ← Validation ← Retry ← Exception ← Handler
```

### Registration Order
First registered = outermost (executes first on request, last on response)
Last registered = innermost (executes last on request, first on response)

### Key Concepts
- Open generic registration: `services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))`
- Marker interfaces for selective behavior
- Composition over inheritance
- Testability of individual behaviors
