#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Demonstrates the Table widget for rendering columnar data
using TimeWarp.Nuru;
using TimeWarp.Terminal;

// Create a terminal for colored output
ITerminal terminal = new TimeWarpTerminal();

terminal.WriteLine("Table Widget Demo");
terminal.WriteLine("==================\n");

// Example 1: Basic table with two columns
terminal.WriteLine("1. Basic Table");
terminal.WriteLine("--------------");

Table basicTable = new Table()
  .AddColumn("Name")
  .AddColumn("Value")
  .AddRow("Host", "localhost")
  .AddRow("Port", "8080")
  .AddRow("Protocol", "HTTP/2");

terminal.WriteTable(basicTable);
terminal.WriteLine();

// Example 2: Table with alignment
terminal.WriteLine("2. Table with Column Alignment");
terminal.WriteLine("-------------------------------");

Table alignedTable = new Table()
  .AddColumn("Package")
  .AddColumn("Downloads", Alignment.Right)
  .AddColumn("Version", Alignment.Center)
  .AddRow("Ardalis.GuardClauses", "12,543,210", "5.0.0")
  .AddRow("Ardalis.Result", "8,234,567", "10.0.0")
  .AddRow("TimeWarp.Nuru", "42,000", "3.0.0");

terminal.WriteTable(alignedTable);
terminal.WriteLine();

// Example 3: Table with styled content
terminal.WriteLine("3. Table with Styled Content");
terminal.WriteLine("----------------------------");

Table styledTable = new Table()
  .AddColumn("Test")
  .AddColumn("Status")
  .AddRow("Unit Tests", $"{AnsiColors.Green}PASSED{AnsiColors.Reset}")
  .AddRow("Integration Tests", $"{AnsiColors.Green}PASSED{AnsiColors.Reset}")
  .AddRow("E2E Tests", $"{AnsiColors.Red}FAILED{AnsiColors.Reset}");

terminal.WriteTable(styledTable);
terminal.WriteLine();

// Example 4: Different border styles
terminal.WriteLine("4. Border Styles");
terminal.WriteLine("----------------");

string[] borderNames = ["Square", "Rounded", "Double", "Heavy", "None"];
BorderStyle[] borderStyles = [BorderStyle.Square, BorderStyle.Rounded, BorderStyle.Doubled, BorderStyle.Heavy, BorderStyle.None];

for (int i = 0; i < borderStyles.Length; i++)
{
  terminal.WriteLine($"\n{borderNames[i]} Border:");
  Table borderTable = new Table()
    .AddColumn("A")
    .AddColumn("B")
    .AddRow("1", "2");
  borderTable.Border = borderStyles[i];
  terminal.WriteTable(borderTable);
}
terminal.WriteLine();

// Example 5: Table with colored border
terminal.WriteLine("5. Colored Border");
terminal.WriteLine("-----------------");

Table coloredBorderTable = new Table()
  .AddColumn("Project")
  .AddColumn("Status")
  .AddRow("Backend", "Running")
  .AddRow("Frontend", "Building");
coloredBorderTable.BorderColor = AnsiColors.Cyan;
coloredBorderTable.Border = BorderStyle.Rounded;

terminal.WriteTable(coloredBorderTable);
terminal.WriteLine();

// Example 6: Headerless table
terminal.WriteLine("6. Headerless Table");
terminal.WriteLine("-------------------");

Table headerlessTable = new Table()
  .AddColumn("Key")
  .AddColumn("Value")
  .AddRow("API_KEY", "sk-abc123...")
  .AddRow("DB_HOST", "database.example.com")
  .AddRow("CACHE_TTL", "3600");
headerlessTable.ShowHeaders = false;

terminal.WriteTable(headerlessTable);
terminal.WriteLine();

// Example 7: Table with row separators
terminal.WriteLine("7. Table with Row Separators");
terminal.WriteLine("----------------------------");

Table separatorsTable = new Table()
  .AddColumn("Time")
  .AddColumn("Event")
  .AddRow("09:00", "Meeting started")
  .AddRow("10:30", "Coffee break")
  .AddRow("11:00", "Presentation");
separatorsTable.ShowRowSeparators = true;

terminal.WriteTable(separatorsTable);
terminal.WriteLine();

// Example 8: Expanded table
terminal.WriteLine("8. Expanded Table (fills terminal width)");
terminal.WriteLine("-----------------------------------------");

Table expandedTable = new Table()
  .AddColumn("Name")
  .AddColumn("Description")
  .AddRow("table", "Renders columnar data")
  .AddRow("panel", "Renders bordered boxes")
  .AddRow("rule", "Renders horizontal dividers");
expandedTable.Expand = true;
expandedTable.Border = BorderStyle.Rounded;

terminal.WriteTable(expandedTable);
terminal.WriteLine();

// Example 9: Fluent builder pattern
terminal.WriteLine("9. Fluent Builder Pattern");
terminal.WriteLine("-------------------------");

terminal.WriteTable(t => t
  .AddColumns("Method", "Endpoint", "Status")
  .AddRow("GET", "/api/users", "200")
  .AddRow("POST", "/api/orders", "201")
  .AddRow("DELETE", "/api/items/42", "404")
  .Border(BorderStyle.Rounded));

terminal.WriteLine();

// Example 10: Shrink to fit terminal width (default behavior)
terminal.WriteLine("10. Shrink to Fit Terminal Width (default)");
terminal.WriteLine("-------------------------------------------");

Table shrinkTable = new Table()
  .AddColumn("Repository")
  .AddColumn(new TableColumn("Worktree Path") { TruncateMode = TruncateMode.Start })
  .AddColumn("Branch")
  .AddRow("timewarp-nuru", "/home/user/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/feature-branch-name", "feature-xyz")
  .AddRow("timewarp-state", "/home/user/worktrees/github.com/TimeWarpEngineering/timewarp-state/main", "main");
shrinkTable.Border = BorderStyle.Rounded;

terminal.WriteTable(shrinkTable);
terminal.WriteLine();

terminal.WriteLine("Note: Path column uses TruncateMode.Start to show the end of paths.");
terminal.WriteLine("Use table.Shrink = false or .Shrink(false) to disable shrinking.");
terminal.WriteLine();

// Example 12: TruncateMode options
terminal.WriteLine("12. TruncateMode Options");
terminal.WriteLine("------------------------");

string longText = "This-is-a-very-long-text-that-will-be-truncated-differently";

Table truncateModeTable = new Table()
  .AddColumn(new TableColumn("Mode") { MaxWidth = 8 })
  .AddColumn(new TableColumn("End (default)") { MaxWidth = 25, TruncateMode = TruncateMode.End })
  .AddColumn(new TableColumn("Start") { MaxWidth = 25, TruncateMode = TruncateMode.Start })
  .AddColumn(new TableColumn("Middle") { MaxWidth = 25, TruncateMode = TruncateMode.Middle })
  .AddRow("Result", longText, longText, longText);
truncateModeTable.Border = BorderStyle.Rounded;

terminal.WriteTable(truncateModeTable);
terminal.WriteLine();

terminal.WriteLine("TruncateMode.End:    'long text...'  - Shows beginning (default)");
terminal.WriteLine("TruncateMode.Start:  '...long text'  - Shows end (good for paths)");
terminal.WriteLine("TruncateMode.Middle: 'long...text'   - Shows both ends");
terminal.WriteLine();

// Example 11: Disable shrinking (allow overflow)
terminal.WriteLine("11. Shrink Disabled (allows horizontal overflow)");
terminal.WriteLine("------------------------------------------------");

terminal.WriteTable(t => t
  .AddColumn("Path")
  .AddRow("/home/user/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/very-long-feature-branch-name")
  .Border(BorderStyle.Rounded)
  .Shrink(false));

terminal.WriteLine();
terminal.WriteLine("Demo complete!");
