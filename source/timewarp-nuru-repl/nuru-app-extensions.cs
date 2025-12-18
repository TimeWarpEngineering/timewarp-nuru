namespace TimeWarp.Nuru;

/// <summary>
/// Extension methods to enable REPL (Read-Eval-Print Loop) mode for Nuru applications.
/// </summary>
public static class NuruCoreAppExtensions
{
  // ============================================================================
  // EndpointBuilder<TBuilder> overloads - preserve builder type in fluent chain
  // ============================================================================

  /// <summary>
  /// Adds REPL support to application (generic EndpointBuilder overload for fluent chaining).
  /// </summary>
  /// <typeparam name="TBuilder">The builder type for proper fluent chaining.</typeparam>
  /// <param name="configurator">The EndpointBuilder from a Map() call.</param>
  /// <param name="configureOptions">Optional action to configure REPL options.</param>
  /// <returns>The underlying builder for chaining.</returns>
  public static TBuilder AddReplSupport<TBuilder>
  (
    this EndpointBuilder<TBuilder> configurator,
    Action<ReplOptions>? configureOptions = null
  )
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    ArgumentNullException.ThrowIfNull(configurator);
    return configurator.Builder.AddReplSupport(configureOptions);
  }

  /// <summary>
  /// Adds REPL command routes to application (generic EndpointBuilder overload for fluent chaining).
  /// </summary>
  /// <typeparam name="TBuilder">The builder type for proper fluent chaining.</typeparam>
  /// <param name="configurator">The EndpointBuilder from a Map() call.</param>
  /// <returns>The underlying builder for chaining.</returns>
  public static TBuilder AddReplRoutes<TBuilder>(this EndpointBuilder<TBuilder> configurator)
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    ArgumentNullException.ThrowIfNull(configurator);
    return configurator.Builder.AddReplRoutes();
  }

  /// <summary>
  /// Adds an interactive mode route (generic EndpointBuilder overload for fluent chaining).
  /// </summary>
  /// <typeparam name="TBuilder">The builder type for proper fluent chaining.</typeparam>
  /// <param name="configurator">The EndpointBuilder from a Map() call.</param>
  /// <param name="patterns">Route patterns to trigger interactive mode.</param>
  /// <returns>The underlying builder for chaining.</returns>
  public static TBuilder AddInteractiveRoute<TBuilder>
  (
    this EndpointBuilder<TBuilder> configurator,
    string patterns = "--interactive,-i"
  )
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    ArgumentNullException.ThrowIfNull(configurator);
    return configurator.Builder.AddInteractiveRoute(patterns);
  }

  // ============================================================================
  // EndpointBuilder overloads (non-generic) - backward compatibility
  // ============================================================================

  /// <summary>
  /// Adds REPL support to application (EndpointBuilder overload for fluent chaining).
  /// </summary>
  /// <param name="configurator">The EndpointBuilder from a Map() call.</param>
  /// <param name="configureOptions">Optional action to configure REPL options.</param>
  /// <returns>The underlying builder for chaining.</returns>
  public static NuruCoreAppBuilder AddReplSupport
  (
    this EndpointBuilder configurator,
    Action<ReplOptions>? configureOptions = null
  )
  {
    ArgumentNullException.ThrowIfNull(configurator);
    return configurator.Builder.AddReplSupport(configureOptions);
  }

  /// <summary>
  /// Adds REPL command routes to application (EndpointBuilder overload for fluent chaining).
  /// </summary>
  /// <param name="configurator">The EndpointBuilder from a Map() call.</param>
  /// <returns>The underlying builder for chaining.</returns>
  public static NuruCoreAppBuilder AddReplRoutes(this EndpointBuilder configurator)
  {
    ArgumentNullException.ThrowIfNull(configurator);
    return configurator.Builder.AddReplRoutes();
  }

  /// <summary>
  /// Adds an interactive mode route (EndpointBuilder overload for fluent chaining).
  /// </summary>
  /// <param name="configurator">The EndpointBuilder from a Map() call.</param>
  /// <param name="patterns">Route patterns to trigger interactive mode.</param>
  /// <returns>The underlying builder for chaining.</returns>
  public static NuruCoreAppBuilder AddInteractiveRoute
  (
    this EndpointBuilder configurator,
    string patterns = "--interactive,-i"
  )
  {
    ArgumentNullException.ThrowIfNull(configurator);
    return configurator.Builder.AddInteractiveRoute(patterns);
  }

  // ============================================================================
  // NuruCoreAppBuilder extension methods
  // ============================================================================

  /// <summary>
  /// Adds REPL (Read-Eval-Print Loop) command routes to application.
  /// This registers built-in REPL commands as routes.
  /// </summary>
  /// <typeparam name="TBuilder">The builder type.</typeparam>
  /// <param name="builder">The NuruCoreAppBuilder instance.</param>
  /// <returns>The builder for chaining.</returns>
  public static TBuilder AddReplRoutes<TBuilder>(this TBuilder builder)
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    ArgumentNullException.ThrowIfNull(builder);

    // Register REPL commands as routes using clean method group syntax
    builder
      .MapMultiple(["exit", "quit", "q"], ReplSession.ExitAsync, "Exit the REPL")
      .Map("history", ReplSession.ShowHistoryAsync, "Show command history")
      .MapMultiple(["clear", "cls"], ReplSession.ClearScreenAsync, "Clear the screen")
      .Map("clear-history", ReplSession.ClearHistoryAsync, "Clear command history")
      .AddAutoHelp();

    return builder;
  }

  /// <summary>
  /// Adds REPL (Read-Eval-Print Loop) support to application.
  /// This stores REPL configuration options and registers REPL commands as routes.
  /// </summary>
  /// <typeparam name="TBuilder">The builder type.</typeparam>
  /// <param name="builder">The NuruCoreAppBuilder instance.</param>
  /// <param name="configureOptions">Optional action to configure REPL options.</param>
  /// <returns>The builder for chaining.</returns>
  public static TBuilder AddReplSupport<TBuilder>
  (
    this TBuilder builder,
    Action<ReplOptions>? configureOptions = null
  )
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    ArgumentNullException.ThrowIfNull(builder);
    builder.AddReplOptions(configureOptions).AddReplRoutes();
    return builder;
  }

  /// <summary>
  /// Runs the application with REPL support. If args contains "--repl", enters interactive mode.
  /// Otherwise, executes the command normally.
  /// </summary>
  /// <param name="app">The NuruCoreApp instance.</param>
  /// <param name="options">Optional REPL options to override configured options.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <example>
  /// <code>
  /// NuruCoreApp app = NuruCoreApp.CreateBuilder()
  ///   .Map("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
  ///   .Map("status", () => Console.WriteLine("OK"))
  ///   .Build();
  ///
  /// // Start REPL immediately
  /// await app.RunReplAsync();
  /// </code>
  /// </example>
  public static Task RunReplAsync
  (
    this NuruCoreApp app,
    ReplOptions? options = null,
    CancellationToken cancellationToken = default
  )
  {
    ArgumentNullException.ThrowIfNull(app);

    // Use configured REPL options or provided options
    ReplOptions replOptions = options ?? app.ReplOptions ?? new ReplOptions();

    return ReplSession.RunAsync(app, replOptions, app.LoggerFactory, cancellationToken);
  }

  /// <summary>
  /// Adds an interactive mode route that starts the REPL when invoked.
  /// This allows apps to support both CLI and REPL modes via command line.
  /// </summary>
  /// <typeparam name="TBuilder">The builder type.</typeparam>
  /// <param name="builder">The NuruCoreAppBuilder instance.</param>
  /// <param name="patterns">Route patterns to trigger interactive mode (default: "--interactive,-i").</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// NuruCoreApp app = NuruCoreAppBuilder.Create()
  ///   .Map("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
  ///   .AddReplSupport(options => options.Prompt = "myapp> ")
  ///   .AddInteractiveRoute()
  ///   .Build();
  ///
  /// // myapp greet Alice    - executes greeting
  /// // myapp --interactive  - enters REPL mode
  /// // myapp -i             - enters REPL mode (short form)
  /// return await app.RunAsync(args);
  /// </code>
  /// </example>
  public static TBuilder AddInteractiveRoute<TBuilder>
  (
    this TBuilder builder,
    string patterns = "--interactive,-i"
  )
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(patterns);

    string[] patternArray = patterns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // Alias syntax only works for exactly 2 options (long + short form)
    // If all patterns are options AND there are exactly 2, use alias syntax
    // Otherwise use MapMultiple for multiple options or literal command aliases
    bool allAreOptions = patternArray.All(p => p.StartsWith('-'));
    bool canUseAliasSyntax = allAreOptions && patternArray.Length == 2;

    if (canUseAliasSyntax)
    {
      // Use alias syntax: "--interactive,-i" creates single endpoint with alternate form
      builder.Map(patterns, StartInteractiveModeAsync, "Enter interactive REPL mode");
    }
    else
    {
      // Use MapMultiple for literals like ["interactive", "repl"]
      // or for more than 2 options which can't use alias syntax
      builder.MapMultiple(patternArray, StartInteractiveModeAsync, "Enter interactive REPL mode");
    }

    return builder;
  }

  /// <summary>
  /// Static handler for the interactive mode route.
  /// Receives NuruCoreAppHolder via DI injection and starts the REPL.
  /// </summary>
  /// <param name="appHolder">The NuruCoreAppHolder instance (injected by framework).</param>
  public static Task StartInteractiveModeAsync(NuruCoreAppHolder appHolder)
  {
    ArgumentNullException.ThrowIfNull(appHolder);
    return appHolder.App.RunReplAsync();
  }
}
