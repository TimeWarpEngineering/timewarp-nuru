namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Set a configuration value.
/// This is an Idempotent Command (I) - mutating but safe to retry.
/// Running "config set key value" multiple times has the same effect as running once.
/// </summary>
[NuruRoute("set", Description = "Set a configuration value")]
public sealed class SetConfigCommand : ConfigGroupBase, ICommand<Unit>, IIdempotent
{
  [Parameter(Order = 0, Description = "Configuration key")]
  public string Key { get; set; } = string.Empty;

  [Parameter(Order = 1, Description = "Configuration value")]
  public string Value { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<SetConfigCommand, Unit>
  {
    public ValueTask<Unit> Handle(SetConfigCommand command, CancellationToken ct)
    {
      WriteLine($"Setting config: {command.Key} = {command.Value}");
      return default;
    }
  }
}
