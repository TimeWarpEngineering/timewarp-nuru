namespace TimeWarp.Nuru;

/// <summary>
/// Provides a lightweight service provider for scenarios without full dependency injection.
/// Used when logging is configured but dependency injection is not enabled.
/// Resolves ILoggerFactory, ILogger&lt;T&gt;, and NuruCoreApp.
/// </summary>
internal sealed class LightweightServiceProvider : IServiceProvider
{
  private readonly NuruCoreApp App;
  private readonly ILoggerFactory LoggerFactory;

  public LightweightServiceProvider(NuruCoreApp app, ILoggerFactory loggerFactory)
  {
    App = app ?? throw new ArgumentNullException(nameof(app));
    LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
  }

  public object? GetService(Type serviceType)
  {
    if (serviceType == typeof(NuruCoreApp))
    {
      return App;
    }

    if (serviceType == typeof(ILoggerFactory))
    {
      return LoggerFactory;
    }

    // Handle ILogger<T> requests by creating Logger<T> instances
    if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(ILogger<>))
    {
      Type categoryType = serviceType.GetGenericArguments()[0];
      Type loggerType = typeof(Logger<>).MakeGenericType(categoryType);
      return Activator.CreateInstance(loggerType, LoggerFactory)!;
    }

    return null;
  }
}
