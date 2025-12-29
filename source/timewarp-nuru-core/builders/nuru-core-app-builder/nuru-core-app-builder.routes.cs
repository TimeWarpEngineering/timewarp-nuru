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
    // Source generator parses pattern and creates route at compile time
    _ = pattern;
    return new EndpointBuilder<TSelf>((TSelf)this);
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
    // Source generator extracts nested route configuration at compile time
    _ = configureRoute;
    return new EndpointBuilder<TSelf>((TSelf)this);
  }

  /// <summary>
  /// Registers a custom type converter for parameter conversion.
  /// </summary>
  /// <param name="converter">The type converter to register.</param>
  /// <remarks>
  /// This method is retained for API compatibility. Custom type converters
  /// are detected by the source generator at compile time.
  /// </remarks>
  public virtual TSelf AddTypeConverter(IRouteTypeConverter converter)
  {
    // Source generator handles type conversion at compile time.
    // Custom converters are registered via attributes or DSL analysis.
    _ = converter;
    return (TSelf)this;
  }

  /// <summary>
  /// Creates a route group with a shared prefix.
  /// All routes defined within the group will have the prefix prepended.
  /// </summary>
  /// <param name="prefix">The prefix for all routes in this group (e.g., "admin").</param>
  /// <returns>A GroupBuilder for configuring nested routes.</returns>
  /// <example>
  /// <code>
  /// builder.WithGroupPrefix("admin")
  ///     .Map("status")
  ///       .WithHandler(() => "admin status")
  ///       .Done()
  ///     .WithGroupPrefix("config")  // Nested: "admin config"
  ///       .Map("get {key}")         // Route: "admin config get {key}"
  ///         .WithHandler((string key) => $"value: {key}")
  ///         .Done()
  ///       .Done()  // End config group
  ///     .Done();   // End admin group
  /// </code>
  /// </example>
  public virtual GroupBuilder<TSelf> WithGroupPrefix(string prefix)
  {
    // Source generator handles group prefix at compile time
    _ = prefix;
    return new GroupBuilder<TSelf>((TSelf)this);
  }

}
