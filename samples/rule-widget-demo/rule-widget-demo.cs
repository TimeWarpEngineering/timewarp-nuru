#!/usr/bin/dotnet --
// rule-widget-demo - Demonstrates the Rule widget for horizontal divider lines
// GitHub Issue: https://github.com/TimeWarpEngineering/timewarp-nuru/issues/89
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

// Get a terminal instance
ITerminal terminal = NuruTerminal.Default;

Console.WriteLine();
Console.WriteLine("Rule Widget Demo".Cyan().Bold());
Console.WriteLine("Demonstrates horizontal divider lines with optional centered text");
Console.WriteLine();

// Simple horizontal line
Console.WriteLine("1. Simple Rule (no title):");
terminal.WriteRule();
Console.WriteLine();

// Rule with centered title
Console.WriteLine("2. Rule with centered title:");
terminal.WriteRule("Section Title");
Console.WriteLine();

// Rule with styled title
Console.WriteLine("3. Rule with styled title:");
terminal.WriteRule("Results".Cyan().Bold());
Console.WriteLine();

// Different line styles
Console.WriteLine("4. Different line styles:");
Console.WriteLine();
Console.WriteLine("   Thin (default):");
terminal.WriteRule("Thin Style", LineStyle.Thin);
Console.WriteLine();
Console.WriteLine("   Doubled:");
terminal.WriteRule("Doubled Style", LineStyle.Doubled);
Console.WriteLine();
Console.WriteLine("   Heavy:");
terminal.WriteRule("Heavy Style", LineStyle.Heavy);
Console.WriteLine();

// Fluent builder API
Console.WriteLine("5. Fluent builder API:");
terminal.WriteRule(rule => rule
    .Title("Configuration")
    .Style(LineStyle.Doubled)
    .Color(AnsiColors.Cyan));
Console.WriteLine();

// Colored rules
Console.WriteLine("6. Colored rules:");
terminal.WriteRule(rule => rule
    .Title("Success".Green())
    .Color(AnsiColors.Green));

terminal.WriteRule(rule => rule
    .Title("Warning".Yellow())
    .Color(AnsiColors.Yellow));

terminal.WriteRule(rule => rule
    .Title("Error".Red())
    .Color(AnsiColors.Red));
Console.WriteLine();

// Pre-configured rule
Console.WriteLine("7. Pre-configured Rule object:");
Rule customRule = new()
{
  Title = "Custom Configuration",
  Style = LineStyle.Heavy,
  Color = AnsiColors.Magenta
};
terminal.WriteRule(customRule);
Console.WriteLine();

// Practical example
Console.WriteLine("8. Practical example - CLI output sections:");
Console.WriteLine();

terminal.WriteRule("Build Output");
Console.WriteLine("  Compiling project...");
Console.WriteLine("  Build succeeded.");
Console.WriteLine();

terminal.WriteRule("Test Results", LineStyle.Doubled);
Console.WriteLine("  ✓ 42 tests passed");
Console.WriteLine("  ✗ 0 tests failed");
Console.WriteLine();

terminal.WriteRule(rule => rule
    .Title("Summary".Bold())
    .Style(LineStyle.Heavy)
    .Color(AnsiColors.BrightGreen));
Console.WriteLine("  Total time: 1.23s");
Console.WriteLine("  Status: " + "SUCCESS".Green().Bold());
Console.WriteLine();

return 0;
