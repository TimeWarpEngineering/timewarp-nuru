// ═══════════════════════════════════════════════════════════════════════════════
// FLAKY API COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Simulate flaky API call with auto-retry.

namespace PipelineRetry.Endpoints;

using PipelineRetry.Behaviors;
using TimeWarp.Nuru;

[NuruRoute("flaky-api", Description = "Simulate flaky API call with auto-retry")]
public sealed class FlakyApiCommand : ICommand<Unit>, IRetryable
{
  [Parameter(Description = "API endpoint")]
  public string Endpoint { get; set; } = string.Empty;

  public int MaxRetries => 3;

  public sealed class Handler : ICommandHandler<FlakyApiCommand, Unit>
  {
    private static int FailureCount = 0;

    public ValueTask<Unit> Handle(FlakyApiCommand command, CancellationToken ct)
    {
      FailureCount++;

      if (FailureCount < 3)
      {
        throw new TimeoutException($"API call to {command.Endpoint} timed out (attempt {FailureCount})");
      }

      Console.WriteLine($"✓ API call to {command.Endpoint} succeeded after {FailureCount} attempts");
      FailureCount = 0;
      return default;
    }
  }
}
