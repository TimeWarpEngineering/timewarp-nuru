# Document ITerminal/IConsole Abstractions

## Description

The `IConsole` and `ITerminal` interfaces along with their implementations (`NuruConsole`, `NuruTerminal`, `TestConsole`, `TestTerminal`) are valuable public APIs that enable testability and custom terminal environments, but they are completely undocumented for consumers. Users have no way to discover these capabilities exist.

Additionally, consumers should be able to use `ITerminal`/`NuruTerminal` directly in their route handlers for colored output, replacing the need for Spectre.Console in basic scenarios. The `SupportsColor` property exists but there are no color output methods.

## Requirements

- Document IConsole/ITerminal in user-facing documentation
- Create samples demonstrating testing capabilities
- Create samples demonstrating custom display/output scenarios
- Add color output methods to IConsole/ITerminal (Spectre.Console alternative for basic use)
- Show how to inject ITerminal into route handlers via DI
- Consider adding MCP tool support for discoverability

## Checklist

### Enhancement - Color Output
- [ ] Add color output methods to IConsole interface (e.g., `WriteColored`, `WriteLineColored`)
- [ ] Add ANSI escape code helpers for common colors (red, green, yellow, cyan, etc.)
- [ ] Implement in NuruConsole/NuruTerminal with `SupportsColor` check
- [ ] Implement in TestConsole/TestTerminal (strip or capture color codes)
- [ ] Consider extension methods for fluent API: `terminal.WriteLine("Success!".Green())`

### Documentation
- [ ] Add ITerminal/IConsole section to README.md (brief mention with link)
- [ ] Create `documentation/user/features/terminal-abstractions.md` with full documentation
- [ ] Document `UseTerminal()` builder method
- [ ] Document testing patterns with `TestTerminal`
- [ ] Document color output as lightweight Spectre.Console alternative

### Samples
- [ ] Create `Samples/Testing/` folder with testing examples
- [ ] Create sample showing unit testing CLI output capture
- [ ] Create sample showing REPL testing with scripted key sequences
- [ ] Create sample showing custom terminal implementation (e.g., logging wrapper)
- [ ] Create sample showing ITerminal injection in route handlers for colored output
- [ ] Create sample comparing Nuru color output vs Spectre.Console (show simplicity)

### MCP
- [ ] Consider adding MCP tool to explain ITerminal/IConsole usage

## Notes

### Interfaces to Document

**IConsole** - Basic I/O abstraction:
- `Write(string)`, `WriteLine(string?)`
- `WriteLineAsync(string?)`, `WriteErrorLine(string?)`
- `ReadLine()`

**ITerminal : IConsole** - Interactive terminal:
- `ReadKey(bool intercept)` - Key-by-key input
- `SetCursorPosition(int, int)`, `GetCursorPosition()`
- `WindowWidth`, `IsInteractive`, `SupportsColor`
- `Clear()`

### Implementations to Document

| Class | Interface | Purpose |
|-------|-----------|---------|
| NuruConsole | IConsole | Production basic I/O |
| NuruTerminal | ITerminal | Production interactive terminal |
| TestConsole | IConsole | Testing with StringWriter/StringReader |
| TestTerminal | ITerminal | Testing REPL with key queue support |

### Key Use Cases

1. **Colored Output in Handlers** - Use ITerminal for colored console output without Spectre.Console dependency
2. **Unit Testing CLI Output** - Capture and assert on command output without console hacks
3. **Testing REPL/Interactive Features** - Script key sequences for deterministic testing
4. **Custom Terminal Environments** - Web terminals, GUI integration, remote consoles
5. **Output Logging/Decoration** - Wrap terminal to add timestamps, colors, or logging

### Sample Code to Include

```csharp
// Handler with colored output (proposed API)
var app = NuruApp.CreateBuilder(args);

app.Services.AddSingleton<ITerminal>(NuruTerminal.Default);

app.Map("status", (ITerminal terminal) =>
{
    terminal.WriteLineColored("✓ All systems operational", ConsoleColor.Green);
    terminal.WriteLineColored("⚠ 2 warnings", ConsoleColor.Yellow);
    terminal.WriteLineColored("✗ 1 error", ConsoleColor.Red);
});

// Or with extension methods (proposed fluent API)
app.Map("deploy {env}", (string env, ITerminal terminal) =>
{
    terminal.WriteLine($"Deploying to {env}...".Cyan());
    // ... do work
    terminal.WriteLine("Deploy complete!".Green());
});

await app.Build().RunAsync(args);
```

```csharp
// Testing example - output capture works regardless of colors
using var terminal = new TestTerminal();
terminal.QueueKeys("hello");
terminal.QueueKey(ConsoleKey.Tab);  // Trigger completion
terminal.QueueKey(ConsoleKey.Enter);
terminal.QueueLine("exit");

var app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("hello", (ITerminal t) => t.WriteLineColored("Hello!", ConsoleColor.Green))
    .Build();

await app.RunReplAsync();

Assert.Contains("Hello!", terminal.Output);  // Color codes stripped or captured
```

### Proposed Color API

```csharp
// Interface additions
public interface IConsole
{
    // Existing methods...

    // New color methods
    void WriteColored(string message, ConsoleColor foreground);
    void WriteLineColored(string message, ConsoleColor foreground);
    void WriteColored(string message, ConsoleColor foreground, ConsoleColor background);
}

// Extension methods for fluent API
public static class ConsoleColorExtensions
{
    public static string Red(this string text) => ...;
    public static string Green(this string text) => ...;
    public static string Yellow(this string text) => ...;
    public static string Cyan(this string text) => ...;
    public static string Bold(this string text) => ...;
}
```

### Files to Reference

- `Source/TimeWarp.Nuru/IO/IConsole.cs`
- `Source/TimeWarp.Nuru/IO/ITerminal.cs`
- `Source/TimeWarp.Nuru/IO/NuruConsole.cs`
- `Source/TimeWarp.Nuru/IO/NuruTerminal.cs`
- `Source/TimeWarp.Nuru/IO/TestTerminal.cs`
