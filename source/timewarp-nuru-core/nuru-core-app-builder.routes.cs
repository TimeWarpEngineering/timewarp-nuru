namespace TimeWarp.Nuru;

/// <summary>
/// Route registration methods for NuruCoreAppBuilder.
/// </summary>
public partial class NuruCoreAppBuilder<TSelf>
{
  /// <summary>
  /// Adds a default route that executes when no arguments are provided.
  /// </summary>
  public virtual EndpointBuilder<TSelf> MapDefault(Delegate handler, string? description = null)
  {
    return MapInternalTyped(string.Empty, handler, description);
  }

  /// <summary>
  /// Adds REPL (Read-Eval-Print Loop) configuration options to application.
  /// This stores REPL configuration options for use when REPL mode is activated.
  /// </summary>
  /// <param name="configureOptions">Optional action to configure REPL options.</param>
  /// <returns>The builder for chaining.</returns>
  public virtual TSelf AddReplOptions(Action<ReplOptions>? configureOptions = null)
  {
    ReplOptions replOptions = new();
    configureOptions?.Invoke(replOptions);
    ReplOptions = replOptions;
    return (TSelf)this;
  }

  /// <summary>
  /// Adds a delegate-based route.
  /// </summary>
  public virtual EndpointBuilder<TSelf> Map(string pattern, Delegate handler, string? description = null)
  {
    ArgumentNullException.ThrowIfNull(pattern);
    return MapInternalTyped(pattern, handler, description);
  }

  /// <summary>
  /// Adds a route using fluent <see cref="NestedCompiledRouteBuilder{TParent}"/> configuration.
  /// Use <see cref="EndpointBuilder{TSelf}.WithHandler"/> to set the handler after configuring the route.
  /// </summary>
  /// <param name="configureRoute">
  /// Function to configure the route pattern. Must call <see cref="NestedCompiledRouteBuilder{TParent}.Done"/>
  /// to complete route configuration and return the <see cref="EndpointBuilder{TSelf}"/>.
  /// </param>
  /// <param name="description">Optional description shown in help.</param>
  /// <returns>An <see cref="EndpointBuilder{TSelf}"/> for further endpoint configuration.</returns>
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
  public virtual EndpointBuilder<TSelf> Map(
    Func<NestedCompiledRouteBuilder<EndpointBuilder<TSelf>>, EndpointBuilder<TSelf>> configureRoute,
    string? description = null)
  {
    return MapNestedTyped(configureRoute, description);
  }

  /// <summary>
  /// Adds a Mediator command-based route.
  /// Requires AddDependencyInjection() to be called first.
  /// </summary>
  public virtual EndpointBuilder<TSelf> Map<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand>(string pattern, string? description = null)
    where TCommand : IRequest, new()
  {
    return MapMediatorTyped(typeof(TCommand), pattern, description);
  }

  /// <summary>
  /// Adds a Mediator command-based route with response.
  /// Requires AddDependencyInjection() to be called first.
  /// </summary>
  public virtual EndpointBuilder<TSelf> Map<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand, TResponse>(string pattern, string? description = null)
    where TCommand : IRequest<TResponse>, new()
  {
    return MapMediatorTyped(typeof(TCommand), pattern, description);
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
  public virtual TSelf MapMultiple(string[] patterns, Delegate handler, string? description = null)
  {
    ArgumentNullException.ThrowIfNull(patterns);
    ArgumentNullException.ThrowIfNull(handler);

    if (patterns.Length == 0)
      throw new ArgumentException("At least one pattern required", nameof(patterns));

    foreach (string pattern in patterns)
    {
      MapInternalTyped(pattern, handler, description);
    }

    return (TSelf)this;
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
  public virtual TSelf MapMultiple<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand>(string[] patterns, string? description = null)
    where TCommand : IRequest, new()
  {
    ArgumentNullException.ThrowIfNull(patterns);

    if (patterns.Length == 0)
      throw new ArgumentException("At least one pattern required", nameof(patterns));

    foreach (string pattern in patterns)
    {
      MapMediatorTyped(typeof(TCommand), pattern, description);
    }

    return (TSelf)this;
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
  public virtual TSelf MapMultiple<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand, TResponse>(string[] patterns, string? description = null)
    where TCommand : IRequest<TResponse>, new()
  {
    ArgumentNullException.ThrowIfNull(patterns);

    if (patterns.Length == 0)
      throw new ArgumentException("At least one pattern required", nameof(patterns));

    foreach (string pattern in patterns)
    {
      MapMediatorTyped(typeof(TCommand), pattern, description);
    }

    return (TSelf)this;
  }

  /// <summary>
  /// Registers a custom type converter for parameter conversion.
  /// </summary>
  /// <param name="converter">The type converter to register.</param>
  public virtual TSelf AddTypeConverter(IRouteTypeConverter converter)
  {
    ArgumentNullException.ThrowIfNull(converter);
    TypeConverterRegistry.RegisterConverter(converter);
    return (TSelf)this;
  }

  // Internal method for creating EndpointBuilder<TSelf> - uses CRTP for type preservation
  private EndpointBuilder<TSelf> MapInternalTyped(string pattern, Delegate handler, string? description)
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
    return new EndpointBuilder<TSelf>((TSelf)this, endpoint);
  }

  // Internal method for creating EndpointBuilder<TSelf> for Mediator commands - uses CRTP for type preservation
  private EndpointBuilder<TSelf> MapMediatorTyped(
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
    return new EndpointBuilder<TSelf>((TSelf)this, endpoint);
  }

  // Internal method for creating nested route builder with EndpointBuilder<TSelf> - uses CRTP for type preservation
  private EndpointBuilder<TSelf> MapNestedTyped(
    Func<NestedCompiledRouteBuilder<EndpointBuilder<TSelf>>, EndpointBuilder<TSelf>> configureRoute,
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

    EndpointBuilder<TSelf> endpointBuilder = new((TSelf)this, endpoint);
    NestedCompiledRouteBuilder<EndpointBuilder<TSelf>> routeBuilder = new(
      endpointBuilder,
      route =>
      {
        endpoint.CompiledRoute = route;
        endpoint.RoutePattern = GeneratePatternFromCompiledRoute(route);
      });

    return configureRoute(routeBuilder);
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
