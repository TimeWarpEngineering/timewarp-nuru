# Hello World

The simplest possible TimeWarp.Nuru applications demonstrating three handler patterns.

## Run It

```bash
dotnet run samples/01-hello-world/01-hello-world-lambda.cs
dotnet run samples/01-hello-world/02-hello-world-method.cs
dotnet run samples/01-hello-world/03-hello-world-attributed.cs
```

## What's Demonstrated

- **01-hello-world-lambda.cs**: Inline lambda handler pattern (simplest approach)
- **02-hello-world-method.cs**: Method reference handler pattern
- **03-hello-world-attributed.cs**: Attributed route with `[NuruRoute]`

## Related Documentation

- [Routing](../../documentation/user/features/routing.md)
- [Attributed Routes](../../documentation/user/features/attributed-routes.md)
