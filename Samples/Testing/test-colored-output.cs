#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

// Demonstrates testing handlers with colored output

using TimeWarp.Nuru;

Console.WriteLine("=== Testing Colored Output ===\n");

// Test 1: Basic colored output
Console.WriteLine("Test 1: Handler with colored output");
{
  using var terminal = new TestTerminal();

  NuruApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("status", (ITerminal t) =>
    {
      t.WriteLine("All systems operational".Green());
      t.WriteLine("2 warnings".Yellow());
      t.WriteLine("0 errors".BrightGreen());
    })
    .Build();

  await app.RunAsync(["status"]);

  // Output contains ANSI codes but text is still searchable
  bool passed =
    terminal.OutputContains("All systems operational") &&
    terminal.OutputContains("2 warnings") &&
    terminal.OutputContains("0 errors");

  Console.WriteLine("  Captured output (with ANSI codes):");
  foreach (string line in terminal.GetOutputLines())
  {
    Console.WriteLine($"    {line}");
  }
  Console.WriteLine($"  Result: {(passed ? "PASSED".Green() : "FAILED".Red())}\n");
}

// Test 2: Formatted text (bold, italic, underline)
Console.WriteLine("Test 2: Text formatting");
{
  using var terminal = new TestTerminal();

  NuruApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("format", (ITerminal t) =>
    {
      t.WriteLine("Important".Bold());
      t.WriteLine("Emphasis".Italic());
      t.WriteLine("Link".Underline());
    })
    .Build();

  await app.RunAsync(["format"]);

  bool passed =
    terminal.OutputContains("Important") &&
    terminal.OutputContains("Emphasis") &&
    terminal.OutputContains("Link");

  Console.WriteLine($"  Contains expected text: {passed}");
  Console.WriteLine($"  Result: {(passed ? "PASSED".Green() : "FAILED".Red())}\n");
}

// Test 3: Chained styles
Console.WriteLine("Test 3: Chained styles");
{
  using var terminal = new TestTerminal();

  NuruApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("alert", (ITerminal t) =>
    {
      t.WriteLine("Critical Error".Red().Bold());
      t.WriteLine("Success!".Green().Bold().Underline());
    })
    .Build();

  await app.RunAsync(["alert"]);

  bool passed =
    terminal.OutputContains("Critical Error") &&
    terminal.OutputContains("Success!");

  Console.WriteLine("  Captured styled output:");
  foreach (string line in terminal.GetOutputLines())
  {
    Console.WriteLine($"    {line}");
  }
  Console.WriteLine($"  Result: {(passed ? "PASSED".Green() : "FAILED".Red())}\n");
}

// Test 4: CSS named colors
Console.WriteLine("Test 4: CSS named colors");
{
  using var terminal = new TestTerminal();

  NuruApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("palette", (ITerminal t) =>
    {
      t.WriteLine("Coral text".Coral());
      t.WriteLine("Gold highlight".Gold());
      t.WriteLine("Navy message".Navy());
    })
    .Build();

  await app.RunAsync(["palette"]);

  bool passed =
    terminal.OutputContains("Coral text") &&
    terminal.OutputContains("Gold highlight") &&
    terminal.OutputContains("Navy message");

  Console.WriteLine($"  Contains CSS color names: {passed}");
  Console.WriteLine($"  Result: {(passed ? "PASSED".Green() : "FAILED".Red())}\n");
}

// Test 5: Color support detection
Console.WriteLine("Test 5: Color support detection");
{
  // TestTerminal.SupportsColor is true by default
  using var colorTerminal = new TestTerminal { SupportsColor = true };
  using var noColorTerminal = new TestTerminal { SupportsColor = false };

  NuruApp app = new NuruAppBuilder()
    .UseTerminal(colorTerminal)
    .Map("check", (ITerminal t) =>
    {
      if (t.SupportsColor)
        t.WriteLine("Colors enabled".Green());
      else
        t.WriteLine("Colors disabled");
    })
    .Build();

  await app.RunAsync(["check"]);

  NuruApp appNoColor = new NuruAppBuilder()
    .UseTerminal(noColorTerminal)
    .Map("check", (ITerminal t) =>
    {
      if (t.SupportsColor)
        t.WriteLine("Colors enabled".Green());
      else
        t.WriteLine("Colors disabled");
    })
    .Build();

  await appNoColor.RunAsync(["check"]);

  bool passed =
    colorTerminal.OutputContains("Colors enabled") &&
    noColorTerminal.OutputContains("Colors disabled");

  Console.WriteLine($"  Color terminal output: {colorTerminal.Output.Trim()}");
  Console.WriteLine($"  No-color terminal output: {noColorTerminal.Output.Trim()}");
  Console.WriteLine($"  Result: {(passed ? "PASSED".Green() : "FAILED".Red())}\n");
}

Console.WriteLine("=== All Tests Complete ===".BrightGreen().Bold());
