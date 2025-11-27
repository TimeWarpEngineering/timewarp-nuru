# Testing Samples

This folder contains examples demonstrating how to test Nuru CLI applications using the `TestTerminal` abstraction.

## Samples

| Sample | Description |
|--------|-------------|
| [test-output-capture.cs](test-output-capture.cs) | Capture and assert on CLI output |
| [test-colored-output.cs](test-colored-output.cs) | Test handlers with colored output |
| [test-terminal-injection.cs](test-terminal-injection.cs) | ITerminal injection in route handlers |

## Key Concepts

### TestTerminal

`TestTerminal` is a testable implementation of `ITerminal` that:
- Captures all stdout and stderr output
- Provides scripted key input for REPL testing
- Supports key sequence queuing for interactive testing

### UseTerminal()

The `UseTerminal()` builder method configures which terminal implementation to use:

```csharp
// Production (default)
var app = new NuruAppBuilder()
    .Map("hello", () => Console.WriteLine("Hello!"))
    .Build();

// Testing
using var terminal = new TestTerminal();
var testApp = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("hello", () => Console.WriteLine("Hello!"))
    .Build();
```

## Running the Samples

```bash
# Run any sample directly
./Samples/Testing/test-output-capture.cs
./Samples/Testing/test-colored-output.cs
./Samples/Testing/test-terminal-injection.cs
```

## See Also

- [Terminal Abstractions Documentation](../../documentation/user/features/terminal-abstractions.md)
- [Output Handling](../../documentation/user/features/output-handling.md)
