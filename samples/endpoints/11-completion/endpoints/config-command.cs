using TimeWarp.Mediator;
using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("config", Description = "Manage configuration")]
public sealed class ConfigCommand : ICommand<Unit>
{
  [Parameter(Order = 0, Description = "Action: get, set, list")]
  public string Action { get; set; } = "";

  [Parameter(Order = 1, Description = "Configuration key")]
  public string Key { get; set; } = "";

  [Parameter(Order = 2, Description = "Configuration value (for set)")]
  public string? Value { get; set; }

  public sealed class Handler : IRequestHandler<ConfigCommand, Unit>
  {
    public ValueTask<Unit> Handle(ConfigCommand c, CancellationToken ct)
    {
      WriteLine($"Config {c.Action}: {c.Key} = {c.Value ?? "(null)"}");
      return Unit.ValueTask;
    }
  }
}
