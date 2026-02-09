#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - REPL DUAL MODE (CLI + Interactive) ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// CLI app that works both as command-line tool and interactive REPL.
//
// DSL: Endpoint with automatic mode detection
//
// USAGE:
//   ./endpoint-repl-dual-mode.cs greet World     # CLI mode
//   ./endpoint-repl-dual-mode.cs --interactive  # REPL mode
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

// Detect mode based on arguments
bool isInteractive = args.Length == 0
  || args.Contains("--interactive")
  || args.Contains("-i");

if (isInteractive)
{
  // REPL mode
  Console.WriteLine("╔════════════════════════════════════╗");
  Console.WriteLine("║  Nuru Dual-Mode Demo               ║");
  Console.WriteLine("║  Type commands or 'exit' to quit   ║");
  Console.WriteLine("╚════════════════════════════════════╝\n");

  ReplOptions replOptions = new()
  {
    Prompt = "dual> ",
    WelcomeMessage = null, // We already showed our custom banner
    HistoryFilePath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".nuru_dual_history"
    )
  };

  return await app.RunAsReplAsync(replOptions);
}
else
{
  // CLI mode - pass through to normal execution
  return await app.RunAsync(args);
}

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
      Console.WriteLine($"Hello, {c.Name}!");
      return default;
    }
  }
}

[NuruRoute("add", Description = "Add two numbers")]
public sealed class AddCommand : IQuery<double>
{
  [Parameter] public double X { get; set; }
  [Parameter] public double Y { get; set; }

  public sealed class Handler : IQueryHandler<AddCommand, double>
  {
    public ValueTask<double> Handle(AddCommand q, CancellationToken ct) =>
      new ValueTask<double>(q.X + q.Y);
  }
}

[NuruRoute("list", Description = "List items")]
public sealed class ListCommand : IQuery<string[]>
{
  [Parameter(IsOptional = true)] public int? Count { get; set; }

  public sealed class Handler : IQueryHandler<ListCommand, string[]>
  {
    public ValueTask<string[]> Handle(ListCommand q, CancellationToken ct)
    {
      int count = q.Count ?? 5;
      string[] items = Enumerable.Range(1, count)
        .Select(i => $"Item {i}")
        .ToArray();

      foreach (string item in items)
      {
        Console.WriteLine($"  {item}");
      }

      return new ValueTask<string[]>(items);
    }
  }
}
