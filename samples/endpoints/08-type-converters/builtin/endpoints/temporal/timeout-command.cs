using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("timeout", Description = "Set a timeout duration")]
public sealed class TimeoutCommand : ICommand<Unit>
{
  [Parameter] public TimeSpan Duration { get; set; }

  public sealed class Handler : ICommandHandler<TimeoutCommand, Unit>
  {
    public ValueTask<Unit> Handle(TimeoutCommand c, CancellationToken ct)
    {
      WriteLine($"Timeout set to: {c.Duration.TotalSeconds} seconds");
      return default;
    }
  }
}
