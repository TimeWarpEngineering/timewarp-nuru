# Create Overview Documentation

## Description

Create comprehensive Overview.md documentation explaining pipeline middleware concepts, behavior ordering, and comparison with other CLI frameworks.

## Parent

073_Add-Pipeline-Middleware-Sample

## Checklist

- [ ] Create `Samples/PipelineMiddleware/Overview.md`
- [ ] Explain pipeline behavior concept
- [ ] Document execution order (outermost to innermost)
- [ ] Describe each behavior's purpose
- [ ] Show registration order diagram
- [ ] Compare with Cocona's attribute-based filters
- [ ] Include usage examples
- [ ] Document marker interface pattern

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
