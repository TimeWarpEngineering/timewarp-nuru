namespace AttributedRoutes.Commands;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Set a configuration value.
/// This is an Idempotent Command (I) - mutating but safe to retry.
/// Running "config set key value" multiple times has the same effect as running once.
/// </summary>
[NuruRoute("config set", Description = "Set a configuration value")]
public sealed class SetConfigRequest : ICommand<Unit>, IIdempotent
{
  [Parameter(Description = "Configuration key")]
  public string Key { get; set; } = string.Empty;

  [Parameter(Description = "Configuration value")]
  public string Value { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<SetConfigRequest, Unit>
  {
    public ValueTask<Unit> Handle(SetConfigRequest request, CancellationToken ct)
    {
      WriteLine($"Setting config: {request.Key} = {request.Value}");
      return default;
    }
  }
}
