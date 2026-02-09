#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - REPL BASIC DEMO ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// Interactive REPL mode with Endpoint DSL using auto-discovered endpoints.
//
// DSL: Endpoint with .RunAsRepl() for interactive mode
//
// PATTERNS:
//   - CLI app that can run in REPL mode
//   - All endpoints available interactively
//   - Type 'exit' or Ctrl+C to quit
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;

NuruAppBuilder builder = NuruApp.CreateBuilder();

// Check if --interactive flag is passed
if (args.Contains("--interactive") || args.Contains("-i"))
{
  builder.AddRepl();
  NuruApp app = builder.DiscoverEndpoints().Build();
  
  Console.WriteLine("Entering REPL mode...");
  Console.WriteLine("Available commands: greet, calc, status, help");
  Console.WriteLine("Type 'exit' to quit\n");
  await app.RunReplAsync();
  return 0;
}

// Otherwise run as standard CLI
NuruApp cliApp = builder.DiscoverEndpoints().Build();
return await cliApp.RunAsync(args);

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
