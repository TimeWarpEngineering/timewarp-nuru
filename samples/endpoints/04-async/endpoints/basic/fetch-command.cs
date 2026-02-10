// ═══════════════════════════════════════════════════════════════════════════════
// FETCH COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Simulate async HTTP fetch operation.

namespace AsyncExamples.Endpoints.Basic;

using TimeWarp.Nuru;

[NuruRoute("fetch", Description = "Simulate async HTTP fetch")]
public sealed class FetchCommand : ICommand<string>
{
  [Parameter(Description = "URL to fetch")]
  public string Url { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<FetchCommand, string>
  {
    public async ValueTask<string> Handle(FetchCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Fetching {command.Url}...");
      await Task.Delay(100, ct); // Simulate network delay
      return $"Content from {command.Url}";
    }
  }
}
