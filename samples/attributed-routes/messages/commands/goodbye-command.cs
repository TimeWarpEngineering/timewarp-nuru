namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Say goodbye and exit.
/// This is a Command (C) - has side effect (exits the process).
/// Note: We use "goodbye" instead of "exit" because the REPL already registers
/// built-in exit/quit/q commands. This demonstrates [NuruRouteAlias] without conflicts.
/// </summary>
[NuruRoute("goodbye", Description = "Say goodbye and exit")]
[NuruRouteAlias("bye", "cya")]
public sealed class GoodbyeCommand : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<GoodbyeCommand, Unit>
  {
    public ValueTask<Unit> Handle(GoodbyeCommand command, CancellationToken ct)
    {
      WriteLine("Goodbye! Thanks for using attributed routes.");
      Environment.Exit(0);
      return default;
    }
  }
}
