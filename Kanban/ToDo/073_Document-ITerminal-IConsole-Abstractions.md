# Document ITerminal/IConsole Abstractions

## Description

The `IConsole` and `ITerminal` interfaces along with their implementations (`NuruConsole`, `NuruTerminal`, `TestConsole`, `TestTerminal`) are valuable public APIs that enable testability and custom terminal environments, but they are completely undocumented for consumers. Users have no way to discover these capabilities exist.

## Requirements

- Document IConsole/ITerminal in user-facing documentation
- Create samples demonstrating testing capabilities
- Create samples demonstrating custom display/output scenarios
- Consider adding MCP tool support for discoverability

## Checklist

### Documentation
- [ ] Add ITerminal/IConsole section to README.md (brief mention with link)
- [ ] Create `documentation/user/features/terminal-abstractions.md` with full documentation
- [ ] Document `UseTerminal()` builder method
- [ ] Document testing patterns with `TestTerminal`

### Samples
- [ ] Create `Samples/Testing/` folder with testing examples
- [ ] Create sample showing unit testing CLI output capture
- [ ] Create sample showing REPL testing with scripted key sequences
- [ ] Create sample showing custom terminal implementation (e.g., logging wrapper)

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

1. **Unit Testing CLI Output** - Capture and assert on command output without console hacks
2. **Testing REPL/Interactive Features** - Script key sequences for deterministic testing
3. **Custom Terminal Environments** - Web terminals, GUI integration, remote consoles
4. **Output Logging/Decoration** - Wrap terminal to add timestamps, colors, or logging

### Sample Code to Include

```csharp
// Testing example
using var terminal = new TestTerminal();
terminal.QueueKeys("hello");
terminal.QueueKey(ConsoleKey.Tab);  // Trigger completion
terminal.QueueKey(ConsoleKey.Enter);
terminal.QueueLine("exit");

var app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("hello", () => Console.WriteLine("Hello!"))
    .Build();

await app.RunReplAsync();

Assert.Contains("Hello!", terminal.Output);
```

### Files to Reference

- `Source/TimeWarp.Nuru/IO/IConsole.cs`
- `Source/TimeWarp.Nuru/IO/ITerminal.cs`
- `Source/TimeWarp.Nuru/IO/NuruTerminal.cs`
- `Source/TimeWarp.Nuru/IO/TestTerminal.cs`
