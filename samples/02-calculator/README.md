# Calculator

A calculator CLI demonstrating multiple route patterns and typed parameters.

## Run It

```bash
dotnet run samples/02-calculator/01-calc-delegate.cs -- add 5 3
dotnet run samples/02-calculator/01-calc-delegate.cs -- multiply 4 7
dotnet run samples/02-calculator/01-calc-delegate.cs -- round 3.7 --mode up
```

## What's Demonstrated

- **01-calc-delegate.cs**: Delegate handlers with typed `{x:double}` parameters
- **02-calc-commands.cs**: Command pattern with attributed routes
- **03-calc-mixed.cs**: Mixing delegate and attributed routes

## Related Documentation

- [Routing](../../documentation/user/features/routing.md)
