# Add Pipeline Middleware Sample

## Description

Create a comprehensive sample demonstrating TimeWarp.Mediator pipeline behaviors (middleware) for cross-cutting concerns like telemetry, performance measuring, logging, validation, authorization, retry/resilience, and exception handling.

This sample will showcase the power of the Mediator pattern's pipeline compared to attribute-based filters, demonstrating how behaviors compose and execute in order.

## Requirements

- Create `Samples/PipelineMiddleware/` directory
- Implement runnable sample `pipeline-middleware.cs`
- Create `Overview.md` documentation
- Register sample in `examples.json`
- Demonstrate at least 5 different pipeline behaviors
- Show pipeline ordering (registration order = execution order)
- Use marker interfaces for selective behavior application

## Checklist

### Implementation
- [ ] Create TelemetryBehavior (OpenTelemetry/Activity tracing)
- [ ] Create PerformanceBehavior (Stopwatch timing with slow-command warnings)
- [ ] Create LoggingBehavior (request/response logging)
- [ ] Create ValidationBehavior (input validation)
- [ ] Create AuthorizationBehavior (permission checks via marker interface)
- [ ] Create RetryBehavior (resilience for IRetryable commands)
- [ ] Create ExceptionHandlingBehavior (consistent error handling)
- [ ] Create sample commands demonstrating each behavior
- [ ] Register all behaviors in correct order

### Documentation
- [ ] Create Overview.md explaining pipeline concepts
- [ ] Document behavior ordering and why it matters
- [ ] Add entry to examples.json

### Verification
- [ ] Sample compiles and runs
- [ ] Pipeline behaviors execute in correct order
- [ ] Output demonstrates each behavior's effect

## Notes

### Pipeline Execution Order (outermost to innermost)
1. TelemetryBehavior - distributed tracing spans
2. PerformanceBehavior - command timing
3. LoggingBehavior - request/response logging
4. AuthorizationBehavior - permission checks
5. ValidationBehavior - input validation
6. RetryBehavior - resilience for network ops
7. ExceptionHandlingBehavior - consistent error handling

### Marker Interfaces
- `IRetryable` - commands that should be retried on transient failures
- `IRequireAuthorization` - commands requiring permission checks

### Key Differentiators from Cocona
- Behaviors are decoupled from commands (no attributes)
- Behaviors are registered in DI container
- Easy to add/remove behaviors without touching commands
- Full testability in isolation
