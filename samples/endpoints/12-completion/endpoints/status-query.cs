using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("status", Description = "Show system status")]
public sealed class StatusQuery : IQuery<Unit>
{
  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

  public sealed class Handler : IQueryHandler<StatusQuery, Unit>
  {
    public ValueTask<Unit> Handle(StatusQuery q, CancellationToken ct)
    {
      WriteLine("System Status: OK");
      if (q.Verbose)
      {
        WriteLine("  CPU: 45%");
        WriteLine("  Memory: 2.1GB");
        WriteLine("  Disk: 78%");
      }
      return default;
    }
  }
}
