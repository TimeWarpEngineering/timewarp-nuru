// ═══════════════════════════════════════════════════════════════════════════════
// API COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Call API with retry on failure.

namespace PipelineCombined.Endpoints;

using PipelineCombined.Behaviors;
using TimeWarp.Nuru;

[NuruRoute("api", Description = "Call API (retries on failure)")]
public sealed class ApiCommand : ICommand<string>, IRetryable
{
  [Parameter] public string Endpoint { get; set; } = "";
  public int MaxRetries => 2;

  public sealed class Handler : ICommandHandler<ApiCommand, string>
  {
    private static int Fails = 0;
    public ValueTask<string> Handle(ApiCommand c, CancellationToken ct)
    {
      if (++Fails < 2) throw new TimeoutException("API timeout");
      Fails = 0;
      return new ValueTask<string>($"Response from {c.Endpoint}");
    }
  }
}
