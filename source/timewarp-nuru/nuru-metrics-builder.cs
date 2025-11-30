namespace TimeWarp.Nuru;

/// <summary>
/// Simple IMetricsBuilder implementation that wraps the service collection.
/// </summary>
internal sealed class NuruMetricsBuilder : IMetricsBuilder
{
  public IServiceCollection Services { get; }

  public NuruMetricsBuilder(IServiceCollection services)
  {
    Services = services;
  }
}
