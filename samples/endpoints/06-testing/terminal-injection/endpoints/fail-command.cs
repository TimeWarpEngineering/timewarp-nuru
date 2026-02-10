using TimeWarp.Nuru;
using TimeWarp.Terminal;

[NuruRoute("fail", Description = "Simulate a failure")]
public sealed class FailCommand : ICommand<Unit>
{
  public sealed class Handler(ITerminal Terminal) : ICommandHandler<FailCommand, Unit>
  {
    public ValueTask<Unit> Handle(FailCommand c, CancellationToken ct)
    {
      Terminal.WriteErrorLine("ERROR".Red().Bold());
      Terminal.WriteErrorLine("Operation failed!".Red());
      throw new InvalidOperationException("Intentional failure");
    }
  }
}
