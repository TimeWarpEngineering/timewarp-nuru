namespace AttributedRoutes.Commands;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Simple greeting request with a required parameter.
/// </summary>
[NuruRoute("greet", Description = "Greet someone by name")]
public sealed class GreetRequest : IRequest
{
  [Parameter(Description = "Name of the person to greet")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : IRequestHandler<GreetRequest>
  {
    public ValueTask<Unit> Handle(GreetRequest request, CancellationToken ct)
    {
      WriteLine($"Hello, {request.Name}!");
      return default;
    }
  }
}
