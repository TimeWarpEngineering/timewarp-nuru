namespace TimeWarp.Nuru;

/// <summary>
/// Route registration methods for NuruAppBuilder.
/// </summary>
public partial class NuruAppBuilder
{
  /// <summary>
  /// Enables REPL (Read-Eval-Print Loop) mode support.
  /// When enabled, the application can be started in interactive mode using --interactive or -i flag.
  /// </summary>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// NuruApp.CreateBuilder(args)
  ///     .AddRepl()  // Enable REPL mode
  ///     .Map("greet {name}")
  ///       .WithHandler((string name) => $"Hello, {name}!")
  ///       .Done()
  ///     .Build();
  ///
  /// // Run with: ./myapp -i  or  ./myapp --interactive
  /// </code>
  /// </example>
  public virtual NuruAppBuilder AddRepl()
  {
    ReplOptions ??= new ReplOptions();
    return this;
  }

  /// <summary>
  /// Enables REPL mode with custom configuration options.
  /// </summary>
  /// <param name="configureOptions">Action to configure REPL options (prompt, history, colors, etc.).</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// NuruApp.CreateBuilder(args)
  ///     .AddRepl(options =>
  ///     {
  ///         options.Prompt = "myapp> ";
  ///         options.WelcomeMessage = "Welcome to MyApp!";
  ///         options.EnableColors = true;
  ///     })
  ///     .Map("greet {name}")
  ///       .WithHandler((string name) => $"Hello, {name}!")
  ///       .Done()
  ///     .Build();
  /// </code>
  /// </example>
  public virtual NuruAppBuilder AddRepl(Action<ReplOptions> configureOptions)
  {
    ArgumentNullException.ThrowIfNull(configureOptions);
    ReplOptions replOptions = new();
    configureOptions(replOptions);
    ReplOptions = replOptions;
    return this;
  }

  /// <summary>
  /// Adds REPL (Read-Eval-Print Loop) configuration options to application.
  /// This stores REPL configuration options for use when REPL mode is activated.
  /// </summary>
  /// <param name="configureOptions">Optional action to configure REPL options.</param>
  /// <returns>The builder for chaining.</returns>
  [Obsolete("Use AddRepl() or AddRepl(Action<ReplOptions>) instead.")]
  public virtual NuruAppBuilder AddReplOptions(Action<ReplOptions>? configureOptions = null)
  {
    ReplOptions replOptions = new();
    configureOptions?.Invoke(replOptions);
    ReplOptions = replOptions;
    return this;
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
  public virtual EndpointBuilder<NuruAppBuilder> Map(string pattern)
  {
    // Source generator parses pattern and creates route at compile time
    _ = pattern;
    return new EndpointBuilder<NuruAppBuilder>(this);
  }

  /// <summary>
  /// Adds a route using fluent <see cref="NestedCompiledRouteBuilder{TParent}"/> configuration.
  /// Use <see cref="EndpointBuilder{TSelf}.WithHandler"/> to set the handler after configuring the route.
  /// </summary>
  /// <param name="configureRoute">
  /// Function to configure the route pattern. Must call <see cref="NestedCompiledRouteBuilder{TParent}.Done"/>
  /// to complete route configuration and return the <see cref="EndpointBuilder{TSelf}"/>
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
  public virtual EndpointBuilder<NuruAppBuilder> Map(
    Func<NestedCompiledRouteBuilder<EndpointBuilder<NuruAppBuilder>>, EndpointBuilder<NuruAppBuilder>> configureRoute)
  {
    // Source generator extracts nested route configuration at compile time
    _ = configureRoute;
    return new EndpointBuilder<NuruAppBuilder>(this);
  }

  /// <summary>
  /// Registers a custom type converter for parameter conversion.
  /// </summary>
  /// <param name="converter">The type converter to register.</param>
  /// <remarks>
  /// This method is retained for API compatibility. Custom type converters
  /// are detected by the source generator at compile time.
  /// </remarks>
  public virtual NuruAppBuilder AddTypeConverter(IRouteTypeConverter converter)
  {
    // Source generator handles type conversion at compile time.
    // Custom converters are registered via attributes or DSL analysis.
    _ = converter;
    return this;
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
  public virtual GroupBuilder<NuruAppBuilder> WithGroupPrefix(string prefix)
  {
    // Source generator handles group prefix at compile time
    _ = prefix;
    return new GroupBuilder<NuruAppBuilder>(this);
  }

  /// <summary>
  /// Discovers and includes all [NuruRoute] endpoint classes from the assembly.
  /// Without this call, no endpoints are included (only fluent routes).
  /// </summary>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// // Include all [NuruRoute] endpoint classes
  /// NuruApp.CreateBuilder(args)
  ///     .DiscoverEndpoints()
  ///     .Build();
  /// </code>
  /// </example>
  public virtual NuruAppBuilder DiscoverEndpoints()
  {
    // Source generator discovers [NuruRoute] classes at compile time
    return this;
  }

  /// <summary>
  /// Discovers and includes only [NuruRoute] endpoint classes that belong to the specified group types.
  /// Use this overload to create subset CLI editions with only specific groups of endpoints.
  /// </summary>
  /// <param name="groupTypes">The group type classes to filter endpoints by.</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// // Include only endpoints from the MaintenanceGroup
  /// NuruApp.CreateBuilder(args)
  ///     .DiscoverEndpoints(typeof(MaintenanceGroup))
  ///     .Build();
  /// </code>
  /// </example>
  public virtual NuruAppBuilder DiscoverEndpoints(params Type[] groupTypes)
  {
    // Source generator discovers [NuruRoute] classes filtered by group types at compile time
    _ = groupTypes;
    return this;
  }

  /// <summary>
  /// Includes a specific endpoint class in this application.
  /// Use this for explicit control over which endpoints are included.
  /// </summary>
  /// <typeparam name="TEndpoint">The endpoint class type with [NuruRoute] attribute.</typeparam>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// // Include only specific endpoints
  /// NuruApp.CreateBuilder(args)
  ///     .Map&lt;DeployCommand&gt;()
  ///     .Map&lt;BuildCommand&gt;()
  ///     .Build();
  /// </code>
  /// </example>
  public virtual NuruAppBuilder Map<TEndpoint>() where TEndpoint : class
  {
    // Source generator extracts the type and includes the endpoint at compile time
    return this;
  }

}
