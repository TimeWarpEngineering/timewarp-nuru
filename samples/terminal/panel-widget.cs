#!/usr/bin/dotnet --
// panel-widget-demo - Demonstrates the Panel widget for bordered boxes
// GitHub Issue: https://github.com/TimeWarpEngineering/timewarp-nuru/issues/90
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;
using TimeWarp.Terminal;

// Get a terminal instance
ITerminal terminal = NuruTerminal.Default;

terminal.WriteLine();
terminal.WriteLine("Panel Widget Demo".Cyan().Bold());
terminal.WriteLine("Demonstrates bordered boxes with optional headers and styled content");
terminal.WriteLine();

// Simple panel with content
terminal.WriteLine("1. Simple Panel:");
terminal.WritePanel("This is important information");
terminal.WriteLine();

// Panel with header
terminal.WriteLine("2. Panel with header:");
terminal.WritePanel("Content goes here", "Notice");
terminal.WriteLine();

// Different border styles
terminal.WriteLine("3. Different border styles:");
terminal.WriteLine();

terminal.WriteLine("   Rounded (default):");
terminal.WritePanel(panel => panel
    .Header("Rounded")
    .Content("Soft corners ╭╮╰╯")
    .Border(BorderStyle.Rounded));
terminal.WriteLine();

terminal.WriteLine("   Square:");
terminal.WritePanel(panel => panel
    .Header("Square")
    .Content("Sharp corners ┌┐└┘")
    .Border(BorderStyle.Square));
terminal.WriteLine();

terminal.WriteLine("   Double:");
terminal.WritePanel(panel => panel
    .Header("Double")
    .Content("Double lines ╔╗╚╝")
    .Border(BorderStyle.Doubled));
terminal.WriteLine();

terminal.WriteLine("   Heavy:");
terminal.WritePanel(panel => panel
    .Header("Heavy")
    .Content("Thick lines ┏┓┗┛")
    .Border(BorderStyle.Heavy));
terminal.WriteLine();

// Multi-line content
terminal.WriteLine("4. Multi-line content:");
terminal.WritePanel(panel => panel
    .Header("Team Members")
    .Content("Alice - Developer\nBob - Designer\nCharlie - Manager")
    .Border(BorderStyle.Rounded));
terminal.WriteLine();

// Padding options
terminal.WriteLine("5. Padding options:");
terminal.WriteLine();

terminal.WriteLine("   Default padding (horizontal=1, vertical=0):");
terminal.WritePanel("Compact");
terminal.WriteLine();

terminal.WriteLine("   More padding (horizontal=3, vertical=1):");
terminal.WritePanel(panel => panel
    .Content("Spacious content")
    .Padding(3, 1));
terminal.WriteLine();

// Colored borders
terminal.WriteLine("6. Colored borders:");
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
terminal.WriteLine();

// Fixed width panel
terminal.WriteLine("7. Fixed width panel (30 characters):");
terminal.WritePanel(panel => panel
    .Header("Fixed")
    .Content("30 chars wide")
    .Width(30));
terminal.WriteLine();

// Styled header and content
terminal.WriteLine("8. Styled header and content:");
terminal.WritePanel(panel => panel
    .Header("💠 Ardalis".Cyan().Bold())
    .Content("Steve 'Ardalis' Smith\n" + "Software Architect".Gray())
    .Border(BorderStyle.Rounded)
    .BorderColor(AnsiColors.Cyan)
    .Padding(2, 1));
terminal.WriteLine();

// Pre-configured panel
terminal.WriteLine("9. Pre-configured Panel object:");
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
terminal.WriteLine();

// Practical example
terminal.WriteLine("10. Practical example - CLI status display:");
terminal.WriteLine();

terminal.WritePanel(panel => panel
    .Header("Build Status".Bold())
    .Content($"{"Project:".Gray()}  TimeWarp.Nuru\n" +
             $"{"Status:".Gray()}   {"✓ Success".Green()}\n" +
             $"{"Duration:".Gray()} 2.34s")
    .Border(BorderStyle.Rounded)
    .BorderColor(AnsiColors.BrightGreen)
    .Padding(2, 1));
terminal.WriteLine();

return 0;
