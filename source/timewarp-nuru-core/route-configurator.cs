namespace TimeWarp.Nuru;

/// <summary>
/// Provides fluent configuration for a registered route endpoint.
/// Returned by <see cref="NuruCoreAppBuilder.Map"/> methods to enable post-registration configuration.
/// </summary>
/// <remarks>
/// <para>
/// This class enables the fluent API pattern for configuring route metadata:
/// </para>
/// <code>
/// app.Map("users list", handler).AsQuery();
/// app.Map("config set {key} {value}", handler).AsIdempotentCommand();
/// app.Map("user create {name}", handler).AsCommand(); // Explicit (default)
/// </code>
/// </remarks>
public sealed class RouteConfigurator
{
  private readonly NuruCoreAppBuilder _builder;
  private readonly Endpoint _endpoint;

  internal RouteConfigurator(NuruCoreAppBuilder builder, Endpoint endpoint)
  {
    _builder = builder;
    _endpoint = endpoint;
  }

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
