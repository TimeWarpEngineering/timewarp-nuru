# Terminal Abstractions

TimeWarp.Nuru provides powerful terminal abstractions (`IConsole` and `ITerminal`) that enable:
- **Testable CLI output** without console hacks
- **Colored terminal output** as a lightweight Spectre.Console alternative
- **Custom terminal environments** for web terminals, GUI integration, or remote consoles

## Quick Start

### Colored Output with Extension Methods

```csharp
using TimeWarp.Nuru;

Console.WriteLine("Error: Something went wrong".Red().Bold());
Console.WriteLine("Success: Operation completed".Green());
Console.WriteLine("Warning: Check your input".Yellow());
Console.WriteLine("Info: Processing...".Cyan());

// Chaining styles
Console.WriteLine("Critical".Red().Bold().OnWhite());
Console.WriteLine("Highlight".BrightYellow().Underline());
```

### Colored Output in Route Handlers

```csharp
NuruAppBuilder builder = NuruApp.CreateBuilder(args);

builder.Map("status", (ITerminal terminal) =>
{
    terminal.WriteLine("All systems operational".Green());
    terminal.WriteLine("2 warnings".Yellow());
    terminal.WriteLine("1 error".Red());
});

builder.Map("deploy {env}", (string env, ITerminal terminal) =>
{
    terminal.WriteLine($"Deploying to {env}...".Cyan().Bold());
    // ... do work
    terminal.WriteLine("Deploy complete!".BrightGreen());
});

return await builder.Build().RunAsync(args);
```

### Testing CLI Output

```csharp
using TestTerminal terminal = new();

NuruApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("greet {name}", (string name, ITerminal t) =>
        t.WriteLine($"Hello, {name}!".Green()))
    .Build();

await app.RunAsync(["greet", "World"]);

// Assert on captured output
Assert.Contains("Hello, World!", terminal.Output);
```

## Interfaces

### IConsole

Basic I/O abstraction for simple console operations:

```csharp
public interface IConsole
{
    void Write(string message);
    void WriteLine(string? message = null);
    Task WriteLineAsync(string? message = null);
    void WriteErrorLine(string? message = null);
    Task WriteErrorLineAsync(string? message = null);
    string? ReadLine();
}
```

**Use `IConsole` when you only need:**
- Writing to stdout/stderr
- Reading complete lines of input

### ITerminal

Interactive terminal abstraction extending `IConsole`:

```csharp
public interface ITerminal : IConsole
{
    ConsoleKeyInfo ReadKey(bool intercept);
    void SetCursorPosition(int left, int top);
    (int Left, int Top) GetCursorPosition();
    int WindowWidth { get; }
    bool IsInteractive { get; }
    bool SupportsColor { get; }
    void Clear();
}
```

**Use `ITerminal` when you need:**
- Key-by-key input (arrow keys, Tab, etc.)
- Cursor positioning for line editing
- Terminal capability detection
- Screen clearing

## Implementations

| Class | Interface | Purpose |
|-------|-----------|---------|
| `NuruConsole` | `IConsole` | Production basic I/O (singleton: `NuruConsole.Default`) |
| `NuruTerminal` | `ITerminal` | Production interactive terminal (singleton: `NuruTerminal.Default`) |
| `TestTerminal` | `ITerminal` | Testing with captured output and scripted key input |

## Color Output

### Extension Methods (Recommended)

TimeWarp.Nuru includes fluent extension methods for colored output:

```csharp
// Foreground colors
"text".Red()
"text".Green()
"text".Yellow()
"text".Blue()
"text".Magenta()
"text".Cyan()
"text".White()
"text".Gray()

// Bright colors
"text".BrightRed()
"text".BrightGreen()
"text".BrightYellow()
// ... etc

// Background colors
"text".OnRed()
"text".OnGreen()
"text".OnBlue()
// ... etc

// Formatting
"text".Bold()
"text".Dim()
"text".Italic()
"text".Underline()
"text".Strikethrough()
"text".Reverse()

// CSS Named Colors
"text".Orange()
"text".Pink()
"text".Purple()
"text".Gold()
"text".Coral()
"text".Crimson()
"text".Teal()
"text".Navy()
// ... and more

// Chaining
"Error".Red().Bold()
"Alert".Yellow().OnBlack()
"Success".Green().Bold().Underline()
```

### Direct AnsiColors (Advanced)

For more control, use `AnsiColors` constants directly:

```csharp
using TimeWarp.Nuru;

// Basic usage
terminal.WriteLine(AnsiColors.Green + "Success!" + AnsiColors.Reset);

// Combined styles
terminal.WriteLine(AnsiColors.Bold + AnsiColors.Red + "Error!" + AnsiColors.Reset);

// All CSS named colors available
terminal.WriteLine(AnsiColors.Coral + "Coral text" + AnsiColors.Reset);
terminal.WriteLine(AnsiColors.DodgerBlue + "Dodger blue" + AnsiColors.Reset);
```

**Available in `AnsiColors`:**
- **Basic colors**: Black, Red, Green, Yellow, Blue, Magenta, Cyan, White, Gray
- **Bright colors**: BrightRed, BrightGreen, BrightYellow, etc.
- **140+ CSS named colors**: Coral, Crimson, DodgerBlue, Gold, etc.
- **Background colors**: BgRed, BgGreen, BgBlue, etc.
- **Formatting**: Bold, Dim, Italic, Underline, Strikethrough, Reverse

## Testing with TestTerminal

### Basic Output Capture

```csharp
using TestTerminal terminal = new();

NuruApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("hello", (ITerminal t) => t.WriteLine("Hello, World!"))
    .Build();

await app.RunAsync(["hello"]);

// Assertions
Assert.True(terminal.OutputContains("Hello, World!"));
Assert.Equal(1, terminal.GetOutputLines().Length);
```

### Testing Error Output

```csharp
using TestTerminal terminal = new();

NuruApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("fail", (ITerminal t) => t.WriteErrorLine("Something went wrong"))
    .Build();

await app.RunAsync(["fail"]);

Assert.True(terminal.ErrorContains("Something went wrong"));
Assert.Contains("Something went wrong", terminal.ErrorOutput);
```

### Testing Interactive Input (REPL)

```csharp
using TestTerminal terminal = new();

// Queue scripted key input
terminal.QueueKeys("hello");      // Type "hello"
terminal.QueueKey(ConsoleKey.Tab); // Press Tab for completion
terminal.QueueKey(ConsoleKey.Enter); // Press Enter
terminal.QueueLine("exit");       // Type "exit" and Enter

NuruApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("hello-world", () => Console.WriteLine("Hello!"))
    .Build();

await app.RunReplAsync();

Assert.True(terminal.OutputContains("Hello!"));
```

### TestTerminal Helper Methods

```csharp
// Queue input
terminal.QueueKey(ConsoleKey.Enter);
terminal.QueueKey(ConsoleKey.Tab, shift: true);
terminal.QueueKey(ConsoleKey.D, ctrl: true); // Ctrl+D
terminal.QueueKeys("some text");
terminal.QueueLine("complete line with enter");
terminal.QueueArrow(ConsoleKey.UpArrow);

// Check output
terminal.OutputContains("text");
terminal.ErrorContains("error");
string[] lines = terminal.GetOutputLines();

// Access raw output
string stdout = terminal.Output;
string stderr = terminal.ErrorOutput;
string all = terminal.AllOutput;

// Reset for next test
terminal.ClearOutput();
terminal.ClearKeys();
```

## Configuration

### Using UseTerminal()

```csharp
// Production (default)
NuruApp app = new NuruAppBuilder()
    .Map("hello", () => Console.WriteLine("Hello!"))
    .Build();
// Uses NuruTerminal.Default automatically

// Testing
using TestTerminal terminal = new();
NuruApp testApp = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("hello", () => Console.WriteLine("Hello!"))
    .Build();

// Custom terminal
NuruApp customApp = new NuruAppBuilder()
    .UseTerminal(new MyCustomTerminal())
    .Build();
```

### Dependency Injection

ITerminal is automatically registered when using `NuruApp.CreateBuilder()`:

```csharp
NuruAppBuilder builder = NuruApp.CreateBuilder(args);

// ITerminal is injectable
builder.Map("status", (ITerminal terminal) =>
{
    if (terminal.SupportsColor)
    {
        terminal.WriteLine("Status: OK".Green());
    }
    else
    {
        terminal.WriteLine("Status: OK");
    }
});
```

## Custom Terminal Implementations

### Logging Wrapper

```csharp
public class LoggingTerminal : ITerminal
{
    private readonly ITerminal _inner;
    private readonly ILogger _logger;

    public LoggingTerminal(ITerminal inner, ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public void WriteLine(string? message = null)
    {
        _logger.LogInformation("Console output: {Message}", message);
        _inner.WriteLine(message);
    }

    // Delegate other members to _inner...
}
```

### Web Terminal Adapter

```csharp
public class WebTerminal : ITerminal
{
    private readonly WebSocket _webSocket;
    private readonly Queue<ConsoleKeyInfo> _inputQueue = new();

    public WebTerminal(WebSocket webSocket)
    {
        _webSocket = webSocket;
    }

    public void WriteLine(string? message = null)
    {
        // Send to WebSocket client
        _webSocket.SendAsync(
            Encoding.UTF8.GetBytes(message ?? ""),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }

    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        // Return queued keys from WebSocket messages
        if (_inputQueue.TryDequeue(out ConsoleKeyInfo key))
            return key;

        // Wait for input from client...
    }

    // Implement other members...
}
```

## Comparison with Spectre.Console

| Feature | TimeWarp.Nuru | Spectre.Console |
|---------|---------------|-----------------|
| Basic colors | `"text".Red()` | `AnsiConsole.MarkupLine("[red]text[/]")` |
| Bold | `"text".Bold()` | `AnsiConsole.MarkupLine("[bold]text[/]")` |
| Chaining | `"text".Red().Bold()` | `AnsiConsole.MarkupLine("[red bold]text[/]")` |
| Testability | Built-in `TestTerminal` | Requires mocking |
| Dependencies | None | ~500KB package |
| AOT Support | Full | Limited |
| Tables/Progress | Not included | Full support |

**Choose TimeWarp.Nuru colors when:**
- You need simple colored output
- Testing is important
- Minimal dependencies are required
- AOT compilation is needed

**Choose Spectre.Console when:**
- You need tables, progress bars, prompts
- Rich terminal UI is required

## Best Practices

### 1. Always Use ITerminal for Handler Output

```csharp
// Good - testable
builder.Map("status", (ITerminal terminal) =>
    terminal.WriteLine("OK".Green()));

// Avoid - not testable
builder.Map("status", () =>
    Console.WriteLine("OK".Green()));
```

### 2. Check Color Support

```csharp
builder.Map("status", (ITerminal terminal) =>
{
    string message = terminal.SupportsColor
        ? "Status: OK".Green()
        : "Status: OK";
    terminal.WriteLine(message);
});
```

### 3. Use Error Output for Errors

```csharp
builder.Map("validate", (ITerminal terminal) =>
{
    if (hasErrors)
    {
        terminal.WriteErrorLine("Validation failed".Red());
        return 1;
    }
    terminal.WriteLine("Validation passed".Green());
    return 0;
});
```

### 4. Dispose TestTerminal

```csharp
// Use 'using' to ensure cleanup
using TestTerminal terminal = new();
// or
using (TestTerminal terminal = new())
{
    // test code
}
```

## See Also

- [Output Handling](output-handling.md) - stdout/stderr best practices
- [Routing Patterns](routing.md) - Route syntax reference
- [Testing Samples](../../../samples/testing/) - Complete testing examples
