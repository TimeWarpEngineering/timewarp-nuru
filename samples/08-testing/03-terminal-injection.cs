#!/usr/bin/dotnet --
// test-terminal-injection - Demonstrates ITerminal injection into route handlers for testable colored output
// Uses NuruApp.CreateBuilder() with UseTerminal() for testable CLI apps
#:package Shouldly
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using Shouldly;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

Console.WriteLine("=== ITerminal Injection Tests ===\n");

// Test 1: Basic ITerminal injection
Console.WriteLine("Test 1: ITerminal injection in handlers");
{
  using TestTerminal terminal = new();

  NuruApp app = NuruApp.CreateBuilder()
    .UseTerminal(terminal)
    .Map("deploy {env}")
      .WithHandler((string env, ITerminal t) =>
      {
        t.WriteLine($"Deploying to {env}...".Cyan());
        t.WriteLine("Building artifacts...".Gray());
        t.WriteLine("Uploading files...".Gray());
        t.WriteLine($"Deployed to {env} successfully!".Green().Bold());
      })
      .AsCommand()
      .Done()
    .Build();

  await app.RunAsync(["deploy", "production"]);

  terminal.OutputContains("Deploying to production").ShouldBeTrue();
  terminal.OutputContains("Deployed to production successfully").ShouldBeTrue();
  Console.WriteLine("  PASSED".Green());
}

// Test 2: Conditional color based on terminal capabilities
Console.WriteLine("\nTest 2: Conditional color output");
{
  using TestTerminal terminal = new();

  NuruApp app = NuruApp.CreateBuilder()
    .UseTerminal(terminal)
    .Map("status")
      .WithHandler((ITerminal t) =>
      {
        string status = t.SupportsColor
          ? "OK".Green()
          : "OK";
        string warning = t.SupportsColor
          ? "WARNING".Yellow()
          : "WARNING";

        t.WriteLine($"Service A: {status}");
        t.WriteLine($"Service B: {warning}");
      })
      .AsQuery()
      .Done()
    .Build();

  await app.RunAsync(["status"]);

  terminal.OutputContains("Service A").ShouldBeTrue();
  terminal.OutputContains("Service B").ShouldBeTrue();
  Console.WriteLine("  PASSED".Green());
}

// Test 3: Error output with ITerminal
Console.WriteLine("\nTest 3: Error output capture");
{
  using TestTerminal terminal = new();

  NuruApp app = NuruApp.CreateBuilder()
    .UseTerminal(terminal)
    .Map("validate {file}")
      .WithHandler((string file, ITerminal t) =>
      {
        if (file == "bad.json")
        {
          t.WriteErrorLine($"Error: Invalid JSON in {file}".Red().Bold());
          t.WriteErrorLine("  Line 5: Unexpected token '}'".Red());
        }
        else
        {
          t.WriteLine($"Validated {file}".Green());
        }
      })
      .AsCommand()
      .Done()
    .Build();

  await app.RunAsync(["validate", "bad.json"]);

  terminal.ErrorContains("Invalid JSON").ShouldBeTrue();
  terminal.ErrorContains("bad.json").ShouldBeTrue();
  Console.WriteLine("  PASSED".Green());
}

// Test 4: Progress-style output
Console.WriteLine("\nTest 4: Progress-style output");
{
  using TestTerminal terminal = new();

  NuruApp app = NuruApp.CreateBuilder()
    .UseTerminal(terminal)
    .Map("build")
      .WithHandler((ITerminal t) =>
      {
        t.WriteLine("Build started".Cyan());
        t.WriteLine("  [1/4] Restoring packages...".Gray());
        t.WriteLine("  [2/4] Compiling source...".Gray());
        t.WriteLine("  [3/4] Running tests...".Gray());
        t.WriteLine("  [4/4] Creating artifacts...".Gray());
        t.WriteLine("Build completed successfully!".BrightGreen().Bold());
      })
      .AsCommand()
      .Done()
    .Build();

  await app.RunAsync(["build"]);

  terminal.GetOutputLines().Length.ShouldBe(6);
  terminal.OutputContains("Build started").ShouldBeTrue();
  terminal.OutputContains("Build completed successfully").ShouldBeTrue();
  Console.WriteLine("  PASSED".Green());
}

// Test 5: Custom colors with WithStyle
Console.WriteLine("\nTest 5: Custom colors with WithStyle()");
{
  using TestTerminal terminal = new();

  NuruApp app = NuruApp.CreateBuilder()
    .UseTerminal(terminal)
    .Map("theme")
      .WithHandler((ITerminal t) =>
      {
        t.WriteLine("Coral message".WithStyle(AnsiColors.Coral));
        t.WriteLine("Deep pink alert".WithStyle(AnsiColors.DeepPink));
        t.WriteLine("Dodger blue info".WithStyle(AnsiColors.DodgerBlue));
      })
      .AsQuery()
      .Done()
    .Build();

  await app.RunAsync(["theme"]);

  terminal.OutputContains("Coral message").ShouldBeTrue();
  terminal.OutputContains("Deep pink alert").ShouldBeTrue();
  terminal.OutputContains("Dodger blue info").ShouldBeTrue();
  Console.WriteLine("  PASSED".Green());
}

Console.WriteLine("\n=== All Tests Complete ===".BrightGreen().Bold());
