namespace TimeWarp.Nuru;

/// <summary>
/// Provides fluent configuration for a registered route endpoint.
/// Returned by <see cref="NuruCoreAppBuilder.Map"/> methods to enable post-registration configuration.
/// </summary>
/// <typeparam name="TBuilder">The builder type to return to, enabling proper fluent chaining.</typeparam>
/// <remarks>
/// <para>
/// This class enables the fluent API pattern for configuring route metadata:
/// </para>
/// <code>
/// app.Map("users list", handler)
///    .AsQuery()
///    .Done()                    // Returns to TBuilder
///    .AddReplSupport()          // Extension methods work!
///    .Build();
///
/// // Or use inline configuration with Also():
/// app.Map("users list", handler)
///    .Also(r => r.AsQuery())    // Returns RouteConfigurator&lt;TBuilder&gt;
///    .Done()
///    .AddReplSupport()
///    .Build();
/// </code>
/// </remarks>
public sealed class RouteConfigurator<TBuilder> : IBuilder<TBuilder>
  where TBuilder : NuruCoreAppBuilder
{
  private readonly TBuilder _builder;
  private readonly Endpoint _endpoint;

  internal RouteConfigurator(TBuilder builder, Endpoint endpoint)
  {
    _builder = builder;
    _endpoint = endpoint;
  }

  /// <summary>
  /// Returns to the parent builder to continue fluent chaining.
  /// </summary>
  /// <returns>The builder for further configuration.</returns>
  public TBuilder Done() => _builder;

  /// <summary>
  /// Marks this route as a query operation (no state change - safe to run and retry freely).
  /// </summary>
  /// <returns>This configurator for further route configuration.</returns>
  /// <remarks>
  /// AI agents can run query routes without confirmation and safely retry on failure.
  /// Examples: list, get, status, show, describe
  /// </remarks>
  public RouteConfigurator<TBuilder> AsQuery()
  {
    _endpoint.MessageType = MessageType.Query;
    _endpoint.CompiledRoute.MessageType = MessageType.Query;
    return this;
  }

  /// <summary>
  /// Marks this route as a command operation (state change, not repeatable - confirm before running).
  /// This is the default behavior.
  /// </summary>
  /// <returns>This configurator for further route configuration.</returns>
  /// <remarks>
  /// AI agents should ask for confirmation before running command routes and should not auto-retry.
  /// Examples: create, append, send, delete (non-idempotent)
  /// </remarks>
  public RouteConfigurator<TBuilder> AsCommand()
  {
    _endpoint.MessageType = MessageType.Command;
    _endpoint.CompiledRoute.MessageType = MessageType.Command;
    return this;
  }

  /// <summary>
  /// Marks this route as an idempotent command (state change but repeatable - safe to retry on failure).
  /// </summary>
  /// <returns>This configurator for further route configuration.</returns>
  /// <remarks>
  /// AI agents may run idempotent commands with less caution than regular commands
  /// and can safely retry on failure.
  /// Examples: set, enable, disable, upsert, update
  /// </remarks>
  public RouteConfigurator<TBuilder> AsIdempotentCommand()
  {
    _endpoint.MessageType = MessageType.IdempotentCommand;
    _endpoint.CompiledRoute.MessageType = MessageType.IdempotentCommand;
    return this;
  }

  /// <summary>
  /// Adds a delegate-based route (forwarded to the app builder).
  /// Enables fluent chaining after route configuration.
  /// </summary>
  public RouteConfigurator<TBuilder> Map(string pattern, Delegate handler, string? description = null) =>
    _builder.MapInternalTyped<TBuilder>(pattern, handler, description);

  /// <summary>
  /// Adds a Mediator command-based route (forwarded to the app builder).
  /// Enables fluent chaining after route configuration.
  /// </summary>
  public RouteConfigurator<TBuilder> Map<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand>(string pattern, string? description = null)
    where TCommand : IRequest, new() =>
    _builder.MapMediatorTyped<TBuilder>(typeof(TCommand), pattern, description);

  /// <summary>
  /// Adds a Mediator command-based route with response (forwarded to the app builder).
  /// Enables fluent chaining after route configuration.
  /// </summary>
  public RouteConfigurator<TBuilder> Map<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand, TResponse>(string pattern, string? description = null)
    where TCommand : IRequest<TResponse>, new() =>
    _builder.MapMediatorTyped<TBuilder>(typeof(TCommand), pattern, description);

  /// <summary>
  /// Adds a default route that executes when no arguments are provided (forwarded to the app builder).
  /// Enables fluent chaining after route configuration.
  /// </summary>
  public RouteConfigurator<TBuilder> MapDefault(Delegate handler, string? description = null) =>
    _builder.MapInternalTyped<TBuilder>(string.Empty, handler, description);

  /// <summary>
  /// Adds multiple route patterns that invoke the same handler (forwarded to the app builder).
  /// Enables fluent chaining after route configuration.
  /// </summary>
  public TBuilder MapMultiple(string[] patterns, Delegate handler, string? description = null) =>
    (TBuilder)_builder.MapMultiple(patterns, handler, description);

  /// <summary>
  /// Adds automatic help routes to the application (forwarded to the app builder).
  /// Enables fluent chaining after route configuration.
  /// </summary>
  public TBuilder AddAutoHelp() => (TBuilder)_builder.AddAutoHelp();

  /// <summary>
  /// Builds the NuruCoreApp from the configured builder.
  /// Enables fluent chaining to terminate with Build().
  /// </summary>
  public NuruCoreApp Build() => _builder.Build();

  /// <summary>
  /// Gets the underlying app builder for accessing extension methods.
  /// Prefer using Done() for better fluent API ergonomics.
  /// </summary>
  /// <example>
  /// <code>
  /// // Preferred - use Done()
  /// app.Map("test", handler).Done().AddReplSupport().Build();
  ///
  /// // Alternative - use Builder property
  /// app.Map("test", handler).Builder.AddReplSupport().Build();
  /// </code>
  /// </example>
  public TBuilder Builder => _builder;

  /// <summary>
  /// Implicitly converts to the builder type, allowing seamless continuation of builder chain.
  /// </summary>
  /// <param name="configurator">The route configurator.</param>
  /// <returns>The underlying app builder.</returns>
#pragma warning disable CA2225, CA1062 // Implicit conversion provides better fluent API ergonomics
  public static implicit operator TBuilder(RouteConfigurator<TBuilder> configurator) => configurator._builder;
#pragma warning restore CA2225, CA1062
}

/// <summary>
/// Non-generic RouteConfigurator for backward compatibility.
/// Prefer using <see cref="RouteConfigurator{TBuilder}"/> for type-safe fluent chaining.
/// </summary>
public sealed class RouteConfigurator : IBuilder<NuruCoreAppBuilder>
{
  private readonly NuruCoreAppBuilder _builder;
  private readonly Endpoint _endpoint;

  internal RouteConfigurator(NuruCoreAppBuilder builder, Endpoint endpoint)
  {
    _builder = builder;
    _endpoint = endpoint;
  }

  /// <summary>
  /// Returns to the parent builder to continue fluent chaining.
  /// </summary>
  /// <returns>The builder for further configuration.</returns>
  public NuruCoreAppBuilder Done() => _builder;

  /// <summary>
  /// Marks this route as a query operation (no state change - safe to run and retry freely).
  /// </summary>
  /// <returns>The app builder for further configuration.</returns>
  /// <remarks>
  /// AI agents can run query routes without confirmation and safely retry on failure.
  /// Examples: list, get, status, show, describe
  /// </remarks>
  public NuruCoreAppBuilder AsQuery()
  {
    _endpoint.MessageType = MessageType.Query;
    _endpoint.CompiledRoute.MessageType = MessageType.Query;
    return _builder;
  }

  /// <summary>
  /// Marks this route as a command operation (state change, not repeatable - confirm before running).
  /// This is the default behavior.
  /// </summary>
  /// <returns>The app builder for further configuration.</returns>
  /// <remarks>
  /// AI agents should ask for confirmation before running command routes and should not auto-retry.
  /// Examples: create, append, send, delete (non-idempotent)
  /// </remarks>
  public NuruCoreAppBuilder AsCommand()
  {
    _endpoint.MessageType = MessageType.Command;
    _endpoint.CompiledRoute.MessageType = MessageType.Command;
    return _builder;
  }

  /// <summary>
  /// Marks this route as an idempotent command (state change but repeatable - safe to retry on failure).
  /// </summary>
  /// <returns>The app builder for further configuration.</returns>
  /// <remarks>
  /// AI agents may run idempotent commands with less caution than regular commands
  /// and can safely retry on failure.
  /// Examples: set, enable, disable, upsert, update
  /// </remarks>
  public NuruCoreAppBuilder AsIdempotentCommand()
  {
    _endpoint.MessageType = MessageType.IdempotentCommand;
    _endpoint.CompiledRoute.MessageType = MessageType.IdempotentCommand;
    return _builder;
  }

  /// <summary>
  /// Adds a delegate-based route (forwarded to the app builder).
  /// Enables fluent chaining after route configuration.
  /// </summary>
  public RouteConfigurator Map(string pattern, Delegate handler, string? description = null) =>
    _builder.Map(pattern, handler, description);

  /// <summary>
  /// Adds a Mediator command-based route (forwarded to the app builder).
  /// Enables fluent chaining after route configuration.
  /// </summary>
  public RouteConfigurator Map<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand>(string pattern, string? description = null)
    where TCommand : IRequest, new() =>
    _builder.Map<TCommand>(pattern, description);

  /// <summary>
  /// Adds a Mediator command-based route with response (forwarded to the app builder).
  /// Enables fluent chaining after route configuration.
  /// </summary>
  public RouteConfigurator Map<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand, TResponse>(string pattern, string? description = null)
    where TCommand : IRequest<TResponse>, new() =>
    _builder.Map<TCommand, TResponse>(pattern, description);

  /// <summary>
  /// Adds a default route that executes when no arguments are provided (forwarded to the app builder).
  /// Enables fluent chaining after route configuration.
  /// </summary>
  public RouteConfigurator MapDefault(Delegate handler, string? description = null) =>
    _builder.MapDefault(handler, description);

  /// <summary>
  /// Adds multiple route patterns that invoke the same handler (forwarded to the app builder).
  /// Enables fluent chaining after route configuration.
  /// </summary>
  public NuruCoreAppBuilder MapMultiple(string[] patterns, Delegate handler, string? description = null) =>
    _builder.MapMultiple(patterns, handler, description);

  /// <summary>
  /// Adds automatic help routes to the application (forwarded to the app builder).
  /// Enables fluent chaining after route configuration.
  /// </summary>
  public NuruCoreAppBuilder AddAutoHelp() => _builder.AddAutoHelp();

  /// <summary>
  /// Builds the NuruCoreApp from the configured builder.
  /// Enables fluent chaining to terminate with Build().
  /// </summary>
  public NuruCoreApp Build() => _builder.Build();

  /// <summary>
  /// Gets the underlying app builder for accessing extension methods.
  /// Use this when you need to call extension methods that don't have RouteConfigurator overloads.
  /// </summary>
  /// <example>
  /// <code>
  /// app.Map("test", handler).Builder.AddReplSupport().Build();
  /// </code>
  /// </example>
  public NuruCoreAppBuilder Builder => _builder;

  /// <summary>
  /// Implicitly converts to NuruCoreAppBuilder, allowing seamless continuation of builder chain.
  /// </summary>
  /// <param name="configurator">The route configurator.</param>
  /// <returns>The underlying app builder.</returns>
#pragma warning disable CA2225, CA1062 // Implicit conversion provides better fluent API ergonomics
  public static implicit operator NuruCoreAppBuilder(RouteConfigurator configurator) => configurator._builder;
#pragma warning restore CA2225, CA1062

  /// <summary>
  /// Gets the underlying app builder.
  /// Provided as an alternate for the implicit conversion operator.
  /// </summary>
  /// <returns>The app builder for further configuration.</returns>
  public NuruCoreAppBuilder ToNuruCoreAppBuilder() => _builder;
}
