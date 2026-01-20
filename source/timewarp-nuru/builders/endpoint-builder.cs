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
/// app.Map("users list")
///    .WithHandler(handler)
///    .WithDescription("List all users")
///    .AsQuery()
///    .Done()                    // Returns to TBuilder
///    .AddReplSupport()          // Extension methods work!
///    .Build();
///
/// // Or use inline configuration with Also():
/// app.Map("users list")
///    .WithHandler(handler)
///    .Also(r => r.AsQuery())    // Returns EndpointBuilder&lt;TBuilder&gt;
///    .Done()
///    .AddReplSupport()
///    .Build();
/// </code>
/// </remarks>
public class EndpointBuilder<TBuilder> : INestedBuilder<TBuilder>
  where TBuilder : NuruCoreAppBuilder<TBuilder>
{
  private readonly TBuilder ParentBuilder;

  internal EndpointBuilder(TBuilder builder)
  {
    ParentBuilder = builder;
  }

  /// <summary>
  /// Returns to the parent builder to continue fluent chaining.
  /// </summary>
  /// <returns>The builder for further configuration.</returns>
  public TBuilder Done() => ParentBuilder;

  /// <summary>
  /// Sets the handler delegate for this endpoint.
  /// </summary>
  /// <param name="handler">The delegate to invoke when this route is matched.</param>
  /// <returns>This configurator for further endpoint configuration.</returns>
  /// <remarks>
  /// Use this when building routes with <see cref="CompiledRouteBuilder"/> where the handler
  /// is set separately from the route pattern:
  /// <code>
  /// app.Map("deploy {env}")
  ///    .WithHandler(async (string env) => await Deploy(env))
  ///    .WithDescription("Deploy to an environment")
  ///    .AsCommand()
  ///    .Done();
  /// </code>
  /// </remarks>
  public EndpointBuilder<TBuilder> WithHandler(Delegate handler)
  {
    // Source generator extracts handler at compile time
    _ = handler;
    return this;
  }

  /// <summary>
  /// Sets the description for this endpoint (shown in help text).
  /// </summary>
  /// <param name="description">The description to display in help output.</param>
  /// <returns>This configurator for further endpoint configuration.</returns>
  /// <example>
  /// <code>
  /// app.Map("deploy {env}")
  ///    .WithHandler((string env) => Deploy(env))
  ///    .WithDescription("Deploy to the specified environment")
  ///    .AsCommand()
  ///    .Done();
  /// </code>
  /// </example>
  public EndpointBuilder<TBuilder> WithDescription(string description)
  {
    // Source generator extracts description at compile time
    _ = description;
    return this;
  }

  /// <summary>
  /// Marks this route as a query operation (no state change - safe to run and retry freely).
  /// </summary>
  /// <returns>This configurator for further route configuration.</returns>
  /// <remarks>
  /// AI agents can run query routes without confirmation and safely retry on failure.
  /// Examples: list, get, status, show, describe
  /// </remarks>
  public EndpointBuilder<TBuilder> AsQuery()
  {
    // Source generator sets MessageType at compile time
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
  public EndpointBuilder<TBuilder> AsCommand()
  {
    // Source generator sets MessageType at compile time
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
  public EndpointBuilder<TBuilder> AsIdempotentCommand()
  {
    // Source generator sets MessageType at compile time
    return this;
  }

  /// <summary>
  /// Declares that this route's command implements the specified interface.
  /// Used to enable filtered pipeline behaviors that only apply to commands implementing certain interfaces.
  /// </summary>
  /// <typeparam name="TFilter">The interface type to implement.</typeparam>
  /// <param name="configure">
  /// Lambda that configures the interface properties.
  /// The generator extracts property assignments from the syntax at compile time.
  /// The lambda is never actually invoked - it's purely declarative.
  /// </param>
  /// <returns>This configurator for further endpoint configuration.</returns>
  /// <remarks>
  /// <para>
  /// The source generator analyzes the lambda syntax to extract property assignments and generates
  /// a command class that implements the interface with those values baked in.
  /// </para>
  /// <para>
  /// Multiple <c>.Implements&lt;T&gt;()</c> calls can be chained to implement multiple interfaces.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Single interface
  /// .Map("admin {action}")
  ///   .Implements&lt;IRequireAuthorization&gt;(x =&gt; x.RequiredPermission = "admin:execute")
  ///   .WithHandler((string action) =&gt; Console.WriteLine($"Admin: {action}"))
  ///   .Done()
  ///
  /// // Multiple interfaces (chained)
  /// .Map("dangerous {op}")
  ///   .Implements&lt;IRequireAuthorization&gt;(x =&gt; x.RequiredPermission = "danger:execute")
  ///   .Implements&lt;IAuditable&gt;(x =&gt; x.AuditLevel = "Critical")
  ///   .WithHandler((string op) =&gt; Console.WriteLine($"Dangerous: {op}"))
  ///   .Done()
  /// </code>
  /// </example>
  public EndpointBuilder<TBuilder> Implements<TFilter>(Action<TFilter> configure) where TFilter : class
  {
    // Source generator extracts interface type and property assignments at compile time
    // The lambda is never actually invoked - purely declarative
    _ = configure;
    return this;
  }

  /// <summary>
  /// Adds a route pattern (forwarded to the app builder).
  /// Use <see cref="WithHandler"/> to set the handler after configuring the route.
  /// Enables fluent chaining after route configuration.
  /// </summary>
  /// <param name="pattern">The route pattern to match.</param>
  /// <returns>An <see cref="EndpointBuilder{TBuilder}"/> for further endpoint configuration.</returns>
  public EndpointBuilder<TBuilder> Map(string pattern) =>
    ParentBuilder.Map(pattern);

  /// <summary>
  /// Adds a route using fluent <see cref="NestedCompiledRouteBuilder{TParent}"/> configuration.
  /// Use <see cref="WithHandler"/> to set the handler after configuring the route.
  /// </summary>
  /// <param name="configureRoute">
  /// Function to configure the route pattern. Must call <see cref="NestedCompiledRouteBuilder{TParent}.Done"/>
  /// to complete route configuration and return the <see cref="EndpointBuilder{TBuilder}"/>.
  /// </param>
  /// <returns>An <see cref="EndpointBuilder{TBuilder}"/> for further endpoint configuration.</returns>
  public EndpointBuilder<TBuilder> Map(
    Func<NestedCompiledRouteBuilder<EndpointBuilder<TBuilder>>, EndpointBuilder<TBuilder>> configureRoute) =>
    ParentBuilder.Map(configureRoute);

  /// <summary>
  /// Builds the NuruApp from the configured builder.
  /// Enables fluent chaining to terminate with Build().
  /// </summary>
  public NuruApp Build() => ParentBuilder.Build();

  /// <summary>
  /// Gets the underlying app builder for accessing extension methods.
  /// Prefer using Done() for better fluent API ergonomics.
  /// </summary>
  /// <example>
  /// <code>
  /// // Preferred - use Done()
  /// app.Map("test").WithHandler(handler).Done().AddReplSupport().Build();
  ///
  /// // Alternative - use Builder property
  /// app.Map("test").WithHandler(handler).Builder.AddReplSupport().Build();
  /// </code>
  /// </example>
  public TBuilder Builder => ParentBuilder;

  /// <summary>
  /// Implicitly converts to the builder type, allowing seamless continuation of builder chain.
  /// </summary>
  /// <param name="configurator">The route configurator.</param>
  /// <returns>The underlying app builder.</returns>
#pragma warning disable CA2225, CA1062 // Implicit conversion provides better fluent API ergonomics
  public static implicit operator TBuilder(EndpointBuilder<TBuilder> configurator) => configurator.ParentBuilder;
#pragma warning restore CA2225, CA1062
}

/// <summary>
/// Type alias for EndpointBuilder with the non-generic NuruCoreAppBuilder.
/// Provided for backward compatibility and convenience when using the non-generic builder.
/// </summary>
public sealed class EndpointBuilder : EndpointBuilder<NuruCoreAppBuilder>
{
  internal EndpointBuilder(NuruCoreAppBuilder builder) : base(builder)
  {
  }
}
