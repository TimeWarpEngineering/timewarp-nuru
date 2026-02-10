using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("daily", Description = "Daily report for a specific date")]
public sealed class DailyCommand : ICommand<Unit>
{
  [Parameter] public string Date { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

  public sealed class Handler : ICommandHandler<DailyCommand, Unit>
  {
    public ValueTask<Unit> Handle(DailyCommand c, CancellationToken ct)
    {
      DateOnly date = DateOnly.Parse(c.Date);
      WriteLine($"Daily report for: {date:yyyy-MM-dd}");
      return default;
    }
  }
}
