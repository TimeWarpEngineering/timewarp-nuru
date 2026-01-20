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

NuruCoreApp app = NuruApp.CreateBuilder(args)
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
    bool SupportsHyperlinks { get; }
    void Clear();
}
```

**Use `ITerminal` when you need:**
- Key-by-key input (arrow keys, Tab, etc.)
- Cursor positioning for line editing
- Terminal capability detection (color, hyperlinks)
- Screen clearing

## Implementations

| Class | Interface | Purpose |
|-------|-----------|---------|
| `NuruConsole` | `IConsole` | Production basic I/O (singleton: `NuruConsole.Default`) |
| `TimeWarpTerminal` | `ITerminal` | Production interactive terminal (singleton: `TimeWarpTerminal.Default`) |
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

## Hyperlinks (OSC 8)

TimeWarp.Nuru supports OSC 8 hyperlinks for clickable URLs in supported terminals.

### Supported Terminals

- Windows Terminal
- iTerm2
- VS Code integrated terminal
- Hyper
- Konsole
- GNOME Terminal 3.26+

### String Extension Method

Use the `Link()` extension method for inline hyperlinks:

```csharp
// Simple hyperlink
Console.WriteLine($"Visit {"Ardalis.com".Link("https://ardalis.com")}");

// Chain with colors (hyperlink + styling)
Console.WriteLine($"Check out {"GitHub".Link("https://github.com").Cyan().Bold()}");

// Multiple links in one line
Console.WriteLine($"{"Home".Link("https://example.com")} | {"Docs".Link("https://docs.example.com")}");
```

### Terminal Extension Methods

Use `WriteLink()` and `WriteLinkLine()` for terminal-aware hyperlinks:

```csharp
NuruAppBuilder builder = NuruApp.CreateBuilder(args);

builder.Map("help", (ITerminal terminal) =>
{
    terminal.WriteLine("For more information:");
    terminal.WriteLink("https://docs.example.com", "Documentation");
    terminal.WriteLine();
    terminal.WriteLinkLine("https://github.com/example", "Source Code");
});
```

These methods automatically check `SupportsHyperlinks` and gracefully degrade:
- **Supported terminals**: Output includes OSC 8 escape sequences (clickable)
- **Unsupported terminals**: Output shows only the display text (plain)

### Detecting Hyperlink Support

```csharp
builder.Map("info", (ITerminal terminal) =>
{
    if (terminal.SupportsHyperlinks)
    {
        terminal.WriteLinkLine("https://docs.example.com", "Click here for docs");
    }
    else
    {
        terminal.WriteLine("Visit: https://docs.example.com");
    }
});
```

### API Summary

| Method | Description |
|--------|-------------|
| `"text".Link("url")` | String extension - always generates OSC 8 sequences |
| `terminal.WriteLink("url", "text")` | Writes hyperlink (or plain text if unsupported) |
| `terminal.WriteLinkLine("url", "text")` | Same as WriteLink with newline |
| `terminal.SupportsHyperlinks` | Check if terminal supports OSC 8 |

### OSC 8 Format Reference

The OSC 8 hyperlink format is:
```
\e]8;;URL\e\DISPLAY_TEXT\e]8;;\e\
```

Where:
- `\e]8;;` starts the hyperlink with URL following
- `\e\` is the string terminator (ST)
- Display text appears between the start and end sequences

## Testing with TestTerminal

### Basic Output Capture

```csharp
using TestTerminal terminal = new();

NuruCoreApp app = NuruApp.CreateBuilder(args)
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

NuruCoreApp app = NuruApp.CreateBuilder(args)
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

NuruCoreApp app = NuruApp.CreateBuilder(args)
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
NuruCoreApp app = NuruApp.CreateBuilder(args)
    .Map("hello", () => Console.WriteLine("Hello!"))
    .Build();
// Uses TimeWarpTerminal.Default automatically

// Testing
using TestTerminal terminal = new();
NuruCoreApp testApp = NuruApp.CreateBuilder(args)
    .UseTerminal(terminal)
    .Map("hello", () => Console.WriteLine("Hello!"))
    .Build();

// Custom terminal
NuruCoreApp customApp = NuruApp.CreateBuilder(args)
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
| Tables/Widgets | Rule, Panel, Table widgets | Full support (tables, progress, prompts) |

**Choose TimeWarp.Nuru when:**
- You need colored output with testability
- You need tables, panels, or rules
- Minimal dependencies are required
- AOT compilation is needed

**Choose Spectre.Console when:**
- You need progress bars, prompts, or live displays
- Advanced terminal UI features are required

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

## Zero-Config Test Isolation

TimeWarp.Nuru provides ambient context classes that enable zero-configuration testing of CLI applications. These use `AsyncLocal<T>` to provide test isolation even when running tests in parallel.

### TestTerminalContext

`TestTerminalContext` provides an ambient `TestTerminal` that Nuru's terminal resolution automatically uses:

```csharp
using TestTerminal terminal = new();
TestTerminalContext.Current = terminal;

// Any code that resolves ITerminal will now use this TestTerminal
await Program.Main(["greet", "World"]);

terminal.OutputContains("Hello, World!").ShouldBeTrue();
```

**Resolution Order:**

When Nuru resolves a terminal, it checks in this order:
1. `TestTerminalContext.Current` (if set)
2. `ITerminal` from DI (if registered)
3. `TimeWarpTerminal.Default` (fallback)

This means you can set `TestTerminalContext.Current` at the start of your test, and all terminal output will be captured without any code changes to the app being tested.

**Parallel Test Isolation:**

Because `TestTerminalContext` uses `AsyncLocal<T>`, each test gets its own isolated context:

```csharp
// Test 1 runs in parallel with Test 2
public static async Task Test1()
{
    using TestTerminal terminal = new();
    TestTerminalContext.Current = terminal;  // Only affects this async context
    await app.RunAsync(["command1"]);
    terminal.OutputContains("result1").ShouldBeTrue();
}

public static async Task Test2()
{
    using TestTerminal terminal = new();
    TestTerminalContext.Current = terminal;  // Separate from Test 1
    await app.RunAsync(["command2"]);
    terminal.OutputContains("result2").ShouldBeTrue();
}
```

### NuruTestContext for Runfile Testing

`NuruTestContext` enables testing of **runfiles** without modifying the application code. It allows a test harness to intercept `NuruCoreApp.RunAsync()` execution.

**How It Works:**

1. Create a test harness file with a `[ModuleInitializer]` that sets `NuruTestContext.TestRunner`
2. Use `Directory.Build.props` to conditionally include the test file when `NURU_TEST` is set
3. When the runfile executes, the test harness takes control instead of normal execution

```csharp
// test-my-app.cs - included via Directory.Build.props when NURU_TEST is set
public static class TestHarness
{
    internal static NuruCoreApp? App;

    [ModuleInitializer]
    public static void Initialize()
    {
        NuruTestContext.TestRunner = async (app) =>
        {
            App = app;  // Capture the configured app

            // Run multiple test scenarios
            using (TestTerminal terminal = new())
            {
                TestTerminalContext.Current = terminal;
                await app.RunAsync(["greet", "Alice"]);
                terminal.OutputContains("Hello, Alice!").ShouldBeTrue();
            }

            using (TestTerminal terminal = new())
            {
                TestTerminalContext.Current = terminal;
                await app.RunAsync(["greet", "Bob"]);
                terminal.OutputContains("Hello, Bob!").ShouldBeTrue();
            }

            Console.WriteLine("All tests passed!");
            return 0;
        };
    }
}
```

**Key Behaviors:**

- The `TestRunner` delegate is only invoked once per execution
- Subsequent calls to `RunAsync` from within the test harness execute normally
- This allows running multiple test scenarios against the same app instance

### Directory.Build.props Setup

To conditionally include test files for runfiles:

```xml
<Project>
  <ItemGroup Condition="'$(NURU_TEST)' != ''">
    <Compile Include="$(NURU_TEST)" />
    <PackageReference Include="TimeWarp.Jaribu" Version="*" />
  </ItemGroup>
</Project>
```

### Running Tests

```bash
# Set the environment variable
export NURU_TEST=test-my-app.cs  # bash
$env:NURU_TEST = "test-my-app.cs"  # PowerShell

# Clean to force rebuild with test harness (important!)
dotnet clean ./my-app.cs

# Run - tests execute instead of normal app
./my-app.cs

# Clean up: remove env var and rebuild for production
unset NURU_TEST  # bash
Remove-Item Env:NURU_TEST  # PowerShell
dotnet clean ./my-app.cs
```

**Important:** Always clean when changing `NURU_TEST` - the runfile cache doesn't track environment variables.

### When to Use Each Pattern

| Pattern | Use Case |
|---------|----------|
| `TestTerminalContext` | Testing apps where you control the test entry point (unit tests, integration tests) |
| `NuruTestContext` | Testing runfiles without modifying application code |
| Both together | Runfile testing with output capture |

### Source Files

- `source/timewarp-nuru-core/io/test-terminal-context.cs` - Ambient terminal context
- `source/timewarp-nuru-core/io/nuru-test-context.cs` - Runfile test harness support
- `samples/testing/runfile-test-harness/` - Complete example with Jaribu integration

## See Also

- [Terminal Widgets](widgets.md) - Rule, Panel, and Table widgets
- [Output Handling](output-handling.md) - stdout/stderr best practices
- [Routing Patterns](routing.md) - Route syntax reference
- [Testing Samples](../../../samples/testing/) - Complete testing examples
- [Runfile Test Harness Sample](../../../samples/testing/runfile-test-harness/overview.md) - Zero-modification testing pattern
