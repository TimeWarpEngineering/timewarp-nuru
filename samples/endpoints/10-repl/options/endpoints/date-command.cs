using TimeWarp.Nuru;

[NuruRoute("date", Description = "Show current date and time")]
public sealed class DateCommand : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<DateCommand, Unit>
  {
    public ValueTask<Unit> Handle(DateCommand q, CancellationToken ct)
    {
      Console.WriteLine($"Today is {DateTime.Now:dddd, MMMM d, yyyy}");
      Console.WriteLine($"Current time: {DateTime.Now:HH:mm:ss}");
      return default;
    }
  }
}
