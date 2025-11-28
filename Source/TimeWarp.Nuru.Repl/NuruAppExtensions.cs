namespace TimeWarp.Nuru.Repl;

using TimeWarp.Nuru;

/// <summary>
/// Extension methods to enable REPL (Read-Eval-Print Loop) mode for Nuru applications.
/// </summary>
public static class NuruAppExtensions
{
  /// <summary>
  /// Adds REPL (Read-Eval-Print Loop) command routes to application.
  /// This registers built-in REPL commands as routes.
  /// </summary>
  /// <param name="builder">The NuruAppBuilder instance.</param>
  /// <returns>The builder for chaining.</returns>
  public static NuruAppBuilder AddReplRoutes(this NuruAppBuilder builder)
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
  /// <param name="builder">The NuruAppBuilder instance.</param>
  /// <param name="configureOptions">Optional action to configure REPL options.</param>
  /// <returns>The builder for chaining.</returns>
  public static NuruAppBuilder AddReplSupport
  (
    this NuruAppBuilder builder,
    Action<ReplOptions>? configureOptions = null
  )
  {
    ArgumentNullException.ThrowIfNull(builder);
    return builder.AddReplOptions(configureOptions).AddReplRoutes();
  }

  /// <summary>
  /// Runs the application with REPL support. If args contains "--repl", enters interactive mode.
  /// Otherwise, executes the command normally.
  /// </summary>
  /// <param name="app">The NuruApp instance.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Exit code from the command or REPL session.</returns>
  /// <example>
  /// <code>
  /// NuruApp app = NuruApp.CreateBuilder()
  ///   .Map("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
  ///   .Map("status", () => Console.WriteLine("OK"))
  ///   .Build();
  ///
  /// // Start REPL immediately
  /// return await app.RunReplAsync();
  /// </code>
  /// </example>
  public static Task<int> RunReplAsync
  (
    this NuruApp app,
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
  /// <param name="builder">The NuruAppBuilder instance.</param>
  /// <param name="patterns">Route patterns to trigger interactive mode (default: "--interactive,-i").</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// NuruApp app = new NuruAppBuilder()
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
  public static NuruAppBuilder AddInteractiveRoute
  (
    this NuruAppBuilder builder,
    string patterns = "--interactive,-i"
  )
  {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(patterns);

    string[] patternArray = patterns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    builder.MapMultiple(patternArray, StartInteractiveModeAsync, "Enter interactive REPL mode");

    return builder;
  }

  /// <summary>
  /// Static handler for the interactive mode route.
  /// Receives NuruApp via DI injection and starts the REPL.
  /// </summary>
  /// <param name="app">The NuruApp instance (injected by framework).</param>
  /// <returns>Exit code from the REPL session.</returns>
  public static Task<int> StartInteractiveModeAsync(NuruApp app)
  {
    return app.RunReplAsync();
  }
}
