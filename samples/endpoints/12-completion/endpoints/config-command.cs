using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("config", Description = "Manage configuration")]
public sealed class ConfigCommand : ICommand<Unit>
{
  [Parameter(Description = "Action: get, set, list")]
  public string Action { get; set; } = "";

  [Parameter(Description = "Configuration key")]
  public string Key { get; set; } = "";

  [Parameter(Description = "Configuration value (for set)")]
  public string? Value { get; set; }

  public sealed class Handler : ICommandHandler<ConfigCommand, Unit>
  {
    public ValueTask<Unit> Handle(ConfigCommand c, CancellationToken ct)
    {
      WriteLine($"Config {c.Action}: {c.Key} = {c.Value ?? "(null)"}");
      return default;
    }
  }
}
