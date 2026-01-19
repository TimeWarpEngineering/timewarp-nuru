namespace TimeWarp.Nuru;

/// <summary>
/// Full-featured CLI app with all extensions auto-wired.
/// Inherits from <see cref="NuruCoreApp"/> and adds Telemetry, REPL, and Completion.
/// </summary>
public class NuruApp : NuruCoreApp
{
  public NuruApp() : base()  { }

  // Telemetry is automatically flushed by the generated RunAsync interceptor
  // when UseTelemetry() is called. No manual flush is needed.

  /// <summary>
  /// Creates a full-featured builder with DI, Configuration, and all extensions auto-wired.
  /// Use fluent extension methods to configure individual features:
  /// <see cref="NuruCoreAppBuilder{TSelf}.AddRepl(Action{ReplOptions})"/> for REPL,
  /// <see cref="NuruCoreAppBuilder{TSelf}.AddHelp(Action{HelpOptions})"/> for help,
  /// <see cref="NuruCoreAppBuilder{TSelf}.AddConfiguration"/> for configuration.
  /// </summary>
  /// <param name="args">Command line arguments.</param>
  /// <param name="options">Optional core application options.</param>
  /// <returns>A configured NuruAppBuilder with all extensions.</returns>
  /// <example>
  /// <code>
  /// NuruApp.CreateBuilder(args)
  ///     .AddRepl(options =>
  ///     {
  ///       options.Prompt = "myapp> ";
  ///       options.WelcomeMessage = "Welcome!";
  ///     })
  ///     .Map("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
  ///     .Build();
  /// </code>
  /// </example>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
    Justification = "Builder ownership is transferred to caller who is responsible for disposal")]
  public static NuruAppBuilder CreateBuilder(string[] args, NuruCoreApplicationOptions? options = null)
  {
    ArgumentNullException.ThrowIfNull(args);
    options ??= new NuruCoreApplicationOptions();
    options.Args = args;
    NuruAppBuilder builder = new(options);
    return builder;
  }
}
