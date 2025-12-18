namespace TimeWarp.Nuru;

/// <summary>
/// Route registration methods for NuruCoreAppBuilder.
/// </summary>
public partial class NuruCoreAppBuilder
{
  /// <summary>
  /// Adds a default route that executes when no arguments are provided.
  /// </summary>
  public virtual EndpointBuilder MapDefault(Delegate handler, string? description = null)
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
  public virtual EndpointBuilder Map(string pattern, Delegate handler, string? description = null)
  {
    ArgumentNullException.ThrowIfNull(pattern);
    return MapInternal(pattern, handler, description);
  }

  /// <summary>
  /// Adds a route using fluent <see cref="NestedCompiledRouteBuilder{TParent}"/> configuration.
  /// Use <see cref="EndpointBuilder.WithHandler"/> to set the handler after configuring the route.
  /// </summary>
  /// <param name="configureRoute">
  /// Function to configure the route pattern. Must call <see cref="NestedCompiledRouteBuilder{TParent}.Done"/>
  /// to complete route configuration and return the <see cref="EndpointBuilder"/>.
  /// </param>
  /// <param name="description">Optional description shown in help.</param>
  /// <returns>An <see cref="EndpointBuilder"/> for further endpoint configuration.</returns>
  /// <example>
  /// <code>
  /// builder.Map(route => route
  ///     .WithLiteral("deploy")
  ///     .WithParameter("env")
  ///     .WithOption("force", "f")
  ///     .Done())                    // Must call Done() to complete route configuration
  ///     .WithHandler(async (string env, bool force) => await Deploy(env, force))
  ///     .AsCommand()
  ///     .Done();
  /// </code>
  /// </example>
  public virtual EndpointBuilder Map(
    Func<NestedCompiledRouteBuilder<EndpointBuilder>, EndpointBuilder> configureRoute,
    string? description = null)
  {
    ArgumentNullException.ThrowIfNull(configureRoute);

    // Create endpoint with placeholder values - these will be set by the nested builder callback
    Endpoint endpoint = new()
    {
      RoutePattern = string.Empty,  // Placeholder - set in callback below
      CompiledRoute = new CompiledRoute { Segments = [] },  // Placeholder - set in callback below
      Description = description
      // Handler will be set via EndpointBuilder.WithHandler()
    };

    EndpointCollection.Add(endpoint);

    EndpointBuilder endpointBuilder = new(this, endpoint);
    NestedCompiledRouteBuilder<EndpointBuilder> routeBuilder = new(
      endpointBuilder,
      route =>
      {
        endpoint.CompiledRoute = route;
        endpoint.RoutePattern = GeneratePatternFromCompiledRoute(route);
      });

    return configureRoute(routeBuilder);
  }

  /// <summary>
  /// Adds a Mediator command-based route.
  /// Requires AddDependencyInjection() to be called first.
  /// </summary>
  public virtual EndpointBuilder Map<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand>(string pattern, string? description = null)
    where TCommand : IRequest, new()
  {
    return MapMediator(typeof(TCommand), pattern, description);
  }

  /// <summary>
  /// Adds a Mediator command-based route with response.
  /// Requires AddDependencyInjection() to be called first.
  /// </summary>
  public virtual EndpointBuilder Map<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand, TResponse>(string pattern, string? description = null)
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

  // Internal methods for creating EndpointBuilder<TBuilder> - used by extension methods
  internal EndpointBuilder<TBuilder> MapInternalTyped<TBuilder>(string pattern, Delegate handler, string? description)
    where TBuilder : NuruCoreAppBuilder
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
    return new EndpointBuilder<TBuilder>((TBuilder)this, endpoint);
  }

  internal EndpointBuilder<TBuilder> MapMediatorTyped<TBuilder>(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    Type commandType,
    string pattern,
    string? description)
    where TBuilder : NuruCoreAppBuilder
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
    return new EndpointBuilder<TBuilder>((TBuilder)this, endpoint);
  }

  internal EndpointBuilder<TBuilder> MapNestedTyped<TBuilder>(
    Func<NestedCompiledRouteBuilder<EndpointBuilder<TBuilder>>, EndpointBuilder<TBuilder>> configureRoute,
    string? description = null)
    where TBuilder : NuruCoreAppBuilder
  {
    ArgumentNullException.ThrowIfNull(configureRoute);

    // Create endpoint with placeholder values - these will be set by the nested builder callback
    Endpoint endpoint = new()
    {
      RoutePattern = string.Empty,  // Placeholder - set in callback below
      CompiledRoute = new CompiledRoute { Segments = [] },  // Placeholder - set in callback below
      Description = description
      // Handler will be set via EndpointBuilder.WithHandler()
    };

    EndpointCollection.Add(endpoint);

    EndpointBuilder<TBuilder> endpointBuilder = new((TBuilder)this, endpoint);
    NestedCompiledRouteBuilder<EndpointBuilder<TBuilder>> routeBuilder = new(
      endpointBuilder,
      route =>
      {
        endpoint.CompiledRoute = route;
        endpoint.RoutePattern = GeneratePatternFromCompiledRoute(route);
      });

    return configureRoute(routeBuilder);
  }

  private EndpointBuilder MapMediator(
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
    return new EndpointBuilder(this, endpoint);
  }

  private EndpointBuilder MapInternal(string pattern, Delegate handler, string? description)
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
    return new EndpointBuilder(this, endpoint);
  }

  /// <summary>
  /// Generates a display pattern string from a compiled route for help text.
  /// </summary>
  private static string GeneratePatternFromCompiledRoute(CompiledRoute route)
  {
    List<string> parts = [];

    foreach (RouteMatcher segment in route.Segments)
    {
      switch (segment)
      {
        case LiteralMatcher literal:
          parts.Add(literal.Value);
          break;

        case ParameterMatcher param:
          string paramPart = param.IsCatchAll ? $"{{*{param.Name}" : $"{{{param.Name}";
          if (!string.IsNullOrEmpty(param.Constraint))
            paramPart += $":{param.Constraint}";
          if (param.IsOptional)
            paramPart += "?";
          paramPart += "}";
          parts.Add(paramPart);
          break;

        case OptionMatcher option:
          string optPart = option.MatchPattern; // e.g., "--force"
          if (option.AlternateForm is not null)
            optPart += $"|-{option.AlternateForm.TrimStart('-')}"; // e.g., "--force|-f"
          if (option.ExpectsValue && option.ParameterName is not null)
          {
            optPart += $" {{{option.ParameterName}";
            if (option.ParameterIsOptional)
              optPart += "?";

            optPart += "}";
          }

          if (option.IsOptional)
            optPart = $"[{optPart}]";

          parts.Add(optPart);
          break;
      }
    }

    return string.Join(" ", parts);
  }
}
