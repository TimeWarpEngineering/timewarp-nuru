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

    // Register REPL commands as routes
    builder
      .AddRoute("exit", () => ReplSession.CurrentSession?.Exit(), "Exit the REPL")
      .AddRoute("quit", () => ReplSession.CurrentSession?.Exit(), "Exit the REPL")
      .AddRoute("q", () => ReplSession.CurrentSession?.Exit(), "Exit the REPL")
      .AddRoute("history", () => ReplSession.CurrentSession?.ShowHistory(), "Show command history")
      .AddRoute("clear", () => ReplSession.CurrentSession?.ClearScreen(), "Clear the screen")
      .AddRoute("cls", () => ReplSession.CurrentSession?.ClearScreen(), "Clear the screen")
      .AddRoute("clear-history", () => ReplSession.CurrentSession?.ClearHistory(), "Clear command history")
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
  public static NuruAppBuilder AddReplSupport(
    this NuruAppBuilder builder,
    Action<ReplOptions>? configureOptions = null)
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
  ///   .AddRoute("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
  ///   .AddRoute("status", () => Console.WriteLine("OK"))
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
}
