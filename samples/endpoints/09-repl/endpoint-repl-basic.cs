#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - REPL BASIC DEMO ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// Interactive REPL mode with Endpoint DSL using auto-discovered endpoints.
//
// DSL: Endpoint with .AddRepl() for interactive mode
//
// PATTERNS:
//   - CLI app that can run in REPL mode
//   - All endpoints available interactively
//   - Type 'exit' or Ctrl+C to quit
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;

// Single Build() with AddRepl() - handles both CLI and REPL modes
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .AddRepl(options =>
  {
    options.Prompt = "repl> ";
    options.WelcomeMessage = """
      Entering REPL mode...
      Available commands: greet, calc, status, help
      Type 'exit' to quit
      """;
    options.AutoStartWhenEmpty = true;  // Auto-start REPL when no args provided
  })
  .Build();

// RunAsync handles both CLI and REPL modes automatically
return await app.RunAsync(args);

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

[NuruRoute("greet", Description = "Greet someone")]
public sealed class GreetCommand : ICommand<Unit>
{
  [Parameter] public string Name { get; set; } = "";

  public sealed class Handler : ICommandHandler<GreetCommand, Unit>
  {
    public ValueTask<Unit> Handle(GreetCommand c, CancellationToken ct)
    {
      Console.WriteLine($"Hello, {c.Name}! Welcome to the REPL.");
      return default;
    }
  }
}

[NuruRoute("calc", Description = "Simple calculator")]
public sealed class CalcCommand : ICommand<double>
{
  [Parameter] public string Operation { get; set; } = "";
  [Parameter] public double X { get; set; }
  [Parameter] public double Y { get; set; }

  public sealed class Handler : ICommandHandler<CalcCommand, double>
  {
    public ValueTask<double> Handle(CalcCommand c, CancellationToken ct)
    {
      double result = c.Operation.ToLower() switch
      {
        "add" => c.X + c.Y,
        "sub" => c.X - c.Y,
        "mul" => c.X * c.Y,
        "div" => c.X / c.Y,
        _ => throw new ArgumentException($"Unknown operation: {c.Operation}")
      };

      Console.WriteLine($"Result: {result}");
      return new ValueTask<double>(result);
    }
  }
}

[NuruRoute("status", Description = "Show system status")]
public sealed class StatusCommand : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<StatusCommand, Unit>
  {
    public ValueTask<Unit> Handle(StatusCommand q, CancellationToken ct)
    {
      Console.WriteLine("System Status:");
      Console.WriteLine("  ✓ Running");
      Console.WriteLine("  ✓ Memory OK");
      Console.WriteLine($"  ✓ Time: {DateTime.Now:HH:mm:ss}");
      return default;
    }
  }
}
