# Register Sample in examples.json

## Description

Add the Pipeline Middleware sample to the examples.json manifest for MCP server discovery.

## Parent

076_Add-Pipeline-Middleware-Sample

## Checklist

- [x] Add entry to `Samples/examples.json`
- [x] Use appropriate id, name, description
- [x] Add relevant tags (pipeline, middleware, mediator, behaviors, cross-cutting-concerns)
- [x] Set difficulty to intermediate or advanced
- [x] Verify MCP server can discover the example

## Results

Added two entries to `Samples/examples.json`:

1. **pipeline-middleware** (advanced):
   - Cross-cutting concerns with pipeline behaviors
   - Tags: pipeline, middleware, mediator, behaviors, cross-cutting-concerns, telemetry, performance, enterprise

2. **unified-middleware** (advanced):
   - Pipeline behaviors for both delegate and Mediator routes
   - Tags: pipeline, middleware, delegate, mediator, unified, cross-cutting-concerns

JSON validated successfully. MCP server will discover these examples via the manifest.

## Notes

Example entry:
```json
{
  "id": "pipeline-middleware",
  "name": "Mediator Pipeline Middleware",
  "description": "Cross-cutting concerns with pipeline behaviors: telemetry, performance, logging, authorization, validation, retry, and exception handling",
  "path": "Samples/PipelineMiddleware/pipeline-middleware.cs",
  "tags": ["pipeline", "middleware", "mediator", "behaviors", "cross-cutting-concerns", "telemetry", "performance", "enterprise"],
  "difficulty": "advanced"
}
```
