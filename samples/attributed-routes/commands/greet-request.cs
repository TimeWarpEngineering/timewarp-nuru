namespace AttributedRoutes.Commands;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Simple greeting request with a required parameter.
/// This is a Query (Q) - read-only, safe to retry.
/// </summary>
[NuruRoute("greet", Description = "Greet someone by name")]
public sealed class GreetRequest : IQuery<Unit>
{
  [Parameter(Description = "Name of the person to greet")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : IQueryHandler<GreetRequest, Unit>
  {
    public ValueTask<Unit> Handle(GreetRequest request, CancellationToken ct)
    {
      WriteLine($"Hello, {request.Name}!");
      return default;
    }
  }
}
