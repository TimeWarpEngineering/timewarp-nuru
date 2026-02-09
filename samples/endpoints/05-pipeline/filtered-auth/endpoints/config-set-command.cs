// ═══════════════════════════════════════════════════════════════════════════════
// CONFIG SET COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Set configuration value (admin only, requires IRequireAuthorization).

namespace PipelineFilteredAuth.Endpoints;

using PipelineFilteredAuth.Behaviors;
using TimeWarp.Nuru;

[NuruRoute("config-set", Description = "Set configuration value (admin only)")]
public sealed class ConfigSetCommand : ICommand<Unit>, IRequireAuthorization
{
  [Parameter(Description = "Configuration key")]
  public string Key { get; set; } = string.Empty;

  [Parameter(Description = "Configuration value")]
  public string Value { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<ConfigSetCommand, Unit>
  {
    public ValueTask<Unit> Handle(ConfigSetCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Setting {command.Key} = {command.Value}");
      return default;
    }
  }
}
