using TimeWarp.Nuru;
using TimeWarp.Terminal;

[NuruRoute("status", Description = "Show status with colors")]
public sealed class StatusCommand : ICommand<Unit>
{
  public sealed class Handler(ITerminal T) : ICommandHandler<StatusCommand, Unit>
  {
    public ValueTask<Unit> Handle(StatusCommand c, CancellationToken ct)
    {
      T.WriteLine($"{"✓".Green()} System Healthy".Green());
      T.WriteLine($"{"✓".Green()} Database Connected".Green());
      T.WriteLine($"{"✓".Green()} API Responsive".Green());
      return default;
    }
  }
}
