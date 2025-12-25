
namespace TimeWarp.Nuru;

/// <summary>
/// Extension methods for configuring TimeWarp.Console.
/// </summary>
public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Adds TimeWarp.Console services to the service collection.
  /// </summary>
  public static IServiceCollection AddNuru(this IServiceCollection services)
  {
    // Register the endpoint collection as a singleton
    services.TryAddSingleton<EndpointCollection>();
    services.TryAddSingleton<IEndpointCollectionBuilder, DefaultEndpointCollectionBuilder>();

    // Register type conversion
    services.TryAddSingleton<ITypeConverterRegistry, TypeConverterRegistry>();

    // Register mediator executor for Mediator integration
    services.TryAddSingleton<MediatorExecutor>();

    // Register route execution context as scoped so each request gets fresh context
    // This follows Jimmy Bogard's recommendation for sharing context in pipelines
    services.TryAddScoped<RouteExecutionContext>();

    // Register session context as singleton to track REPL vs CLI execution context
    // Must be singleton (not scoped) because delegate handlers are resolved from root ServiceProvider
    services.TryAddSingleton<SessionContext>();

    return services;
  }
}
