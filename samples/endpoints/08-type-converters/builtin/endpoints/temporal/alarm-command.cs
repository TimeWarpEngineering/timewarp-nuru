using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("alarm", Description = "Set an alarm time")]
public sealed class AlarmCommand : ICommand<Unit>
{
  [Parameter] public string Time { get; set; } = "09:00";

  public sealed class Handler : ICommandHandler<AlarmCommand, Unit>
  {
    public ValueTask<Unit> Handle(AlarmCommand c, CancellationToken ct)
    {
      TimeOnly time = TimeOnly.Parse(c.Time);
      WriteLine($"Alarm set for: {time:HH:mm}");
      return default;
    }
  }
}
