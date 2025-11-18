namespace TimeWarp.Nuru.Repl;

using TimeWarp.Nuru;

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
  public static async Task<int> RunReplAsync(
    this NuruApp app,
    ReplOptions? options = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(app);

    // Use configured REPL options or provided options
    ReplOptions replOptions = options ?? app.ReplOptions ?? new ReplOptions();
    var repl = new ReplMode(app, replOptions);

    // Set static context for command handlers
    ReplContext.Current = repl;

    try
    {
      return await repl.RunAsync(cancellationToken).ConfigureAwait(false);
    }
    finally
    {
      ReplContext.Current = null;
    }
  }
}
