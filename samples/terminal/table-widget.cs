#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Demonstrates the Table widget for rendering columnar data
using TimeWarp.Nuru;
using TimeWarp.Terminal;

// Create a terminal for colored output
ITerminal terminal = new NuruTerminal();

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
terminal.WriteLine("Demo complete!");
