// ═══════════════════════════════════════════════════════════════════════════════
// ADMIN CONFIG COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Set config (admin only, requires IRequireAuthorization).

namespace PipelineCombined.Endpoints;

using PipelineCombined.Behaviors;
using TimeWarp.Nuru;

[NuruRoute("admin-config", Description = "Set config (admin only)")]
public sealed class AdminConfigCommand : ICommand<Unit>, IRequireAuthorization
{
  [Parameter] public string Key { get; set; } = "";
  [Parameter] public string Value { get; set; } = "";

  public sealed class Handler : ICommandHandler<AdminConfigCommand, Unit>
  {
    public ValueTask<Unit> Handle(AdminConfigCommand c, CancellationToken ct)
    {
      Console.WriteLine($"Set {c.Key} = {c.Value}");
      return default;
    }
  }
}
