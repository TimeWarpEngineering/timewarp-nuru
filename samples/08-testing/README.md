# Testing

Demonstrates testing patterns for TimeWarp.Nuru applications using `TestTerminal`.

## Run It

```bash
dotnet run samples/08-testing/01-output-capture.cs
dotnet run samples/08-testing/02-colored-output.cs
dotnet run samples/08-testing/03-terminal-injection.cs
dotnet run samples/08-testing/04-debug-test.cs
```

## What's Demonstrated

- **01-output-capture**: Capturing stdout/stderr with `TestTerminal`
- **02-colored-output**: Testing styled/colored output
- **03-terminal-injection**: Injecting `ITerminal` for testability
- **04-debug-test**: Debug-mode testing patterns

## Related Documentation

- [Terminal Abstractions](../../documentation/user/features/terminal-abstractions.md)
