// ═══════════════════════════════════════════════════════════════════════════════
// FETCH COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Fetch data with retry on network errors.

namespace PipelineRetry.Endpoints;

using PipelineRetry.Behaviors;
using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("fetch", Description = "Fetch data with retry on network errors")]
public sealed class FetchCommand : ICommand<string>, IRetryable
{
  [Parameter(Description = "URL to fetch")]
  public string Url { get; set; } = string.Empty;

  public int MaxRetries => 2;

  public sealed class Handler : IRequestHandler<FetchCommand, string>
  {
    private static int FailureCount = 0;

    public ValueTask<string> Handle(FetchCommand command, CancellationToken ct)
    {
      FailureCount++;

      if (FailureCount < 2)
      {
        throw new HttpRequestException($"Network error fetching {command.Url}");
      }

      string result = $"Data from {command.Url}";
      Console.WriteLine($"✓ Fetched successfully after {FailureCount} attempts");
      FailureCount = 0;
      return ValueTask.FromResult<string>(result);
    }
  }
}
