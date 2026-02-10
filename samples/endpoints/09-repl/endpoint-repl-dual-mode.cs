#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - REPL DUAL MODE (CLI + Interactive) ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// CLI app that works both as command-line tool and interactive REPL.
//
// DSL: Endpoint with automatic mode detection via AutoStartWhenEmpty
//
// USAGE:
//   ./endpoint-repl-dual-mode.cs greet World     # CLI mode
//   ./endpoint-repl-dual-mode.cs                 # REPL mode (no args)
//   ./endpoint-repl-dual-mode.cs --interactive   # REPL mode (explicit flag)
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;

// Single Build() with AddRepl() - handles both CLI and REPL modes automatically
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .AddRepl(options =>
  {
    options.Prompt = "dual> ";
    options.WelcomeMessage = """
      ╔════════════════════════════════════╗
      ║  Nuru Dual-Mode Demo               ║
      ║  Type commands or 'exit' to quit   ║
      ╚════════════════════════════════════╝
      """;
    options.PersistHistory = true;
    options.HistoryFilePath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".nuru_dual_history"
    );
    options.AutoStartWhenEmpty = true;  // Auto-start REPL when no args provided
  })
  .Build();

// RunAsync automatically handles mode detection:
// - With args: runs as CLI
// - Without args: starts REPL (due to AutoStartWhenEmpty)
// - With --interactive: starts REPL
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
  // Use nullable type for optional parameter (no IsOptional attribute needed)
  [Parameter] public int? Count { get; set; }

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
