namespace TimeWarp.Nuru;

/// <summary>
/// Lightweight builder for CLI applications without dependency injection.
/// Perfect for simple CLI tools that don't need DI or Mediator support.
/// </summary>
public class DirectAppBuilder
{
  private readonly EndpointCollection EndpointCollection = [];
  private readonly TypeConverterRegistry TypeConverterRegistry = new();

  /// <summary>
  /// Adds a delegate-based route without DI support.
  /// </summary>
  public DirectAppBuilder AddRoute
  (
    string pattern,
    Delegate handler,
    string? description = null
  )
  {
    ArgumentNullException.ThrowIfNull(pattern);
    ArgumentNullException.ThrowIfNull(handler);

    RouteEndpoint endpoint = new()
    {
      RoutePattern = pattern,
      ParsedRoute = RoutePatternParser.Parse(pattern),
      Handler = handler,
      Method = handler.Method,
      Description = description
    };

    EndpointCollection.Add(endpoint);
    return this;
  }

  /// <summary>
  /// Builds and returns a runnable DirectApp without DI.
  /// </summary>
  public DirectApp Build()
  {
    EndpointCollection.Sort();
    return new DirectApp
    (
      EndpointCollection,
      TypeConverterRegistry
    );
  }
}