namespace TimeWarp.Nuru;

/// <summary>
/// Route registration methods for NuruCoreAppBuilder.
/// </summary>
public partial class NuruCoreAppBuilder<TSelf>
{
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
  /// Adds a route pattern for fluent configuration.
  /// Use <see cref="EndpointBuilder{TSelf}.WithHandler"/> to set the handler,
  /// and <see cref="EndpointBuilder{TSelf}.WithDescription"/> to set the description.
  /// </summary>
  /// <param name="pattern">The route pattern to match (e.g., "deploy {env}").</param>
  /// <returns>An <see cref="EndpointBuilder{TSelf}"/> for further endpoint configuration.</returns>
  /// <example>
  /// <code>
  /// builder.Map("deploy {env}")
  ///     .WithHandler((string env) => Deploy(env))
  ///     .WithDescription("Deploy to the specified environment")
  ///     .AsCommand()
  ///     .Done();
  /// </code>
  /// </example>
  public virtual EndpointBuilder<TSelf> Map(string pattern)
  {
    ArgumentNullException.ThrowIfNull(pattern);
    return MapPatternTyped(pattern);
  }

  /// <summary>
  /// Adds a route using fluent <see cref="NestedCompiledRouteBuilder{TParent}"/> configuration.
  /// Use <see cref="EndpointBuilder{TSelf}.WithHandler"/> to set the handler after configuring the route.
  /// </summary>
  /// <param name="configureRoute">
  /// Function to configure the route pattern. Must call <see cref="NestedCompiledRouteBuilder{TParent}.Done"/>
  /// to complete route configuration and return the <see cref="EndpointBuilder{TSelf}"/>.
  /// </param>
  /// <returns>An <see cref="EndpointBuilder{TSelf}"/> for further endpoint configuration.</returns>
  /// <example>
  /// <code>
  /// builder.Map(route => route
  ///     .WithLiteral("deploy")
  ///     .WithParameter("env")
  ///     .WithOption("force", "f")
  ///     .Done())                    // Must call Done() to complete route configuration
  ///     .WithHandler(async (string env, bool force) => await Deploy(env, force))
  ///     .WithDescription("Deploy to an environment")
  ///     .AsCommand()
  ///     .Done();
  /// </code>
  /// </example>
  public virtual EndpointBuilder<TSelf> Map(
    Func<NestedCompiledRouteBuilder<EndpointBuilder<TSelf>>, EndpointBuilder<TSelf>> configureRoute)
  {
    return MapNestedTyped(configureRoute);
  }

  /// <summary>
  /// Adds a Mediator command-based route.
  /// Requires AddDependencyInjection() to be called first.
  /// Use <see cref="EndpointBuilder{TSelf}.WithDescription"/> to set the description.
  /// </summary>
  public virtual EndpointBuilder<TSelf> Map<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand>(string pattern)
    where TCommand : IRequest, new()
  {
    return MapMediatorTyped(typeof(TCommand), pattern);
  }

  /// <summary>
  /// Adds a Mediator command-based route with response.
  /// Requires AddDependencyInjection() to be called first.
  /// Use <see cref="EndpointBuilder{TSelf}.WithDescription"/> to set the description.
  /// </summary>
  public virtual EndpointBuilder<TSelf> Map<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand, TResponse>(string pattern)
    where TCommand : IRequest<TResponse>, new()
  {
    return MapMediatorTyped(typeof(TCommand), pattern);
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

  // Internal method for creating EndpointBuilder<TSelf> from pattern only - uses CRTP for type preservation
  private EndpointBuilder<TSelf> MapPatternTyped(string pattern)
  {
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
      CompiledRoute = PatternParser.Parse(pattern, LoggerFactory)
      // Handler and Description will be set via EndpointBuilder.WithHandler() and WithDescription()
    };

    EndpointCollection.Add(endpoint);
    return new EndpointBuilder<TSelf>((TSelf)this, endpoint);
  }

  // Internal method for creating EndpointBuilder<TSelf> with handler - uses CRTP for type preservation
  private EndpointBuilder<TSelf> MapInternalTyped(string pattern, Delegate handler)
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
      Method = handler.Method
      // Description will be set via EndpointBuilder.WithDescription()
    };

    EndpointCollection.Add(endpoint);
    return new EndpointBuilder<TSelf>((TSelf)this, endpoint);
  }

  // Internal method for creating EndpointBuilder<TSelf> for Mediator commands - uses CRTP for type preservation
  private EndpointBuilder<TSelf> MapMediatorTyped(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    Type commandType,
    string pattern)
  {
    if (ServiceCollection is null)
    {
      throw new InvalidOperationException("Dependency injection must be added before using Mediator commands. Call AddDependencyInjection() first.");
    }

    Endpoint endpoint = new()
    {
      RoutePattern = pattern,
      CompiledRoute = PatternParser.Parse(pattern, LoggerFactory),
      CommandType = commandType
      // Description will be set via EndpointBuilder.WithDescription()
    };

    EndpointCollection.Add(endpoint);
    return new EndpointBuilder<TSelf>((TSelf)this, endpoint);
  }

  // Internal method for creating nested route builder with EndpointBuilder<TSelf> - uses CRTP for type preservation
  private EndpointBuilder<TSelf> MapNestedTyped(
    Func<NestedCompiledRouteBuilder<EndpointBuilder<TSelf>>, EndpointBuilder<TSelf>> configureRoute)
  {
    ArgumentNullException.ThrowIfNull(configureRoute);

    // Create endpoint with placeholder values - these will be set by the nested builder callback
    Endpoint endpoint = new()
    {
      RoutePattern = string.Empty,  // Placeholder - set in callback below
      CompiledRoute = new CompiledRoute { Segments = [] }  // Placeholder - set in callback below
      // Handler and Description will be set via EndpointBuilder.WithHandler() and WithDescription()
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
