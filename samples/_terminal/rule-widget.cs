#!/usr/bin/dotnet --
// rule-widget-demo - Demonstrates the Rule widget for horizontal divider lines
// GitHub Issue: https://github.com/TimeWarpEngineering/timewarp-nuru/issues/89
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;
using TimeWarp.Terminal;

// Get a terminal instance
ITerminal terminal = TimeWarpTerminal.Default;

terminal.WriteLine();
terminal.WriteLine("Rule Widget Demo".Cyan().Bold());
terminal.WriteLine("Demonstrates horizontal divider lines with optional centered text");
terminal.WriteLine();

// Simple horizontal line
terminal.WriteLine("1. Simple Rule (no title):");
terminal.WriteRule();
terminal.WriteLine();

// Rule with centered title
terminal.WriteLine("2. Rule with centered title:");
terminal.WriteRule("Section Title");
terminal.WriteLine();

// Rule with styled title
terminal.WriteLine("3. Rule with styled title:");
terminal.WriteRule("Results".Cyan().Bold());
terminal.WriteLine();

// Different line styles
terminal.WriteLine("4. Different line styles:");
terminal.WriteLine();
terminal.WriteLine("   Thin (default):");
terminal.WriteRule("Thin Style", LineStyle.Thin);
terminal.WriteLine();
terminal.WriteLine("   Doubled:");
terminal.WriteRule("Doubled Style", LineStyle.Doubled);
terminal.WriteLine();
terminal.WriteLine("   Heavy:");
terminal.WriteRule("Heavy Style", LineStyle.Heavy);
terminal.WriteLine();

// Fluent builder API
terminal.WriteLine("5. Fluent builder API:");
terminal.WriteRule(rule => rule
    .Title("Configuration")
    .Style(LineStyle.Doubled)
    .Color(AnsiColors.Cyan));
terminal.WriteLine();

// Colored rules
terminal.WriteLine("6. Colored rules:");
terminal.WriteRule(rule => rule
    .Title("Success".Green())
    .Color(AnsiColors.Green));

terminal.WriteRule(rule => rule
    .Title("Warning".Yellow())
    .Color(AnsiColors.Yellow));

terminal.WriteRule(rule => rule
    .Title("Error".Red())
    .Color(AnsiColors.Red));
terminal.WriteLine();

// Pre-configured rule
terminal.WriteLine("7. Pre-configured Rule object:");
Rule customRule = new()
{
  Title = "Custom Configuration",
  Style = LineStyle.Heavy,
  Color = AnsiColors.Magenta
};
terminal.WriteRule(customRule);
terminal.WriteLine();

// Practical example
terminal.WriteLine("8. Practical example - CLI output sections:");
terminal.WriteLine();

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
terminal.WriteLine();

return 0;
