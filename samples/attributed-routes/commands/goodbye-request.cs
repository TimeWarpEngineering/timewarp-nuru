namespace AttributedRoutes.Commands;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Goodbye request with multiple aliases.
/// Note: We use "goodbye" instead of "exit" because the REPL already registers
/// built-in exit/quit/q commands. This demonstrates [NuruRouteAlias] without conflicts.
/// </summary>
[NuruRoute("goodbye", Description = "Say goodbye and exit")]
[NuruRouteAlias("bye", "cya")]
public sealed class GoodbyeRequest : IRequest
{
  public sealed class Handler : IRequestHandler<GoodbyeRequest>
  {
    public ValueTask<Unit> Handle(GoodbyeRequest request, CancellationToken ct)
    {
      WriteLine("Goodbye! Thanks for using attributed routes.");
      Environment.Exit(0);
      return default;
    }
  }
}
