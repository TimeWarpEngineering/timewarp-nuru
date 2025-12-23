namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using Mediator;
using TimeWarp.Terminal;

/// <summary>
/// Set a configuration value.
/// This is an Idempotent Command (I) - mutating but safe to retry.
/// Running "config set key value" multiple times has the same effect as running once.
/// Demonstrates ITerminal injection for testable output.
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
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public ValueTask<Unit> Handle(SetConfigCommand command, CancellationToken ct)
    {
      Terminal.WriteLine($"Setting config: {command.Key} = {command.Value}");
      return default;
    }
  }
}
