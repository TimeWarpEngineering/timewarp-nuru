using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using TimeWarp.Terminal;
using static System.Console;

[NuruRoute("endpoints", Description = "List configured endpoints")]
public sealed class ListEndpointsQuery : IQuery<Unit>
{
  public sealed class Handler(IOptions<AppConfiguration> config) : IQueryHandler<ListEndpointsQuery, Unit>
  {
    public ValueTask<Unit> Handle(ListEndpointsQuery query, CancellationToken ct)
    {
      WriteLine("Configured Endpoints:");
      foreach (KeyValuePair<string, EndpointConfig> ep in config.Value.Endpoints)
      {
        WriteLine($"  {ep.Key,-10} -> {ep.Value.Url,-30} (timeout: {ep.Value.Timeout}s)");
      }
      return default;
    }
  }
}
