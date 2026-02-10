using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("schedule", Description = "Schedule for a specific date/time")]
public sealed class ScheduleCommand : ICommand<Unit>
{
  [Parameter] public DateTime Date { get; set; }

  public sealed class Handler : ICommandHandler<ScheduleCommand, Unit>
  {
    public ValueTask<Unit> Handle(ScheduleCommand c, CancellationToken ct)
    {
      WriteLine($"Scheduled for: {c.Date:yyyy-MM-dd HH:mm:ss}");
      return default;
    }
  }
}
