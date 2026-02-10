#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - REPL OPTIONS SHOWCASE ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// Comprehensive REPL configuration demonstrating all available options.
//
// DSL: Endpoint with full ReplOptions customization
//
// FEATURES:
//   - Custom prompts (static and dynamic)
//   - Welcome/goodbye messages
//   - History configuration
//   - Arrow key history navigation
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;

// Single Build() with AddRepl() - handles both CLI and REPL modes
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .AddRepl(options =>
  {
    // Prompts
    options.Prompt = "nuru> ";
    options.ContinuationPrompt = "... ";

    // Messages
    options.WelcomeMessage = """
      ╔════════════════════════════════════════════════╗
      ║     TimeWarp.Nuru Interactive Shell          ║
      ║     Type 'help' for available commands       ║
      ╚════════════════════════════════════════════════╝
      """;
    options.GoodbyeMessage = "Thank you for using Nuru!";

    // History
    options.PersistHistory = true;
    options.HistoryFilePath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".nuru_history"
    );
    options.MaxHistorySize = 1000;

    // Behavior
    options.ContinueOnError = true;
    options.ShowExitCode = false;
    options.ShowTiming = true;
    options.EnableArrowHistory = true;
    options.EnableColors = true;
    options.PromptColor = "\x1b[32m"; // Green

    // Auto-start REPL when no arguments provided
    options.AutoStartWhenEmpty = true;
  })
  .Build();

// RunAsync handles both CLI and REPL modes automatically
return await app.RunAsync(args);

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

[NuruRoute("help", Description = "Show help information")]
public sealed class HelpCommand : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<HelpCommand, Unit>
  {
    public ValueTask<Unit> Handle(HelpCommand c, CancellationToken ct)
    {
      Console.WriteLine("""
        Available Commands:
          greet <name>     - Greet someone
          calc <expr>      - Calculate expression
          date             - Show current date
          exit             - Exit REPL
        """);
      return default;
    }
  }
}

[NuruRoute("greet", Description = "Greet someone")]
public sealed class GreetCommand : ICommand<Unit>
{
  [Parameter] public string Name { get; set; } = "";

  public sealed class Handler : ICommandHandler<GreetCommand, Unit>
  {
    public ValueTask<Unit> Handle(GreetCommand c, CancellationToken ct)
    {
      Console.WriteLine($"Hello, {c.Name}!");
      return default;
    }
  }
}

[NuruRoute("calc", Description = "Calculate simple expression")]
public sealed class CalcCommand : ICommand<double>
{
  [Parameter] public double X { get; set; }
  [Parameter] public string Op { get; set; } = "+";
  [Parameter] public double Y { get; set; }

  public sealed class Handler : ICommandHandler<CalcCommand, double>
  {
    public ValueTask<double> Handle(CalcCommand c, CancellationToken ct)
    {
      double result = c.Op switch
      {
        "+" => c.X + c.Y,
        "-" => c.X - c.Y,
        "*" or "x" => c.X * c.Y,
        "/" => c.X / c.Y,
        _ => throw new ArgumentException($"Unknown operator: {c.Op}")
      };

      Console.WriteLine($"{c.X} {c.Op} {c.Y} = {result}");
      return new ValueTask<double>(result);
    }
  }
}

[NuruRoute("date", Description = "Show current date and time")]
public sealed class DateCommand : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<DateCommand, Unit>
  {
    public ValueTask<Unit> Handle(DateCommand q, CancellationToken ct)
    {
      Console.WriteLine($"Today is {DateTime.Now:dddd, MMMM d, yyyy}");
      Console.WriteLine($"Current time: {DateTime.Now:HH:mm:ss}");
      return default;
    }
  }
}
