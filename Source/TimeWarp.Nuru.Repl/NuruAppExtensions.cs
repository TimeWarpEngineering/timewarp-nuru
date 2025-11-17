namespace TimeWarp.Nuru.Repl;

/// <summary>
/// Extension methods to enable REPL (Read-Eval-Print Loop) mode for Nuru applications.
/// </summary>
public static class NuruAppExtensions
{
  /// <summary>
  /// Runs the application with REPL support. If args contains "--repl", enters interactive mode.
  /// Otherwise, executes the command normally.
  /// </summary>
  /// <param name="app">The NuruApp instance.</param>
  /// <param name="args">Command line arguments.</param>
  /// <param name="configureOptions">Optional action to configure REPL options.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Exit code from the command or REPL session.</returns>
  /// <example>
  /// <code>
  /// var app = NuruApp.CreateBuilder()
  ///   .AddRoute("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
  ///   .AddRoute("status", () => Console.WriteLine("OK"))
  ///   .Build();
  ///
  /// // Handles --repl flag automatically
  /// return await app.RunWithReplSupportAsync(args);
  /// </code>
  /// </example>
  public static Task<int> RunWithReplSupportAsync(
    this NuruApp app,
    string[] args,
    Action<ReplOptions>? configureOptions = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(app);
    ArgumentNullException.ThrowIfNull(args);

    // Check if --repl flag is present
    if (args.Length == 1 && args[0] == "--repl")
    {
      var options = new ReplOptions();
      configureOptions?.Invoke(options);

      var repl = new ReplMode(app, options);
      return repl.RunAsync(cancellationToken);
    }

    // Normal execution
    return app.RunAsync(args);
  }

  /// <summary>
  /// Starts the REPL loop directly, bypassing command line argument parsing.
  /// Use this when you want to programmatically start REPL mode.
  /// </summary>
  /// <param name="app">The NuruApp instance.</param>
  /// <param name="options">Optional REPL configuration options.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Exit code from the last executed command.</returns>
  /// <example>
  /// <code>
  /// var app = NuruApp.CreateBuilder()
  ///   .AddRoute("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
  ///   .Build();
  ///
  /// // Start REPL immediately
  /// return await app.RunReplAsync();
  /// </code>
  /// </example>
  public static Task<int> RunReplAsync(
    this NuruApp app,
    ReplOptions? options = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(app);

    var repl = new ReplMode(app, options);
    return repl.RunAsync(cancellationToken);
  }
}
