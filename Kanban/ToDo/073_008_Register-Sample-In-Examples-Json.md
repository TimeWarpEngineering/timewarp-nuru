# Register Sample in examples.json

## Description

Add the Pipeline Middleware sample to the examples.json manifest for MCP server discovery.

## Parent

073_Add-Pipeline-Middleware-Sample

## Checklist

- [ ] Add entry to `Samples/examples.json`
- [ ] Use appropriate id, name, description
- [ ] Add relevant tags (pipeline, middleware, mediator, behaviors, cross-cutting-concerns)
- [ ] Set difficulty to intermediate or advanced
- [ ] Verify MCP server can discover the example

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
