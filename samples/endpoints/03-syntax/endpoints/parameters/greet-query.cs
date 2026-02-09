// ═══════════════════════════════════════════════════════════════════════════════
// GREET QUERY
// ═══════════════════════════════════════════════════════════════════════════════
// Greet someone by name - demonstrates basic parameter usage.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("greet", Description = "Greet someone by name")]
public sealed class GreetQuery : IQuery<Unit>
{
  [Parameter(Description = "Name of the person to greet")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : IQueryHandler<GreetQuery, Unit>
  {
    public ValueTask<Unit> Handle(GreetQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Hello {query.Name}");
      return default;
    }
  }
}
