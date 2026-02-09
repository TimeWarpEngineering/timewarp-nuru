// ═══════════════════════════════════════════════════════════════════════════════
// DELAY COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Basic async delay with Task.Delay.

namespace AsyncExamples.Endpoints.Basic;

using TimeWarp.Nuru;

[NuruRoute("delay", Description = "Async delay command with milliseconds")]
public sealed class DelayCommand : ICommand<Unit>
{
  [Parameter(Description = "Milliseconds to delay")]
  public int Ms { get; set; }

  public sealed class Handler : ICommandHandler<DelayCommand, Unit>
  {
    public async ValueTask<Unit> Handle(DelayCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Starting {command.Ms}ms delay...");
      await Task.Delay(command.Ms, ct);
      Console.WriteLine("Delay complete!");
      return default;
    }
  }
}
