namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Get a configuration value.
/// This is a Query (Q) - read-only, safe to retry.
/// </summary>
[NuruRoute("get", Description = "Get a configuration value")]
public sealed class GetConfigQuery : ConfigGroupBase, IQuery<Unit>
{
  [Parameter(Description = "Configuration key")]
  public string Key { get; set; } = string.Empty;

  public sealed class Handler : IQueryHandler<GetConfigQuery, Unit>
  {
    public ValueTask<Unit> Handle(GetConfigQuery query, CancellationToken ct)
    {
      // In a real app, this would look up the value
      WriteLine($"Config value for '{query.Key}': (not set)");
      return default;
    }
  }
}
