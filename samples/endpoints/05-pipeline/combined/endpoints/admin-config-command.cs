// ═══════════════════════════════════════════════════════════════════════════════
// ADMIN CONFIG COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Set config (admin only, requires IRequireAuthorization).

namespace PipelineCombined.Endpoints;

using PipelineCombined.Behaviors;
using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("admin-config", Description = "Set config (admin only)")]
public sealed class AdminConfigCommand : ICommand<Unit>, IRequireAuthorization
{
  [Parameter(Order = 0)] public string Key { get; set; } = "";
  [Parameter(Order = 1)] public string Value { get; set; } = "";

  public sealed class Handler : IRequestHandler<AdminConfigCommand, Unit>
  {
    public ValueTask<Unit> Handle(AdminConfigCommand c, CancellationToken ct)
    {
      Console.WriteLine($"Set {c.Key} = {c.Value}");
      return Unit.ValueTask;
    }
  }
}
