// ═══════════════════════════════════════════════════════════════════════════════
// API COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Simulate API call with telemetry.

namespace PipelineTelemetry.Endpoints;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("api", Description = "Simulate API call with telemetry")]
public sealed class ApiCommand : ICommand<Unit>
{
  [Parameter(Description = "API endpoint")]
  public string Endpoint { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<ApiCommand, Unit>
  {
    public async Task<Unit> Handle(ApiCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Calling API: {command.Endpoint}");
      await Task.Delay(100, ct);
      Console.WriteLine("API call complete");
      return default;
    }
  }
}
