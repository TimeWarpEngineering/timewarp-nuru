# Unified Middleware

Demonstrates that delegate routes and endpoints share the same behavior pipeline.

## Run It

```bash
# Delegate routes (inline handlers)
dotnet run samples/11-unified-middleware/unified-middleware.cs -- add 5 3
dotnet run samples/11-unified-middleware/unified-middleware.cs -- greet World

# Endpoints ([NuruRoute] commands)
dotnet run samples/11-unified-middleware/unified-middleware.cs -- echo "hello"
dotnet run samples/11-unified-middleware/unified-middleware.cs -- slow 600
```

## What's Demonstrated

- ONE unified behavior pipeline for ALL routes
- `LoggingBehavior` wraps both delegate routes and endpoints
- `PerformanceBehavior` monitors execution time for all routes
- Mix delegate routes and endpoints freely in the same app

## Related Documentation

- [Pipeline Behaviors](../../documentation/user/features/pipeline-behaviors.md)
- [Endpoints](../../documentation/user/features/endpoints.md)
