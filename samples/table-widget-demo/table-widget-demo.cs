#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Demonstrates the Table widget for rendering columnar data
using TimeWarp.Nuru;

Console.WriteLine("Table Widget Demo");
Console.WriteLine("==================\n");

// Create a terminal for colored output
ITerminal terminal = new NuruTerminal();

// Example 1: Basic table with two columns
Console.WriteLine("1. Basic Table");
Console.WriteLine("--------------");

Table basicTable = new Table()
  .AddColumn("Name")
  .AddColumn("Value")
  .AddRow("Host", "localhost")
  .AddRow("Port", "8080")
  .AddRow("Protocol", "HTTP/2");

terminal.WriteTable(basicTable);
Console.WriteLine();

// Example 2: Table with alignment
Console.WriteLine("2. Table with Column Alignment");
Console.WriteLine("-------------------------------");

Table alignedTable = new Table()
  .AddColumn("Package")
  .AddColumn("Downloads", Alignment.Right)
  .AddColumn("Version", Alignment.Center)
  .AddRow("Ardalis.GuardClauses", "12,543,210", "5.0.0")
  .AddRow("Ardalis.Result", "8,234,567", "10.0.0")
  .AddRow("TimeWarp.Nuru", "42,000", "3.0.0");

terminal.WriteTable(alignedTable);
Console.WriteLine();

// Example 3: Table with styled content
Console.WriteLine("3. Table with Styled Content");
Console.WriteLine("----------------------------");

Table styledTable = new Table()
  .AddColumn("Test")
  .AddColumn("Status")
  .AddRow("Unit Tests", $"{AnsiColors.Green}PASSED{AnsiColors.Reset}")
  .AddRow("Integration Tests", $"{AnsiColors.Green}PASSED{AnsiColors.Reset}")
  .AddRow("E2E Tests", $"{AnsiColors.Red}FAILED{AnsiColors.Reset}");

terminal.WriteTable(styledTable);
Console.WriteLine();

// Example 4: Different border styles
Console.WriteLine("4. Border Styles");
Console.WriteLine("----------------");

string[] borderNames = ["Square", "Rounded", "Double", "Heavy", "None"];
BorderStyle[] borderStyles = [BorderStyle.Square, BorderStyle.Rounded, BorderStyle.Doubled, BorderStyle.Heavy, BorderStyle.None];

for (int i = 0; i < borderStyles.Length; i++)
{
  Console.WriteLine($"\n{borderNames[i]} Border:");
  Table borderTable = new Table()
    .AddColumn("A")
    .AddColumn("B")
    .AddRow("1", "2");
  borderTable.Border = borderStyles[i];
  terminal.WriteTable(borderTable);
}
Console.WriteLine();

// Example 5: Table with colored border
Console.WriteLine("5. Colored Border");
Console.WriteLine("-----------------");

Table coloredBorderTable = new Table()
  .AddColumn("Project")
  .AddColumn("Status")
  .AddRow("Backend", "Running")
  .AddRow("Frontend", "Building");
coloredBorderTable.BorderColor = AnsiColors.Cyan;
coloredBorderTable.Border = BorderStyle.Rounded;

terminal.WriteTable(coloredBorderTable);
Console.WriteLine();

// Example 6: Headerless table
Console.WriteLine("6. Headerless Table");
Console.WriteLine("-------------------");

Table headerlessTable = new Table()
  .AddColumn("Key")
  .AddColumn("Value")
  .AddRow("API_KEY", "sk-abc123...")
  .AddRow("DB_HOST", "database.example.com")
  .AddRow("CACHE_TTL", "3600");
headerlessTable.ShowHeaders = false;

terminal.WriteTable(headerlessTable);
Console.WriteLine();

// Example 7: Table with row separators
Console.WriteLine("7. Table with Row Separators");
Console.WriteLine("----------------------------");

Table separatorsTable = new Table()
  .AddColumn("Time")
  .AddColumn("Event")
  .AddRow("09:00", "Meeting started")
  .AddRow("10:30", "Coffee break")
  .AddRow("11:00", "Presentation");
separatorsTable.ShowRowSeparators = true;

terminal.WriteTable(separatorsTable);
Console.WriteLine();

// Example 8: Expanded table
Console.WriteLine("8. Expanded Table (fills terminal width)");
Console.WriteLine("-----------------------------------------");

Table expandedTable = new Table()
  .AddColumn("Name")
  .AddColumn("Description")
  .AddRow("table", "Renders columnar data")
  .AddRow("panel", "Renders bordered boxes")
  .AddRow("rule", "Renders horizontal dividers");
expandedTable.Expand = true;
expandedTable.Border = BorderStyle.Rounded;

terminal.WriteTable(expandedTable);
Console.WriteLine();

// Example 9: Fluent builder pattern
Console.WriteLine("9. Fluent Builder Pattern");
Console.WriteLine("-------------------------");

terminal.WriteTable(t => t
  .AddColumns("Method", "Endpoint", "Status")
  .AddRow("GET", "/api/users", "200")
  .AddRow("POST", "/api/orders", "201")
  .AddRow("DELETE", "/api/items/42", "404")
  .Border(BorderStyle.Rounded));

Console.WriteLine();
Console.WriteLine("Demo complete!");
