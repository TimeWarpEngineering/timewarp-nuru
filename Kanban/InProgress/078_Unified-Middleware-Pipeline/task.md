# Unified Middleware Pipeline

## Description

Implement unified middleware behavior across both delegate and Mediator-based routes, ensuring cross-cutting concerns apply consistently regardless of route registration style.

This is a multi-phase effort:
1. First, migrate from TimeWarp.Mediator to martinothamar/Mediator for better AOT support
2. Then, implement DelegateHandler to route delegate executions through the Mediator pipeline

## Problem Statement

When mixing delegate and Mediator approaches (see `Samples/Calculator/calc-mixed.cs`), middleware like logging, metrics, or validation only applies to Mediator commands. Users reasonably expect cross-cutting concerns to apply uniformly to all routes.

## Subtasks

- 078_001: Migrate to martinothamar/Mediator
- 078_002: Implement unified DelegateHandler

## References

- `Samples/Calculator/calc-mixed.cs` - Mixed delegate/Mediator example
- `Samples/PipelineMiddleware/pipeline-middleware.cs` - Pipeline behavior example
- https://www.jimmybogard.com/sharing-context-in-mediatr-pipelines/
- https://github.com/martinothamar/Mediator
- https://github.com/TimeWarpEngineering/martinothamar-mediator
