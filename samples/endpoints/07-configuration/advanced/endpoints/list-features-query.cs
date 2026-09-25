using Microsoft.Extensions.Options;
using TimeWarp.Mediator;
using TimeWarp.Nuru;
using TimeWarp.Terminal;
using static System.Console;

[NuruRoute("features", Description = "List feature flags")]
public sealed class ListFeaturesQuery : IQuery<Unit>
{
  public sealed class Handler(IOptions<AppConfiguration> config) : IQueryHandler<ListFeaturesQuery, Unit>
  {
    public Task<Unit> Handle(ListFeaturesQuery query, CancellationToken ct)
    {
      WriteLine("Feature Flags:");
      foreach (FeatureConfig f in config.Value.Features)
      {
        string status = f.Enabled ? "ENABLED ".Green() : "DISABLED".Red();
        WriteLine($"  {f.Name,-15} {status}");
      }
      return Unit.Task;
    }
  }
}
