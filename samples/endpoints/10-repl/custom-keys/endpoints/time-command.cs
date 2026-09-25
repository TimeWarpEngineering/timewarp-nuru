using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("time", Description = "Show current time")]
public sealed class TimeCommand : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<TimeCommand, Unit>
  {
    public Task<Unit> Handle(TimeCommand q, CancellationToken ct)
    {
      Console.WriteLine($"Current time: {DateTime.Now:HH:mm:ss}");
      return Unit.Task;
    }
  }
}
