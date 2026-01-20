#!/usr/bin/dotnet --
// test-colored-output - Demonstrates testing handlers with colored output
// Uses NuruApp.CreateBuilder() with UseTerminal() for testable CLI apps
#:package Shouldly
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using Shouldly;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

Console.WriteLine("=== Testing Colored Output ===\n");

// Test 1: Basic colored output
Console.WriteLine("Test 1: Handler with colored output");
{
  using TestTerminal terminal = new();

  NuruApp app = NuruApp.CreateBuilder()
    .UseTerminal(terminal)
    .Map("status")
      .WithHandler((ITerminal t) =>
      {
        t.WriteLine("All systems operational".Green());
        t.WriteLine("2 warnings".Yellow());
        t.WriteLine("0 errors".BrightGreen());
      })
      .AsQuery()
      .Done()
    .Build();

  await app.RunAsync(["status"]);

  // Output contains ANSI codes but text is still searchable
  terminal.OutputContains("All systems operational").ShouldBeTrue();
  terminal.OutputContains("2 warnings").ShouldBeTrue();
  terminal.OutputContains("0 errors").ShouldBeTrue();

  Console.WriteLine("  Captured output (with ANSI codes):");
  foreach (string line in terminal.GetOutputLines())
  {
    Console.WriteLine($"    {line}");
  }

  Console.WriteLine("  PASSED".Green());
}

// Test 2: Formatted text (bold, italic, underline)
Console.WriteLine("\nTest 2: Text formatting");
{
  using TestTerminal terminal = new();

  NuruApp app = NuruApp.CreateBuilder()
    .UseTerminal(terminal)
    .Map("format")
      .WithHandler((ITerminal t) =>
      {
        t.WriteLine("Important".Bold());
        t.WriteLine("Emphasis".Italic());
        t.WriteLine("Link".Underline());
      })
      .AsQuery()
      .Done()
    .Build();

  await app.RunAsync(["format"]);

  terminal.OutputContains("Important").ShouldBeTrue();
  terminal.OutputContains("Emphasis").ShouldBeTrue();
  terminal.OutputContains("Link").ShouldBeTrue();
  Console.WriteLine("  PASSED".Green());
}

// Test 3: Chained styles
Console.WriteLine("\nTest 3: Chained styles");
{
  using TestTerminal terminal = new();

  NuruApp app = NuruApp.CreateBuilder()
    .UseTerminal(terminal)
    .Map("alert")
      .WithHandler((ITerminal t) =>
      {
        t.WriteLine("Critical Error".Red().Bold());
        t.WriteLine("Success!".Green().Bold().Underline());
      })
      .AsCommand()
      .Done()
    .Build();

  await app.RunAsync(["alert"]);

  terminal.OutputContains("Critical Error").ShouldBeTrue();
  terminal.OutputContains("Success!").ShouldBeTrue();

  Console.WriteLine("  Captured styled output:");
  foreach (string line in terminal.GetOutputLines())
  {
    Console.WriteLine($"    {line}");
  }

  Console.WriteLine("  PASSED".Green());
}

// Test 4: CSS named colors
Console.WriteLine("\nTest 4: CSS named colors");
{
  using TestTerminal terminal = new();

  NuruApp app = NuruApp.CreateBuilder()
    .UseTerminal(terminal)
    .Map("palette")
      .WithHandler((ITerminal t) =>
      {
        t.WriteLine("Coral text".Coral());
        t.WriteLine("Gold highlight".Gold());
        t.WriteLine("Navy message".Navy());
      })
      .AsQuery()
      .Done()
    .Build();

  await app.RunAsync(["palette"]);

  terminal.OutputContains("Coral text").ShouldBeTrue();
  terminal.OutputContains("Gold highlight").ShouldBeTrue();
  terminal.OutputContains("Navy message").ShouldBeTrue();
  Console.WriteLine("  PASSED".Green());
}

// Test 5a: Color support detection - with color
Console.WriteLine("\nTest 5a: Color terminal");
string colorOutput;
{
  using TestTerminal colorTerminal = new() { SupportsColor = true };

  NuruApp app = NuruApp.CreateBuilder()
    .UseTerminal(colorTerminal)
    .Map("check")
      .WithHandler((ITerminal t) =>
      {
        if (t.SupportsColor)
          t.WriteLine("Colors enabled".Green());
        else
          t.WriteLine("Colors disabled");
      })
      .AsQuery()
      .Done()
    .Build();

  await app.RunAsync(["check"]);

  colorTerminal.OutputContains("Colors enabled").ShouldBeTrue();
  colorOutput = colorTerminal.Output.Trim();
  Console.WriteLine("  PASSED".Green());
}

// Test 5b: Color support detection - without color
// NOTE: Separate block due to generator limitation with multiple apps in same block (see task #319)
Console.WriteLine("\nTest 5b: No-color terminal");
string noColorOutput;
{
  using TestTerminal noColorTerminal = new() { SupportsColor = false };

  NuruApp app = NuruApp.CreateBuilder()
    .UseTerminal(noColorTerminal)
    .Map("check")
      .WithHandler((ITerminal t) =>
      {
        if (t.SupportsColor)
          t.WriteLine("Colors enabled".Green());
        else
          t.WriteLine("Colors disabled");
      })
      .AsQuery()
      .Done()
    .Build();

  await app.RunAsync(["check"]);

  noColorTerminal.OutputContains("Colors disabled").ShouldBeTrue();
  noColorOutput = noColorTerminal.Output.Trim();
  Console.WriteLine("  PASSED".Green());
}

Console.WriteLine($"  Color terminal output: {colorOutput}");
Console.WriteLine($"  No-color terminal output: {noColorOutput}");

Console.WriteLine("\n=== All Tests Complete ===".BrightGreen().Bold());
