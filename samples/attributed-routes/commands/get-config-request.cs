namespace AttributedRoutes.Commands;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Get a configuration value.
/// This is a Query (Q) - read-only, safe to retry.
/// </summary>
[NuruRoute("config get", Description = "Get a configuration value")]
public sealed class GetConfigRequest : IQuery<Unit>
{
  [Parameter(Description = "Configuration key")]
  public string Key { get; set; } = string.Empty;

  public sealed class Handler : IQueryHandler<GetConfigRequest, Unit>
  {
    public ValueTask<Unit> Handle(GetConfigRequest request, CancellationToken ct)
    {
      // In a real app, this would look up the value
      WriteLine($"Config value for '{request.Key}': (not set)");
      return default;
    }
  }
}
