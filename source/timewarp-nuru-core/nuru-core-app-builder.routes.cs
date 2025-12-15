namespace TimeWarp.Nuru;

/// <summary>
/// Route registration methods for NuruCoreAppBuilder.
/// </summary>
public partial class NuruCoreAppBuilder
{
  /// <summary>
  /// Adds a default route that executes when no arguments are provided.
  /// </summary>
  public virtual RouteConfigurator MapDefault(Delegate handler, string? description = null)
  {
    return MapInternal(string.Empty, handler, description);
  }

  /// <summary>
  /// Adds REPL (Read-Eval-Print Loop) configuration options to application.
  /// This stores REPL configuration options for use when REPL mode is activated.
  /// </summary>
  /// <param name="configureOptions">Optional action to configure REPL options.</param>
  /// <returns>The builder for chaining.</returns>
  public virtual NuruCoreAppBuilder AddReplOptions(Action<ReplOptions>? configureOptions = null)
  {
    ReplOptions replOptions = new();
    configureOptions?.Invoke(replOptions);
    ReplOptions = replOptions;
    return this;
  }

  /// <summary>
  /// Adds a delegate-based route.
  /// </summary>
  public virtual RouteConfigurator Map(string pattern, Delegate handler, string? description = null)
  {
    ArgumentNullException.ThrowIfNull(pattern);
    return MapInternal(pattern, handler, description);
  }

  /// <summary>
  /// Adds a Mediator command-based route.
  /// Requires AddDependencyInjection() to be called first.
  /// </summary>
  public virtual RouteConfigurator Map<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand>(string pattern, string? description = null)
    where TCommand : IRequest, new()
  {
    return MapMediator(typeof(TCommand), pattern, description);
  }

  /// <summary>
  /// Adds a Mediator command-based route with response.
  /// Requires AddDependencyInjection() to be called first.
  /// </summary>
  public virtual RouteConfigurator Map<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand, TResponse>(string pattern, string? description = null)
    where TCommand : IRequest<TResponse>, new()
  {
    return MapMediator(typeof(TCommand), pattern, description);
  }

  /// <summary>
  /// Adds multiple route patterns that invoke the same handler.
  /// Useful for command aliases (e.g., "exit", "quit", "q").
  /// The first pattern is considered the primary for help display.
  /// </summary>
  /// <param name="patterns">Array of route patterns (first is primary).</param>
  /// <param name="handler">The handler to invoke for all patterns.</param>
  /// <param name="description">Description shown in help.</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// builder.Map(
  ///     ["exit", "quit", "q"],
  ///     () => Environment.Exit(0),
  ///     "Exit the application");
  /// </code>
  /// </example>
  public virtual NuruCoreAppBuilder MapMultiple(string[] patterns, Delegate handler, string? description = null)
  {
    ArgumentNullException.ThrowIfNull(patterns);
    ArgumentNullException.ThrowIfNull(handler);

    if (patterns.Length == 0)
      throw new ArgumentException("At least one pattern required", nameof(patterns));

    foreach (string pattern in patterns)
    {
      MapInternal(pattern, handler, description);
    }

    return this;
  }

  /// <summary>
  /// Adds multiple route patterns for a Mediator command.
  /// Requires AddDependencyInjection() to be called first.
  /// The first pattern is considered the primary for help display.
  /// </summary>
  /// <param name="patterns">Array of route patterns (first is primary).</param>
  /// <param name="description">Description shown in help.</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// builder.Map&lt;ExitCommand&gt;(
  ///     ["exit", "quit", "q"],
  ///     "Exit the application");
  /// </code>
  /// </example>
  public virtual NuruCoreAppBuilder MapMultiple<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand>(string[] patterns, string? description = null)
    where TCommand : IRequest, new()
  {
    ArgumentNullException.ThrowIfNull(patterns);

    if (patterns.Length == 0)
      throw new ArgumentException("At least one pattern required", nameof(patterns));

    foreach (string pattern in patterns)
    {
      MapMediator(typeof(TCommand), pattern, description);
    }

    return this;
  }

  /// <summary>
  /// Adds multiple route patterns for a Mediator command with response.
  /// Requires AddDependencyInjection() to be called first.
  /// The first pattern is considered the primary for help display.
  /// </summary>
  /// <param name="patterns">Array of route patterns (first is primary).</param>
  /// <param name="description">Description shown in help.</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// builder.Map&lt;GreetCommand, string&gt;(
  ///     ["greet", "hello", "hi"],
  ///     "Greet someone");
  /// </code>
  /// </example>
  public virtual NuruCoreAppBuilder MapMultiple<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand, TResponse>(string[] patterns, string? description = null)
    where TCommand : IRequest<TResponse>, new()
  {
    ArgumentNullException.ThrowIfNull(patterns);

    if (patterns.Length == 0)
      throw new ArgumentException("At least one pattern required", nameof(patterns));

    foreach (string pattern in patterns)
    {
      MapMediator(typeof(TCommand), pattern, description);
    }

    return this;
  }

  /// <summary>
  /// Registers a custom type converter for parameter conversion.
  /// </summary>
  /// <param name="converter">The type converter to register.</param>
  public virtual NuruCoreAppBuilder AddTypeConverter(IRouteTypeConverter converter)
  {
    ArgumentNullException.ThrowIfNull(converter);
    TypeConverterRegistry.RegisterConverter(converter);
    return this;
  }

  private RouteConfigurator MapMediator(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    Type commandType,
    string pattern,
    string? description)
  {
    if (ServiceCollection is null)
    {
      throw new InvalidOperationException("Dependency injection must be added before using Mediator commands. Call AddDependencyInjection() first.");
    }

    Endpoint endpoint = new()
    {
      RoutePattern = pattern,
      CompiledRoute = PatternParser.Parse(pattern, LoggerFactory),
      Description = description,
      CommandType = commandType
    };

    EndpointCollection.Add(endpoint);
    return new RouteConfigurator(this, endpoint);
  }

  private RouteConfigurator MapInternal(string pattern, Delegate handler, string? description)
  {
    ArgumentNullException.ThrowIfNull(handler);

    // Log route registration if logger is available
    if (LoggerFactory is not null)
    {
      ILogger<NuruCoreAppBuilder> logger = LoggerFactory.CreateLogger<NuruCoreAppBuilder>();
      if (EndpointCollection.Count == 0)
      {
        ParsingLoggerMessages.StartingRouteRegistration(logger, null);
      }

      ParsingLoggerMessages.RegisteringRoute(logger, pattern, null);
    }

    Endpoint endpoint = new()
    {
      RoutePattern = pattern,
      CompiledRoute = PatternParser.Parse(pattern, LoggerFactory),
      Handler = handler,
      Method = handler.Method,
      Description = description
    };

    EndpointCollection.Add(endpoint);
    return new RouteConfigurator(this, endpoint);
  }
}
