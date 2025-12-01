#!/usr/bin/dotnet --
// panel-widget-demo - Demonstrates the Panel widget for bordered boxes
// GitHub Issue: https://github.com/TimeWarpEngineering/timewarp-nuru/issues/90
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

// Get a terminal instance
ITerminal terminal = NuruTerminal.Default;

Console.WriteLine();
Console.WriteLine("Panel Widget Demo".Cyan().Bold());
Console.WriteLine("Demonstrates bordered boxes with optional headers and styled content");
Console.WriteLine();

// Simple panel with content
Console.WriteLine("1. Simple Panel:");
terminal.WritePanel("This is important information");
Console.WriteLine();

// Panel with header
Console.WriteLine("2. Panel with header:");
terminal.WritePanel("Content goes here", "Notice");
Console.WriteLine();

// Different border styles
Console.WriteLine("3. Different border styles:");
Console.WriteLine();

Console.WriteLine("   Rounded (default):");
terminal.WritePanel(panel => panel
    .Header("Rounded")
    .Content("Soft corners ╭╮╰╯")
    .Border(BorderStyle.Rounded));
Console.WriteLine();

Console.WriteLine("   Square:");
terminal.WritePanel(panel => panel
    .Header("Square")
    .Content("Sharp corners ┌┐└┘")
    .Border(BorderStyle.Square));
Console.WriteLine();

Console.WriteLine("   Double:");
terminal.WritePanel(panel => panel
    .Header("Double")
    .Content("Double lines ╔╗╚╝")
    .Border(BorderStyle.Doubled));
Console.WriteLine();

Console.WriteLine("   Heavy:");
terminal.WritePanel(panel => panel
    .Header("Heavy")
    .Content("Thick lines ┏┓┗┛")
    .Border(BorderStyle.Heavy));
Console.WriteLine();

// Multi-line content
Console.WriteLine("4. Multi-line content:");
terminal.WritePanel(panel => panel
    .Header("Team Members")
    .Content("Alice - Developer\nBob - Designer\nCharlie - Manager")
    .Border(BorderStyle.Rounded));
Console.WriteLine();

// Padding options
Console.WriteLine("5. Padding options:");
Console.WriteLine();

Console.WriteLine("   Default padding (horizontal=1, vertical=0):");
terminal.WritePanel("Compact");
Console.WriteLine();

Console.WriteLine("   More padding (horizontal=3, vertical=1):");
terminal.WritePanel(panel => panel
    .Content("Spacious content")
    .Padding(3, 1));
Console.WriteLine();

// Colored borders
Console.WriteLine("6. Colored borders:");
terminal.WritePanel(panel => panel
    .Header("Success".Green())
    .Content("Operation completed successfully")
    .BorderColor(AnsiColors.Green));

terminal.WritePanel(panel => panel
    .Header("Warning".Yellow())
    .Content("Proceed with caution")
    .BorderColor(AnsiColors.Yellow));

terminal.WritePanel(panel => panel
    .Header("Error".Red())
    .Content("Something went wrong")
    .BorderColor(AnsiColors.Red));
Console.WriteLine();

// Fixed width panel
Console.WriteLine("7. Fixed width panel (30 characters):");
terminal.WritePanel(panel => panel
    .Header("Fixed")
    .Content("30 chars wide")
    .Width(30));
Console.WriteLine();

// Styled header and content
Console.WriteLine("8. Styled header and content:");
terminal.WritePanel(panel => panel
    .Header("💠 Ardalis".Cyan().Bold())
    .Content("Steve 'Ardalis' Smith\n" + "Software Architect".Gray())
    .Border(BorderStyle.Rounded)
    .BorderColor(AnsiColors.Cyan)
    .Padding(2, 1));
Console.WriteLine();

// Pre-configured panel
Console.WriteLine("9. Pre-configured Panel object:");
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
Console.WriteLine();

// Practical example
Console.WriteLine("10. Practical example - CLI status display:");
Console.WriteLine();

terminal.WritePanel(panel => panel
    .Header("Build Status".Bold())
    .Content($"{"Project:".Gray()}  TimeWarp.Nuru\n" +
             $"{"Status:".Gray()}   {"✓ Success".Green()}\n" +
             $"{"Duration:".Gray()} 2.34s")
    .Border(BorderStyle.Rounded)
    .BorderColor(AnsiColors.BrightGreen)
    .Padding(2, 1));
Console.WriteLine();

return 0;
