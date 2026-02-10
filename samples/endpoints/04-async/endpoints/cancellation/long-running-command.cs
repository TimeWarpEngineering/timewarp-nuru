// ═══════════════════════════════════════════════════════════════════════════════
// LONG-RUNNING COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Long-running operation with CancellationToken support.

namespace AsyncExamples.Endpoints.Cancellation;

using TimeWarp.Nuru;

[NuruRoute("long-running", Description = "Long-running operation with cancellation support")]
public sealed class LongRunningCommand : ICommand<Unit>
{
  [Parameter(Description = "Number of iterations")]
  public int Iterations { get; set; } = 10;

  public sealed class Handler : ICommandHandler<LongRunningCommand, Unit>
  {
    public async ValueTask<Unit> Handle(LongRunningCommand command, CancellationToken ct)
    {
      for (int i = 0; i < command.Iterations; i++)
      {
        ct.ThrowIfCancellationRequested();
        Console.WriteLine($"Iteration {i + 1}/{command.Iterations}");
        await Task.Delay(100, ct);
      }

      Console.WriteLine("Long-running operation complete!");
      return default;
    }
  }
}
