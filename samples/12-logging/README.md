# Logging

Demonstrates logging configuration with Microsoft.Extensions.Logging and Serilog.

## Run It

```bash
dotnet run samples/12-logging/console-logging.cs -- test
dotnet run samples/12-logging/console-logging.cs -- greet Alice
dotnet run samples/12-logging/serilog-logging.cs -- test
```

## What's Demonstrated

- **console-logging.cs**: Microsoft.Extensions.Logging.Console integration
- **serilog-logging.cs**: Serilog structured logging integration
- `ILogger<T>` injection into behaviors
- Automatic `LoggerFactory` disposal

## Related Documentation

- [Logging](../../documentation/user/features/logging.md)
