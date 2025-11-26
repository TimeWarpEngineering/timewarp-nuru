namespace TimeWarp.Nuru;

/// <summary>
/// Route registration methods for NuruAppBuilder.
/// </summary>
public partial class NuruAppBuilder
{
  #region Map Methods (ASP.NET Core style aliases)

  /// <summary>
  /// Maps a route pattern to a delegate handler.
  /// This is an alias for <see cref="AddRoute(string, Delegate, string?)"/> that mirrors ASP.NET Core's Map pattern.
  /// </summary>
  /// <param name="pattern">The route pattern (e.g., "greet {name}").</param>
  /// <param name="handler">The delegate to execute when the route matches.</param>
  /// <param name="description">Optional description for help text.</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// var builder = NuruApp.CreateBuilder(args);
  /// builder.Map("greet {name}", (string name) => $"Hello, {name}!");
  /// builder.Map("status", () => "OK");
  /// await builder.Build().RunAsync(args);
  /// </code>
  /// </example>
  public NuruAppBuilder Map(string pattern, Delegate handler, string? description = null)
    => AddRoute(pattern, handler, description);

  /// <summary>
  /// Maps a route pattern to a Mediator command.
  /// This is an alias for <see cref="AddRoute{TCommand}(string, string?)"/> that mirrors ASP.NET Core's Map pattern.
  /// Requires AddDependencyInjection() to be called first.
  /// </summary>
  /// <typeparam name="TCommand">The command type that implements <see cref="IRequest"/>.</typeparam>
  /// <param name="pattern">The route pattern (e.g., "deploy {env}").</param>
  /// <param name="description">Optional description for help text.</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// var builder = NuruApp.CreateBuilder(args);
  /// builder.Map&lt;DeployCommand&gt;("deploy {env}");
  /// await builder.Build().RunAsync(args);
  /// </code>
  /// </example>
  public NuruAppBuilder Map<TCommand>(string pattern, string? description = null)
    where TCommand : IRequest, new()
    => AddRoute<TCommand>(pattern, description);

  /// <summary>
  /// Maps a route pattern to a Mediator command with response.
  /// This is an alias for <see cref="AddRoute{TCommand, TResponse}(string, string?)"/> that mirrors ASP.NET Core's Map pattern.
  /// Requires AddDependencyInjection() to be called first.
  /// </summary>
  /// <typeparam name="TCommand">The command type that implements <see cref="IRequest{TResponse}"/>.</typeparam>
  /// <typeparam name="TResponse">The response type returned by the command.</typeparam>
  /// <param name="pattern">The route pattern (e.g., "calculate {a} {b}").</param>
  /// <param name="description">Optional description for help text.</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// var builder = NuruApp.CreateBuilder(args);
  /// builder.Map&lt;CalculateCommand, int&gt;("calculate {a:int} {b:int}");
  /// await builder.Build().RunAsync(args);
  /// </code>
  /// </example>
  public NuruAppBuilder Map<TCommand, TResponse>(string pattern, string? description = null)
    where TCommand : IRequest<TResponse>, new()
    => AddRoute<TCommand, TResponse>(pattern, description);

  /// <summary>
  /// Maps a default route that executes when no arguments are provided.
  /// This is an alias for <see cref="AddDefaultRoute(Delegate, string?)"/> that mirrors ASP.NET Core's Map pattern.
  /// </summary>
  /// <param name="handler">The delegate to execute when no arguments are provided.</param>
  /// <param name="description">Optional description for help text.</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// var builder = NuruApp.CreateBuilder(args);
  /// builder.MapDefault(() => Console.WriteLine("Welcome! Use --help for available commands."));
  /// await builder.Build().RunAsync(args);
  /// </code>
  /// </example>
  public NuruAppBuilder MapDefault(Delegate handler, string? description = null)
    => AddDefaultRoute(handler, description);

  #endregion

  /// <summary>
  /// Adds a default route that executes when no arguments are provided.
  /// </summary>
  public NuruAppBuilder AddDefaultRoute(Delegate handler, string? description = null)
  {
    return AddRouteInternal(string.Empty, handler, description);
  }

  /// <summary>
  /// Adds REPL (Read-Eval-Print Loop) configuration options to application.
  /// This stores REPL configuration options for use when REPL mode is activated.
  /// </summary>
  /// <param name="configureOptions">Optional action to configure REPL options.</param>
  /// <returns>The builder for chaining.</returns>
  public NuruAppBuilder AddReplOptions(Action<ReplOptions>? configureOptions = null)
  {
    var replOptions = new ReplOptions();
    configureOptions?.Invoke(replOptions);
    ReplOptions = replOptions;
    return this;
  }

  /// <summary>
  /// Adds a delegate-based route.
  /// </summary>
  public NuruAppBuilder AddRoute(string pattern, Delegate handler, string? description = null)
  {
    ArgumentNullException.ThrowIfNull(pattern);
    return AddRouteInternal(pattern, handler, description);
  }

  /// <summary>
  /// Adds a Mediator command-based route.
  /// Requires AddDependencyInjection() to be called first.
  /// </summary>
  public NuruAppBuilder AddRoute<TCommand>(string pattern, string? description = null)
    where TCommand : IRequest, new()
  {
    return AddMediatorRoute(typeof(TCommand), pattern, description);
  }

  /// <summary>
  /// Adds a Mediator command-based route with response.
  /// Requires AddDependencyInjection() to be called first.
  /// </summary>
  public NuruAppBuilder AddRoute<TCommand, TResponse>(string pattern, string? description = null)
    where TCommand : IRequest<TResponse>, new()
  {
    return AddMediatorRoute(typeof(TCommand), pattern, description);
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
  /// builder.AddRoutes(
  ///     ["exit", "quit", "q"],
  ///     () => Environment.Exit(0),
  ///     "Exit the application");
  /// </code>
  /// </example>
  public NuruAppBuilder AddRoutes(string[] patterns, Delegate handler, string? description = null)
  {
    ArgumentNullException.ThrowIfNull(patterns);
    ArgumentNullException.ThrowIfNull(handler);

    if (patterns.Length == 0)
      throw new ArgumentException("At least one pattern required", nameof(patterns));

    foreach (string pattern in patterns)
    {
      AddRouteInternal(pattern, handler, description);
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
  /// builder.AddRoutes&lt;ExitCommand&gt;(
  ///     ["exit", "quit", "q"],
  ///     "Exit the application");
  /// </code>
  /// </example>
  public NuruAppBuilder AddRoutes<TCommand>(string[] patterns, string? description = null)
    where TCommand : IRequest, new()
  {
    ArgumentNullException.ThrowIfNull(patterns);

    if (patterns.Length == 0)
      throw new ArgumentException("At least one pattern required", nameof(patterns));

    foreach (string pattern in patterns)
    {
      AddMediatorRoute(typeof(TCommand), pattern, description);
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
  /// builder.AddRoutes&lt;GreetCommand, string&gt;(
  ///     ["greet", "hello", "hi"],
  ///     "Greet someone");
  /// </code>
  /// </example>
  public NuruAppBuilder AddRoutes<TCommand, TResponse>(string[] patterns, string? description = null)
    where TCommand : IRequest<TResponse>, new()
  {
    ArgumentNullException.ThrowIfNull(patterns);

    if (patterns.Length == 0)
      throw new ArgumentException("At least one pattern required", nameof(patterns));

    foreach (string pattern in patterns)
    {
      AddMediatorRoute(typeof(TCommand), pattern, description);
    }

    return this;
  }

  /// <summary>
  /// Registers a custom type converter for parameter conversion.
  /// </summary>
  /// <param name="converter">The type converter to register.</param>
  public NuruAppBuilder AddTypeConverter(IRouteTypeConverter converter)
  {
    ArgumentNullException.ThrowIfNull(converter);
    TypeConverterRegistry.RegisterConverter(converter);
    return this;
  }

  private NuruAppBuilder AddMediatorRoute(Type commandType, string pattern, string? description)
  {
    if (ServiceCollection is null)
    {
      throw new InvalidOperationException("Dependency injection must be added before using Mediator commands. Call AddDependencyInjection() first.");
    }

    var endpoint = new Endpoint
    {
      RoutePattern = pattern,
      CompiledRoute = PatternParser.Parse(pattern, LoggerFactory),
      Description = description,
      CommandType = commandType
    };

    EndpointCollection.Add(endpoint);
    return this;
  }

  private NuruAppBuilder AddRouteInternal(string pattern, Delegate handler, string? description)
  {
    ArgumentNullException.ThrowIfNull(handler);

    // Log route registration if logger is available
    if (LoggerFactory is not null)
    {
      ILogger<NuruAppBuilder> logger = LoggerFactory.CreateLogger<NuruAppBuilder>();
      if (EndpointCollection.Count == 0)
      {
        LoggerMessages.StartingRouteRegistration(logger, null);
      }

      LoggerMessages.RegisteringRoute(logger, pattern, null);
    }

    var endpoint = new Endpoint
    {
      RoutePattern = pattern,
      CompiledRoute = PatternParser.Parse(pattern, LoggerFactory),
      Handler = handler,
      Method = handler.Method,
      Description = description
    };

    EndpointCollection.Add(endpoint);
    return this;
  }
}
