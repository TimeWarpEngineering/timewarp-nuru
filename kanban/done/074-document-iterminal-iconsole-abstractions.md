# Document ITerminal/IConsole Abstractions

## Dependency

**Complete task 075 first** - Move AnsiColors to core package and add extension methods. Documentation should reflect the final API location and extension methods.

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
- [x] Consider moving `AnsiColors` from `TimeWarp.Nuru.Repl` to core `TimeWarp.Nuru` package (Done in task 075)
- [x] Or: Document that consumers need REPL package for colors (suboptimal) - N/A, moved to core
- [x] Add convenience extension methods on IConsole for colored output (Done in task 075)
- [x] Ensure TestTerminal properly handles ANSI codes in output (strip or preserve for assertions) - preserves codes, text searchable

### Documentation
- [x] Add ITerminal/IConsole section to README.md (brief mention with link)
- [x] Create `documentation/user/features/terminal-abstractions.md` with full documentation
- [x] Document `UseTerminal()` builder method
- [x] Document testing patterns with `TestTerminal`
- [x] Document color output as lightweight Spectre.Console alternative

### Samples
- [x] Create `samples/testing/` folder with testing examples
- [x] Create sample showing unit testing CLI output capture (test-output-capture.cs)
- [ ] Create sample showing REPL testing with scripted key sequences (deferred - requires interactive REPL)
- [x] Create sample showing custom terminal implementation (e.g., logging wrapper) - documented in terminal-abstractions.md
- [x] Create sample showing ITerminal injection in route handlers for colored output (test-terminal-injection.cs)
- [x] Create sample comparing Nuru color output vs Spectre.Console (show simplicity) - comparison table in docs

### MCP
- [x] Consider adding MCP tool to explain ITerminal/IConsole usage - Already covered via `examples.json` with `test-output-capture`, `test-colored-output`, and `test-terminal-injection` samples discoverable through `GetExample` MCP tool

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

### Color Infrastructure (Now in Core Package!)

**AnsiColors** (`Source/TimeWarp.Nuru/IO/AnsiColors.cs`):
- Basic colors: Black, Red, Green, Yellow, Blue, Magenta, Cyan, White, Gray
- Bright colors: BrightRed, BrightGreen, BrightYellow, etc.
- All CSS named colors: Coral, Crimson, DodgerBlue, Gold, etc. (140+ colors)
- Background colors: BgRed, BgGreen, BgBlue, etc.
- Text formatting: Bold, Dim, Italic, Underline, Strikethrough, Reverse

**AnsiColorExtensions** (`Source/TimeWarp.Nuru/IO/AnsiColorExtensions.cs`):
- Fluent extension methods for clean API: `"text".Red()`, `"text".Bold()`
- Uses C# 14 extension block syntax

**SyntaxColors** (`Source/TimeWarp.Nuru.Repl/Display/SyntaxColors.cs`):
- CommandColor, ErrorColor, KeywordColor, StringColor, etc.
- PSReadLine-inspired syntax highlighting theme
- Remains in REPL package (REPL-specific)

**Usage Pattern** (fluent extensions):
```csharp
terminal.WriteLine("Success!".Green());
terminal.WriteLine("Error: ".Red() + message);
terminal.WriteLine("Header".Bold().Cyan());
```

### Files to Reference

- `Source/TimeWarp.Nuru/IO/IConsole.cs`
- `Source/TimeWarp.Nuru/IO/ITerminal.cs`
- `Source/TimeWarp.Nuru/IO/NuruConsole.cs`
- `Source/TimeWarp.Nuru/IO/NuruTerminal.cs`
- `Source/TimeWarp.Nuru/IO/TestTerminal.cs`
- `Source/TimeWarp.Nuru/IO/AnsiColors.cs` ← **Color constants**
- `Source/TimeWarp.Nuru/IO/AnsiColorExtensions.cs` ← **Fluent extension methods**
- `Source/TimeWarp.Nuru.Repl/Display/SyntaxColors.cs` ← **REPL syntax theme**
