namespace AttributedRoutes.Commands;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Goodbye request with multiple aliases.
/// This is a Command (C) - has side effect (exits the process).
/// Note: We use "goodbye" instead of "exit" because the REPL already registers
/// built-in exit/quit/q commands. This demonstrates [NuruRouteAlias] without conflicts.
/// </summary>
[NuruRoute("goodbye", Description = "Say goodbye and exit")]
[NuruRouteAlias("bye", "cya")]
public sealed class GoodbyeRequest : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<GoodbyeRequest, Unit>
  {
    public ValueTask<Unit> Handle(GoodbyeRequest request, CancellationToken ct)
    {
      WriteLine("Goodbye! Thanks for using attributed routes.");
      Environment.Exit(0);
      return default;
    }
  }
}
