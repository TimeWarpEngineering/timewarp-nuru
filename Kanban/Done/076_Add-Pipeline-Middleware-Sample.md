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
- [x] Create TelemetryBehavior (OpenTelemetry/Activity tracing)
- [x] Create PerformanceBehavior (Stopwatch timing with slow-command warnings)
- [x] Create LoggingBehavior (request/response logging)
- [x] Create ValidationBehavior (input validation) - handled via ExceptionHandlingBehavior
- [x] Create AuthorizationBehavior (permission checks via marker interface)
- [x] Create RetryBehavior (resilience for IRetryable commands)
- [x] Create ExceptionHandlingBehavior (consistent error handling)
- [x] Create sample commands demonstrating each behavior
- [x] Register all behaviors in correct order

### Documentation
- [x] Create Overview.md explaining pipeline concepts
- [x] Document behavior ordering and why it matters
- [x] Add entry to examples.json

### Verification
- [x] Sample compiles and runs
- [x] Pipeline behaviors execute in correct order
- [x] Output demonstrates each behavior's effect

## Results

Comprehensive Pipeline Middleware sample completed with 6 pipeline behaviors:

1. **TelemetryBehavior** - OpenTelemetry-compatible Activity spans for distributed tracing
2. **LoggingBehavior** - Request entry/exit logging with error capture
3. **PerformanceBehavior** - Stopwatch timing with 500ms slow-command warnings
4. **AuthorizationBehavior** - Permission checks via IRequireAuthorization marker interface
5. **RetryBehavior** - Exponential backoff retry for IRetryable commands
6. **ExceptionHandlingBehavior** - Differentiated error handling with user-friendly messages

### Sample Commands
- `echo {message}` - Basic pipeline demonstration
- `slow {delay:int}` - Performance warning trigger
- `admin {action}` - Authorization via CLI_AUTHORIZED env var
- `flaky {failCount:int}` - Retry behavior with simulated failures
- `error {errorType}` - Exception handling (validation, auth, argument, unknown)
- `trace {operation}` - Telemetry/Activity demonstration

### Files Created
- `Samples/PipelineMiddleware/pipeline-middleware.cs` - Main sample (580+ lines)
- `Samples/PipelineMiddleware/Overview.md` - Comprehensive documentation
- `Samples/examples.json` - MCP discovery entries added

All 8 subtasks completed:
- 076_001: Create sample structure
- 076_002: Performance Behavior
- 076_003: Telemetry Behavior
- 076_004: Authorization Behavior
- 076_005: Retry Behavior
- 076_006: Exception Handling Behavior
- 076_007: Overview Documentation
- 076_008: Register in examples.json

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
