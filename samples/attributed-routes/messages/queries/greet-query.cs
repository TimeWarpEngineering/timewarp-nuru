namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using TimeWarp.Terminal;
using Mediator;

/// <summary>
/// Simple greeting query with a required parameter.
/// This is a Query (Q) - read-only, safe to retry.
/// Demonstrates ITerminal injection for testable output.
/// </summary>
[NuruRoute("greet", Description = "Greet someone by name")]
public sealed class GreetQuery : IQuery<Unit>
{
  [Parameter(Description = "Name of the person to greet")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : IQueryHandler<GreetQuery, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public ValueTask<Unit> Handle(GreetQuery query, CancellationToken ct)
    {
      Terminal.WriteLine($"Hello, {query.Name}!");
      return default;
    }
  }
}
