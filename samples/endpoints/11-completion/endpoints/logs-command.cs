using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("logs", Description = "View and manage logs")]
public sealed class LogsCommand : ICommand<Unit>
{
  [Parameter(Description = "Action: show, tail, clear")]
  public string Action { get; set; } = "";

  [Option("lines", "n", Description = "Number of lines to show")]
  public int Lines { get; set; } = 50;

  [Option("follow", "f", Description = "Follow log output")]
  public bool Follow { get; set; }

  public sealed class Handler : ICommandHandler<LogsCommand, Unit>
  {
    public ValueTask<Unit> Handle(LogsCommand c, CancellationToken ct)
    {
      WriteLine($"Logs {c.Action}");
      WriteLine($"  Lines: {c.Lines}");
      WriteLine($"  Follow: {c.Follow}");
      return default;
    }
  }
}
