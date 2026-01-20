# Unified Middleware

Demonstrates that delegate routes and attributed routes share the same behavior pipeline.

## Run It

```bash
# Delegate routes (inline handlers)
dotnet run samples/11-unified-middleware/unified-middleware.cs -- add 5 3
dotnet run samples/11-unified-middleware/unified-middleware.cs -- greet World

# Attributed routes ([NuruRoute] commands)
dotnet run samples/11-unified-middleware/unified-middleware.cs -- echo "hello"
dotnet run samples/11-unified-middleware/unified-middleware.cs -- slow 600
```

## What's Demonstrated

- ONE unified behavior pipeline for ALL routes
- `LoggingBehavior` wraps both delegate and attributed routes
- `PerformanceBehavior` monitors execution time for all routes
- Mix delegate and attributed routes freely in the same app

## Related Documentation

- [Pipeline Behaviors](../../documentation/user/features/pipeline-behaviors.md)
- [Attributed Routes](../../documentation/user/features/attributed-routes.md)
