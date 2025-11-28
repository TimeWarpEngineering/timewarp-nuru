#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

// Demonstrates ITerminal injection into route handlers for testable colored output

using TimeWarp.Nuru;

Console.WriteLine("=== ITerminal Injection Demo ===\n");

// Demo 1: Basic ITerminal injection
Console.WriteLine("Demo 1: ITerminal injection in handlers\n".Cyan().Bold());
{
  using var terminal = new TestTerminal();

  NuruApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("deploy {env}", (string env, ITerminal t) =>
    {
      t.WriteLine($"Deploying to {env}...".Cyan());
      // Simulate work
      t.WriteLine("Building artifacts...".Gray());
      t.WriteLine("Uploading files...".Gray());
      t.WriteLine($"Deployed to {env} successfully!".Green().Bold());
    })
    .Build();

  await app.RunAsync(["deploy", "production"]);

  Console.WriteLine("Captured output:");
  foreach (string line in terminal.GetOutputLines())
  {
    Console.WriteLine($"  {line}");
  }
  Console.WriteLine();
}

// Demo 2: Conditional color based on terminal capabilities
Console.WriteLine("Demo 2: Conditional color output\n".Cyan().Bold());
{
  using var terminal = new TestTerminal();

  NuruApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("status", (ITerminal t) =>
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
    .Build();

  await app.RunAsync(["status"]);

  Console.WriteLine("With color support (TestTerminal.SupportsColor = true):");
  foreach (string line in terminal.GetOutputLines())
  {
    Console.WriteLine($"  {line}");
  }
  Console.WriteLine();
}

// Demo 3: Error handling with ITerminal
Console.WriteLine("Demo 3: Error handling with colored output\n".Cyan().Bold());
{
  using var terminal = new TestTerminal();

  NuruApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("validate {file}", (string file, ITerminal t) =>
    {
      // Simulate validation
      if (file == "bad.json")
      {
        t.WriteErrorLine($"Error: Invalid JSON in {file}".Red().Bold());
        t.WriteErrorLine("  Line 5: Unexpected token '}'".Red());
        return 1;
      }

      t.WriteLine($"Validated {file}".Green());
      return 0;
    })
    .Build();

  int exitCode = await app.RunAsync(["validate", "bad.json"]);

  Console.WriteLine($"Exit code: {exitCode}");
  Console.WriteLine("Captured error output:");
  foreach (string line in terminal.ErrorOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
  {
    Console.WriteLine($"  {line}");
  }
  Console.WriteLine();
}

// Demo 4: Progress-style output
Console.WriteLine("Demo 4: Progress-style output\n".Cyan().Bold());
{
  using var terminal = new TestTerminal();

  NuruApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("build", (ITerminal t) =>
    {
      t.WriteLine("Build started".Cyan());
      t.WriteLine("  [1/4] Restoring packages...".Gray());
      t.WriteLine("  [2/4] Compiling source...".Gray());
      t.WriteLine("  [3/4] Running tests...".Gray());
      t.WriteLine("  [4/4] Creating artifacts...".Gray());
      t.WriteLine("Build completed successfully!".BrightGreen().Bold());
    })
    .Build();

  await app.RunAsync(["build"]);

  Console.WriteLine("Captured build output:");
  foreach (string line in terminal.GetOutputLines())
  {
    Console.WriteLine($"  {line}");
  }
  Console.WriteLine();
}

// Demo 5: Using WithStyle for custom colors
Console.WriteLine("Demo 5: Custom colors with WithStyle()\n".Cyan().Bold());
{
  using var terminal = new TestTerminal();

  NuruApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("theme", (ITerminal t) =>
    {
      // Using WithStyle for custom ANSI codes
      t.WriteLine("Coral message".WithStyle(AnsiColors.Coral));
      t.WriteLine("Deep pink alert".WithStyle(AnsiColors.DeepPink));
      t.WriteLine("Dodger blue info".WithStyle(AnsiColors.DodgerBlue));
    })
    .Build();

  await app.RunAsync(["theme"]);

  Console.WriteLine("Captured themed output:");
  foreach (string line in terminal.GetOutputLines())
  {
    Console.WriteLine($"  {line}");
  }
  Console.WriteLine();
}

Console.WriteLine("=== Demo Complete ===".BrightGreen().Bold());
