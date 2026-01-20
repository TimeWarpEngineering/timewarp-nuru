# Async Examples

Demonstrates async/await patterns in TimeWarp.Nuru handlers.

## Run It

```bash
dotnet run samples/06-async-examples/async-examples.cs -- ping
dotnet run samples/06-async-examples/async-examples.cs -- fetch https://example.com
dotnet run samples/06-async-examples/async-examples.cs -- process 5
dotnet run samples/06-async-examples/async-examples.cs -- long-task 3
```

## What's Demonstrated

- Simple async handlers returning `Task`
- Async with required and optional parameters
- Async returning `Task<int>` for exit codes
- Error handling in async handlers
- Cancellation support (Ctrl+C handling)
- Multiple optional parameters with options

## Related Documentation

- [Routing](../../documentation/user/features/routing.md)
