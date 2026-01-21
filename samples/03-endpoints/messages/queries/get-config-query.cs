namespace Endpoints.Messages;

using TimeWarp.Nuru;
using TimeWarp.Terminal;

/// <summary>
/// Get a configuration value.
/// This is a Query (Q) - read-only, safe to retry.
/// Demonstrates ITerminal injection for testable output.
/// </summary>
[NuruRoute("get", Description = "Get a configuration value")]
public sealed class GetConfigQuery : ConfigGroupBase, IQuery<Unit>
{
  [Parameter(Description = "Configuration key")]
  public string Key { get; set; } = string.Empty;

  public sealed class Handler : IQueryHandler<GetConfigQuery, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public ValueTask<Unit> Handle(GetConfigQuery query, CancellationToken ct)
    {
      // In a real app, this would look up the value
      Terminal.WriteLine($"Config value for '{query.Key}': (not set)");
      return default;
    }
  }
}
