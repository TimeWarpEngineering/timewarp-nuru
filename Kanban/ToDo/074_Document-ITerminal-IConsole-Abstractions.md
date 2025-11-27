# Document ITerminal/IConsole Abstractions

## Description

The `IConsole` and `ITerminal` interfaces along with their implementations (`NuruConsole`, `NuruTerminal`, `TestConsole`, `TestTerminal`) are valuable public APIs that enable testability and custom terminal environments, but they are completely undocumented for consumers. Users have no way to discover these capabilities exist.

**We already have `AnsiColors` and `SyntaxColors` classes** with comprehensive color support (all CSS named colors, backgrounds, bold/italic/underline) - but they're buried in `TimeWarp.Nuru.Repl` and completely undocumented for consumer use. Consumers who don't use REPL miss out entirely.

## Requirements

- Document IConsole/ITerminal in user-facing documentation
- Document existing AnsiColors/SyntaxColors for consumer use
- Move AnsiColors to core TimeWarp.Nuru package (or make it more discoverable)
- Create samples demonstrating testing capabilities
- Create samples demonstrating colored output in handlers
- Show how to inject ITerminal into route handlers via DI
- Consider adding MCP tool support for discoverability

## Checklist

### Enhancement - Make Colors Accessible
- [ ] Consider moving `AnsiColors` from `TimeWarp.Nuru.Repl` to core `TimeWarp.Nuru` package
- [ ] Or: Document that consumers need REPL package for colors (suboptimal)
- [ ] Add convenience extension methods on IConsole for colored output
- [ ] Ensure TestTerminal properly handles ANSI codes in output (strip or preserve for assertions)

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
// Handler with colored output using existing AnsiColors
var app = NuruApp.CreateBuilder(args);

app.Services.AddSingleton<ITerminal>(NuruTerminal.Default);

app.Map("status", (ITerminal terminal) =>
{
    terminal.WriteLine(AnsiColors.Green + "✓ All systems operational" + AnsiColors.Reset);
    terminal.WriteLine(AnsiColors.Yellow + "⚠ 2 warnings" + AnsiColors.Reset);
    terminal.WriteLine(AnsiColors.Red + "✗ 1 error" + AnsiColors.Reset);
});

// With bold, colors, and backgrounds
app.Map("deploy {env}", (string env, ITerminal terminal) =>
{
    terminal.WriteLine(AnsiColors.Bold + AnsiColors.Cyan + $"Deploying to {env}..." + AnsiColors.Reset);
    // ... do work
    terminal.WriteLine(AnsiColors.BrightGreen + "✓ Deploy complete!" + AnsiColors.Reset);
});

// Using CSS named colors
app.Map("fancy", (ITerminal terminal) =>
{
    terminal.WriteLine(AnsiColors.Coral + "Coral text" + AnsiColors.Reset);
    terminal.WriteLine(AnsiColors.DodgerBlue + "Dodger blue" + AnsiColors.Reset);
    terminal.WriteLine(AnsiColors.Gold + AnsiColors.Bold + "Golden bold" + AnsiColors.Reset);
});

await app.Build().RunAsync(args);
```

```csharp
// Testing example - output capture includes ANSI codes
using var terminal = new TestTerminal();
terminal.QueueKeys("hello");
terminal.QueueKey(ConsoleKey.Tab);
terminal.QueueKey(ConsoleKey.Enter);
terminal.QueueLine("exit");

var app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("hello", (ITerminal t) =>
        t.WriteLine(AnsiColors.Green + "Hello!" + AnsiColors.Reset))
    .Build();

await app.RunReplAsync();

Assert.Contains("Hello!", terminal.Output);  // ANSI codes present but text matches
```

### Existing Color Infrastructure (ALREADY EXISTS - just undocumented!)

**AnsiColors** (`Source/TimeWarp.Nuru.Repl/Display/AnsiColors.cs`):
- Basic colors: Black, Red, Green, Yellow, Blue, Magenta, Cyan, White, Gray
- Bright colors: BrightRed, BrightGreen, BrightYellow, etc.
- All CSS named colors: Coral, Crimson, DodgerBlue, Gold, etc. (140+ colors)
- Background colors: BgRed, BgGreen, BgBlue, etc.
- Text formatting: Bold, Dim, Italic, Underline, Strikethrough, Reverse

**SyntaxColors** (`Source/TimeWarp.Nuru.Repl/Display/SyntaxColors.cs`):
- CommandColor, ErrorColor, KeywordColor, StringColor, etc.
- PSReadLine-inspired syntax highlighting theme

**Current Usage Pattern** (manual string concatenation):
```csharp
Terminal.WriteLine(AnsiColors.Green + "Success!" + AnsiColors.Reset);
Terminal.WriteLine(AnsiColors.Red + "Error: " + AnsiColors.Reset + message);
Terminal.WriteLine(AnsiColors.Bold + AnsiColors.Cyan + "Header" + AnsiColors.Reset);
```

### Proposed Improvements

```csharp
// Extension methods for cleaner API (optional enhancement)
public static class AnsiColorExtensions
{
    public static string Red(this string text) => AnsiColors.Red + text + AnsiColors.Reset;
    public static string Green(this string text) => AnsiColors.Green + text + AnsiColors.Reset;
    public static string Bold(this string text) => AnsiColors.Bold + text + AnsiColors.Reset;
    // etc.
}

// Then usage becomes:
terminal.WriteLine("Success!".Green());
terminal.WriteLine("Error: ".Red() + message);
```

### Files to Reference

- `Source/TimeWarp.Nuru/IO/IConsole.cs`
- `Source/TimeWarp.Nuru/IO/ITerminal.cs`
- `Source/TimeWarp.Nuru/IO/NuruConsole.cs`
- `Source/TimeWarp.Nuru/IO/NuruTerminal.cs`
- `Source/TimeWarp.Nuru/IO/TestTerminal.cs`
- `Source/TimeWarp.Nuru.Repl/Display/AnsiColors.cs` ← **Existing color support!**
- `Source/TimeWarp.Nuru.Repl/Display/SyntaxColors.cs` ← **Existing syntax theme!**
