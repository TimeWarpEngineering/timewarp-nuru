# Pipeline Middleware

Demonstrates the `INuruBehavior` pipeline for cross-cutting concerns.

## Run It

```bash
dotnet run samples/07-pipeline-middleware/01-pipeline-middleware-basic.cs -- echo "Hello"
dotnet run samples/07-pipeline-middleware/01-pipeline-middleware-basic.cs -- slow 600
dotnet run samples/07-pipeline-middleware/02-pipeline-middleware-exception.cs -- fail
dotnet run samples/07-pipeline-middleware/03-pipeline-middleware-telemetry.cs -- work 100
```

## What's Demonstrated

- **01-basic**: Logging and performance timing behaviors
- **02-exception**: Exception handling and retry patterns
- **03-telemetry**: OpenTelemetry integration
- **04-filtered-auth**: Route-filtered authorization
- **05-retry**: Automatic retry with exponential backoff
- **06-combined**: Multiple behaviors working together

## Related Documentation

- [Pipeline Behaviors](../../documentation/user/features/pipeline-behaviors.md)
