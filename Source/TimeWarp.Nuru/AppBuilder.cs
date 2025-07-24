using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Mediator;
using TimeWarp.Nuru.Endpoints;
using TimeWarp.Nuru.Parsing;

namespace TimeWarp.Nuru;

/// <summary>
/// Builder for configuring a TimeWarp.Console application.
/// </summary>
public class AppBuilder
{
  private readonly ServiceCollection _services = new();
  private readonly EndpointCollection _endpoints = new();

  public IServiceCollection Services => _services;

  public AppBuilder()
  {
    // Add default services
    _services.AddNuru();
    _services.AddSingleton(_endpoints);
  }

  /// <summary>
  /// Adds a delegate-based route.
  /// </summary>
  public AppBuilder AddRoute(string pattern, Delegate handler, string? description = null)
  {
    var endpoint = new RouteEndpoint
    {
      RoutePattern = pattern,
      ParsedRoute = RoutePatternParser.Parse(pattern),
      Handler = handler,
      Method = handler.Method,
      Description = description
    };

    _endpoints.Add(endpoint);
    return this;
  }

  /// <summary>
  /// Adds a Mediator command-based route.
  /// </summary>
  public AppBuilder AddRoute<TCommand>(string pattern, string? description = null)
      where TCommand : IRequest, new()
  {
    var endpoint = new RouteEndpoint
    {
      RoutePattern = pattern,
      ParsedRoute = RoutePatternParser.Parse(pattern),
      Handler = new Action(() => { }), // Placeholder
      Method = typeof(Action).GetMethod("Invoke")!,
      Description = description,
      CommandType = typeof(TCommand)
    };

    _endpoints.Add(endpoint);
    return this;
  }

  /// <summary>
  /// Adds a Mediator command-based route with response.
  /// </summary>
  public AppBuilder AddRoute<TCommand, TResponse>(string pattern, string? description = null)
      where TCommand : IRequest<TResponse>, new()
  {
    var endpoint = new RouteEndpoint
    {
      RoutePattern = pattern,
      ParsedRoute = RoutePatternParser.Parse(pattern),
      Handler = new Action(() => { }), // Placeholder
      Method = typeof(Action).GetMethod("Invoke")!,
      Description = description,
      CommandType = typeof(TCommand)
    };

    _endpoints.Add(endpoint);
    return this;
  }

  /// <summary>
  /// Builds the service provider and returns a runnable app.
  /// </summary>
  public NuruApp Build()
  {
    ServiceProvider serviceProvider = _services.BuildServiceProvider();
    return new NuruApp(serviceProvider);
  }
}

/// <summary>
/// A built TimeWarp.Console application.
/// </summary>
public class NuruApp
{
  private readonly IServiceProvider _serviceProvider;

  public IServiceProvider Services => _serviceProvider;

  public NuruApp(IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider;
  }

  public async Task<int> RunAsync(string[] args)
  {
    NuruCli cli = _serviceProvider.GetRequiredService<NuruCli>();
    return await cli.RunAsync(args).ConfigureAwait(false);
  }
}
