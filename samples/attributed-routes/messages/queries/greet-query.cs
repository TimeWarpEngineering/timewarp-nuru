namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Simple greeting query with a required parameter.
/// This is a Query (Q) - read-only, safe to retry.
/// </summary>
[NuruRoute("greet", Description = "Greet someone by name")]
public sealed class GreetQuery : IQuery<Unit>
{
  [Parameter(Description = "Name of the person to greet")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : IQueryHandler<GreetQuery, Unit>
  {
    public ValueTask<Unit> Handle(GreetQuery query, CancellationToken ct)
    {
      WriteLine($"Hello, {query.Name}!");
      return default;
    }
  }
}
