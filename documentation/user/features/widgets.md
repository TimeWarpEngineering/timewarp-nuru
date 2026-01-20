# Terminal Widgets

TimeWarp.Nuru includes a built-in widget system for creating styled terminal output. These widgets provide a lightweight alternative to Spectre.Console for common formatting needs.

## Overview

| Widget | Purpose | Extension Method |
|--------|---------|------------------|
| **Rule** | Horizontal divider lines with optional centered text | `terminal.WriteRule()` |
| **Panel** | Bordered boxes with headers and multi-line content | `terminal.WritePanel()` |
| **Table** | Columnar data with alignment and border styles | `terminal.WriteTable()` |

All widgets:
- Support the fluent builder pattern for configuration
- Work with `TestTerminal` for unit testing
- Handle ANSI color codes correctly in width calculations
- Are fully AOT-compatible

## Rule Widget

Creates horizontal divider lines with optional centered text. Useful for separating sections in CLI output.

### Basic Usage

```csharp
using TimeWarp.Nuru;

ITerminal terminal = TimeWarpTerminal.Default;

// Simple horizontal line
terminal.WriteRule();

// Rule with centered title
terminal.WriteRule("Section Title");

// Rule with styled title
terminal.WriteRule("Results".Cyan().Bold());
```

### Line Styles

```csharp
// Thin (default) - uses ─
terminal.WriteRule("Thin Style", LineStyle.Thin);

// Doubled - uses ═
terminal.WriteRule("Doubled Style", LineStyle.Doubled);

// Heavy - uses ━
terminal.WriteRule("Heavy Style", LineStyle.Heavy);
```

### Fluent Builder API

```csharp
terminal.WriteRule(rule => rule
    .Title("Configuration")
    .Style(LineStyle.Doubled)
    .Color(AnsiColors.Cyan));

terminal.WriteRule(rule => rule
    .Title("Success".Green())
    .Color(AnsiColors.Green));
```

### Pre-configured Rule Object

```csharp
Rule customRule = new()
{
    Title = "Custom Configuration",
    Style = LineStyle.Heavy,
    Color = AnsiColors.Magenta
};
terminal.WriteRule(customRule);
```

### Rule Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Title` | `string?` | `null` | Optional centered text |
| `Style` | `LineStyle` | `Thin` | Line character style |
| `Color` | `string?` | `null` | ANSI color code for the line |
| `Width` | `int?` | `null` | Fixed width (uses terminal width if null) |

### RuleBuilder Methods

| Method | Description |
|--------|-------------|
| `.Title(string)` | Sets the centered title text |
| `.Style(LineStyle)` | Sets the line style |
| `.Color(string)` | Sets the ANSI color code |
| `.Width(int)` | Sets a fixed width |
| `.Build()` | Returns the configured `Rule` |

## Panel Widget

Creates bordered boxes with optional headers. Useful for highlighting important information, status displays, or grouping related content.

### Basic Usage

```csharp
using TimeWarp.Nuru;

ITerminal terminal = TimeWarpTerminal.Default;

// Simple panel with content
terminal.WritePanel("This is important information");

// Panel with header
terminal.WritePanel("Content goes here", "Notice");
```

### Border Styles

```csharp
// Rounded (default) - uses ╭╮╰╯
terminal.WritePanel(panel => panel
    .Header("Rounded")
    .Content("Soft corners")
    .Border(BorderStyle.Rounded));

// Square - uses ┌┐└┘
terminal.WritePanel(panel => panel
    .Header("Square")
    .Content("Sharp corners")
    .Border(BorderStyle.Square));

// Doubled - uses ╔╗╚╝
terminal.WritePanel(panel => panel
    .Header("Doubled")
    .Content("Double lines")
    .Border(BorderStyle.Doubled));

// Heavy - uses ┏┓┗┛
terminal.WritePanel(panel => panel
    .Header("Heavy")
    .Content("Thick lines")
    .Border(BorderStyle.Heavy));

// None - no border
terminal.WritePanel(panel => panel
    .Content("Borderless content")
    .Border(BorderStyle.None));
```

### Multi-line Content

```csharp
terminal.WritePanel(panel => panel
    .Header("Team Members")
    .Content("Alice - Developer\nBob - Designer\nCharlie - Manager")
    .Border(BorderStyle.Rounded));
```

### Padding and Styling

```csharp
// Custom padding
terminal.WritePanel(panel => panel
    .Content("Spacious content")
    .Padding(3, 1));  // horizontal=3, vertical=1

// Colored border
terminal.WritePanel(panel => panel
    .Header("Success".Green())
    .Content("Operation completed successfully")
    .BorderColor(AnsiColors.Green));

// Fixed width
terminal.WritePanel(panel => panel
    .Header("Fixed")
    .Content("30 chars wide")
    .Width(30));

// Styled header and content
terminal.WritePanel(panel => panel
    .Header("Status".Cyan().Bold())
    .Content("All systems operational".Green())
    .Border(BorderStyle.Rounded)
    .BorderColor(AnsiColors.Cyan)
    .Padding(2, 1));
```

### Automatic Text Wrapping

Panels automatically wrap long text at word boundaries to fit within the panel width. This is ANSI-aware, so color codes are preserved across wrapped lines.

```csharp
// Long text automatically wraps at word boundaries
terminal.WritePanel(panel => panel
    .Header("Description")
    .Content("This is a long piece of text that will automatically wrap at word boundaries to fit within the panel. " +
             "The wrapping is ANSI-aware, so " + "colored text".Cyan() + " preserves formatting across line breaks.")
    .Width(50));

// Disable word wrapping if needed
terminal.WritePanel(panel => panel
    .Header("Raw Output")
    .Content("This text will not wrap and may extend beyond the panel border if too long")
    .WordWrap(false)
    .Width(40));
```

### Pre-configured Panel Object

```csharp
Panel customPanel = new()
{
    Header = "Configuration",
    Content = "Environment: Production\nDebug: false\nVersion: 1.0.0",
    Border = BorderStyle.Doubled,
    BorderColor = AnsiColors.Magenta,
    PaddingHorizontal = 2,
    PaddingVertical = 1
};
terminal.WritePanel(customPanel);
```

### Panel Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Header` | `string?` | `null` | Optional header in top border |
| `Content` | `string?` | `null` | Panel content (supports `\n` for multi-line) |
| `Border` | `BorderStyle` | `Rounded` | Border style |
| `BorderColor` | `string?` | `null` | ANSI color code for border |
| `PaddingHorizontal` | `int` | `1` | Left/right padding inside panel |
| `PaddingVertical` | `int` | `0` | Top/bottom padding inside panel |
| `Width` | `int?` | `null` | Fixed width (uses terminal width if null) |
| `WordWrap` | `bool` | `true` | Wrap long text at word boundaries (ANSI-aware) |

### PanelBuilder Methods

| Method | Description |
|--------|-------------|
| `.Header(string)` | Sets the header text |
| `.Content(string)` | Sets the content text |
| `.Border(BorderStyle)` | Sets the border style |
| `.BorderColor(string)` | Sets the border color |
| `.Padding(int, int)` | Sets horizontal and vertical padding |
| `.PaddingHorizontal(int)` | Sets horizontal padding only |
| `.PaddingVertical(int)` | Sets vertical padding only |
| `.Width(int)` | Sets a fixed width |
| `.WordWrap(bool)` | Enables/disables word wrapping (default: true) |
| `.Build()` | Returns the configured `Panel` |

## Table Widget

Creates formatted tables with columns, alignment, and borders. Useful for displaying structured data like lists, status reports, or configuration values.

### Basic Usage

```csharp
using TimeWarp.Nuru;

ITerminal terminal = TimeWarpTerminal.Default;

// Simple table
Table basicTable = new Table()
    .AddColumn("Name")
    .AddColumn("Value")
    .AddRow("Host", "localhost")
    .AddRow("Port", "8080")
    .AddRow("Protocol", "HTTP/2");

terminal.WriteTable(basicTable);
```

### Column Alignment

```csharp
Table alignedTable = new Table()
    .AddColumn("Package")
    .AddColumn("Downloads", Alignment.Right)
    .AddColumn("Version", Alignment.Center)
    .AddRow("Ardalis.GuardClauses", "12,543,210", "5.0.0")
    .AddRow("Ardalis.Result", "8,234,567", "10.0.0")
    .AddRow("TimeWarp.Nuru", "42,000", "3.0.0");

terminal.WriteTable(alignedTable);
```

### Styled Content

```csharp
Table styledTable = new Table()
    .AddColumn("Test")
    .AddColumn("Status")
    .AddRow("Unit Tests", "PASSED".Green())
    .AddRow("Integration Tests", "PASSED".Green())
    .AddRow("E2E Tests", "FAILED".Red());

terminal.WriteTable(styledTable);
```

### Border Styles

```csharp
// Square (default)
Table squareTable = new Table()
    .AddColumn("A")
    .AddColumn("B")
    .AddRow("1", "2");
squareTable.Border = BorderStyle.Square;

// Rounded
Table roundedTable = new Table()
    .AddColumn("A")
    .AddColumn("B")
    .AddRow("1", "2");
roundedTable.Border = BorderStyle.Rounded;

// Doubled
Table doubledTable = new Table()
    .AddColumn("A")
    .AddColumn("B")
    .AddRow("1", "2");
doubledTable.Border = BorderStyle.Doubled;

// Heavy
Table heavyTable = new Table()
    .AddColumn("A")
    .AddColumn("B")
    .AddRow("1", "2");
heavyTable.Border = BorderStyle.Heavy;

// None (borderless)
Table noBorderTable = new Table()
    .AddColumn("A")
    .AddColumn("B")
    .AddRow("1", "2");
noBorderTable.Border = BorderStyle.None;
```

### Table Options

```csharp
// Colored border
Table coloredTable = new Table()
    .AddColumn("Project")
    .AddColumn("Status")
    .AddRow("Backend", "Running")
    .AddRow("Frontend", "Building");
coloredTable.BorderColor = AnsiColors.Cyan;
coloredTable.Border = BorderStyle.Rounded;

// Headerless table
Table headerlessTable = new Table()
    .AddColumn("Key")
    .AddColumn("Value")
    .AddRow("API_KEY", "sk-abc123...")
    .AddRow("DB_HOST", "database.example.com");
headerlessTable.ShowHeaders = false;

// Row separators
Table separatorsTable = new Table()
    .AddColumn("Time")
    .AddColumn("Event")
    .AddRow("09:00", "Meeting started")
    .AddRow("10:30", "Coffee break")
    .AddRow("11:00", "Presentation");
separatorsTable.ShowRowSeparators = true;

// Expanded table (fills terminal width)
Table expandedTable = new Table()
    .AddColumn("Name")
    .AddColumn("Description")
    .AddRow("table", "Renders columnar data")
    .AddRow("panel", "Renders bordered boxes");
expandedTable.Expand = true;
```

### Fluent Builder Pattern

```csharp
terminal.WriteTable(t => t
    .AddColumns("Method", "Endpoint", "Status")
    .AddRow("GET", "/api/users", "200")
    .AddRow("POST", "/api/orders", "201")
    .AddRow("DELETE", "/api/items/42", "404")
    .Border(BorderStyle.Rounded));
```

### Table Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Columns` | `IReadOnlyList<TableColumn>` | `[]` | Column definitions |
| `Rows` | `IReadOnlyList<string[]>` | `[]` | Data rows |
| `Border` | `BorderStyle` | `Square` | Border style |
| `BorderColor` | `string?` | `null` | ANSI color code for border |
| `ShowHeaders` | `bool` | `true` | Whether to display header row |
| `ShowRowSeparators` | `bool` | `false` | Whether to show lines between rows |
| `Expand` | `bool` | `false` | Whether to fill terminal width |

### Table Methods

| Method | Description |
|--------|-------------|
| `.AddColumn(string)` | Adds a left-aligned column |
| `.AddColumn(string, Alignment)` | Adds a column with specified alignment |
| `.AddColumn(TableColumn)` | Adds a pre-configured column |
| `.AddColumns(params string[])` | Adds multiple columns |
| `.AddRow(params string[])` | Adds a data row |
| `.Render(int)` | Returns rendered lines |

### TableColumn Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Header` | `string` | `""` | Column header text |
| `Alignment` | `Alignment` | `Left` | Content alignment |
| `MaxWidth` | `int?` | `null` | Maximum column width (truncates if exceeded) |
| `HeaderColor` | `string?` | `null` | ANSI color for header |

### TableBuilder Methods

| Method | Description |
|--------|-------------|
| `.AddColumn(string)` | Adds a left-aligned column |
| `.AddColumn(string, Alignment)` | Adds a column with alignment |
| `.AddColumns(params string[])` | Adds multiple columns |
| `.AddRow(params string[])` | Adds a data row |
| `.Border(BorderStyle)` | Sets border style |
| `.BorderColor(string)` | Sets border color |
| `.HideHeaders()` | Hides the header row |
| `.ShowRowSeparators()` | Shows lines between rows |
| `.Expand()` | Expands to terminal width |
| `.Build()` | Returns the configured `Table` |

## Shared Enums

### LineStyle

Used by the Rule widget for horizontal line characters.

| Value | Character | Unicode |
|-------|-----------|---------|
| `Thin` | ─ | U+2500 |
| `Doubled` | ═ | U+2550 |
| `Heavy` | ━ | U+2501 |

### BorderStyle

Used by Panel and Table widgets for box borders.

| Value | Corners | Lines | Description |
|-------|---------|-------|-------------|
| `None` | - | - | No border |
| `Rounded` | ╭╮╰╯ | ─│ | Rounded corners with thin lines |
| `Square` | ┌┐└┘ | ─│ | Sharp corners with thin lines |
| `Doubled` | ╔╗╚╝ | ═║ | Double-line border |
| `Heavy` | ┏┓┗┛ | ━┃ | Thick/heavy lines |

### Alignment

Used by Table widget for column content alignment.

| Value | Description |
|-------|-------------|
| `Left` | Content aligned to left edge (default) |
| `Center` | Content centered in column |
| `Right` | Content aligned to right edge (common for numbers) |

## Testing with TestTerminal

All widgets work with `TestTerminal` for unit testing:

```csharp
using TestTerminal terminal = new();

NuruApp app = NuruApp.CreateBuilder(args)
    .UseTerminal(terminal)
    .Map("status", (ITerminal t) =>
    {
        t.WritePanel(panel => panel
            .Header("Status")
            .Content("All systems operational"));
        
        t.WriteTable(table => table
            .AddColumns("Service", "Status")
            .AddRow("API", "Running")
            .AddRow("Database", "Connected"));
    })
    .Build();

await app.RunAsync(["status"]);

// Assert on captured output
Assert.True(terminal.OutputContains("Status"));
Assert.True(terminal.OutputContains("All systems operational"));
Assert.True(terminal.OutputContains("API"));
```

## Practical Examples

### CLI Build Output

```csharp
terminal.WriteRule("Build Output");
terminal.WriteLine("  Compiling project...");
terminal.WriteLine("  Build succeeded.");
terminal.WriteLine();

terminal.WriteRule("Test Results", LineStyle.Doubled);
terminal.WriteLine("  ✓ 42 tests passed");
terminal.WriteLine("  ✗ 0 tests failed");
terminal.WriteLine();

terminal.WriteRule(rule => rule
    .Title("Summary".Bold())
    .Style(LineStyle.Heavy)
    .Color(AnsiColors.BrightGreen));
terminal.WriteLine("  Total time: 1.23s");
terminal.WriteLine("  Status: " + "SUCCESS".Green().Bold());
```

### Status Display

```csharp
terminal.WritePanel(panel => panel
    .Header("Build Status".Bold())
    .Content($"{"Project:".Gray()}  TimeWarp.Nuru\n" +
             $"{"Status:".Gray()}   {"✓ Success".Green()}\n" +
             $"{"Duration:".Gray()} 2.34s")
    .Border(BorderStyle.Rounded)
    .BorderColor(AnsiColors.BrightGreen)
    .Padding(2, 1));
```

### Configuration Display

```csharp
terminal.WriteTable(t => t
    .AddColumn("Setting")
    .AddColumn("Value", Alignment.Right)
    .AddRow("Environment", "Production")
    .AddRow("Debug Mode", "Disabled")
    .AddRow("Log Level", "Warning")
    .AddRow("Cache TTL", "3600s")
    .Border(BorderStyle.Rounded)
    .BorderColor(AnsiColors.Cyan));
```

## See Also

- [Terminal Abstractions](terminal-abstractions.md) - ITerminal interface and colored output
- [Testing Samples](../../../samples/testing/) - Complete testing examples
- [Rule Widget Demo](../../../samples/terminal/rule-widget.cs) - Rule widget examples
- [Panel Widget Demo](../../../samples/terminal/panel-widget.cs) - Panel widget examples
- [Table Widget Demo](../../../samples/terminal/table-widget.cs) - Table widget examples
