# TimeWarp.Terminal

Terminal abstractions and widgets for console applications - IConsole, ITerminal, panels, tables, rules, and ANSI color support.

## Interfaces

### IConsole

Basic console I/O abstraction for testable console applications.

```csharp
public interface IConsole
{
    void Write(string message);
    void WriteLine(string? message = null);
    void WriteErrorLine(string? message = null);
    string? ReadLine();
}
```

### ITerminal

Extended terminal interface with cursor control, colors, and hyperlinks.

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

## Implementations

| Class | Description |
|-------|-------------|
| `NuruConsole` | Production `IConsole` wrapping `System.Console` |
| `TimeWarpTerminal` | Production `ITerminal` with full terminal capabilities |
| `TestConsole` | Test implementation with captured output |
| `TestTerminal` | Test implementation for interactive scenarios |

### Testing Example

```csharp
var terminal = new TestTerminal();
terminal.QueueInput("user input");

// Run code that uses ITerminal
myCommand.Execute(terminal);

// Verify output
Assert.Contains("expected text", terminal.GetOutput());
```

## Widgets

### Panel

Bordered panel with title and content.

```csharp
terminal.WritePanel(panel => panel
    .WithTitle("Status")
    .WithContent("All systems operational")
    .WithBorder(BorderStyle.Rounded));
```

### Table

Formatted table with columns and rows.

```csharp
terminal.WriteTable(table => table
    .AddColumn("Name")
    .AddColumn("Value")
    .AddRow("CPU", "45%")
    .AddRow("Memory", "2.1 GB"));
```

### Rule

Horizontal rule with optional title.

```csharp
terminal.WriteRule(rule => rule
    .WithTitle("Section")
    .WithStyle(LineStyle.Double));
```

## ANSI Colors

Extension methods for colored console output.

```csharp
terminal.WriteLine("Success!".Green());
terminal.WriteLine("Warning!".Yellow().Bold());
terminal.WriteLine("Error!".Red().OnWhite());
```

### Available Colors

`Black`, `Red`, `Green`, `Yellow`, `Blue`, `Magenta`, `Cyan`, `White`

Bright variants: `BrightRed`, `BrightGreen`, etc.

### Styles

`Bold()`, `Dim()`, `Italic()`, `Underline()`, `Strikethrough()`

### Background Colors

`OnRed()`, `OnGreen()`, `OnBlue()`, etc.

## Hyperlinks

Terminal hyperlinks (OSC 8) for supported terminals.

```csharp
terminal.WriteHyperlink("Click here", "https://example.com");

// Or using extension method
terminal.WriteLine("Visit ".WithHyperlink("our site", "https://example.com"));
```
