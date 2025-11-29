namespace TimeWarp.Nuru;

/// <summary>
/// Simple ILoggingBuilder implementation that wraps the service collection.
/// </summary>
internal sealed class NuruLoggingBuilder : ILoggingBuilder
{
  public NuruLoggingBuilder(IServiceCollection services)
  {
    Services = services;
  }

  public IServiceCollection Services { get; }
}
