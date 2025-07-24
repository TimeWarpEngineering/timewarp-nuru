using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TimeWarp.Nuru.CommandResolver;
using TimeWarp.Nuru.Endpoints;
using TimeWarp.Nuru.TypeConversion;

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

    // Register command executor for Mediator integration
    services.TryAddSingleton<CommandExecutor>();

    // Register the CLI
    services.TryAddSingleton<NuruCli>();

    return services;
  }
}
