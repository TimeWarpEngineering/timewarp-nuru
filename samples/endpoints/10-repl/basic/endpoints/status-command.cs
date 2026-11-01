using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("status", Description = "Show system status")]
public sealed class StatusCommand : IQuery<Unit>
{
  public sealed class Handler : IRequestHandler<StatusCommand, Unit>
  {
    public ValueTask<Unit> Handle(StatusCommand q, CancellationToken ct)
    {
      Console.WriteLine("System Status:");
      Console.WriteLine("  ✓ Running");
      Console.WriteLine("  ✓ Memory OK");
      Console.WriteLine($"  ✓ Time: {DateTime.Now:HH:mm:ss}");
      return Unit.ValueTask;
    }
  }
}
