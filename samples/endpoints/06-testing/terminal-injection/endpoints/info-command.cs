using TimeWarp.Mediator;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

[NuruRoute("info", Description = "Show info with styling")]
public sealed class InfoCommand : ICommand<Unit>
{
  public sealed class Handler(ITerminal Terminal) : ICommandHandler<InfoCommand, Unit>
  {
    public Task<Unit> Handle(InfoCommand c, CancellationToken ct)
    {
      Terminal.WriteLine("INFO".Blue().Bold());
      Terminal.WriteLine("This is an informational message.".Blue());
      return Unit.Task;
    }
  }
}
