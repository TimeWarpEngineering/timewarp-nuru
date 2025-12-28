namespace TimeWarp.Nuru;

/// <summary>
/// Full-featured CLI app with all extensions auto-wired.
/// Inherits from <see cref="NuruCoreApp"/> and adds Telemetry, REPL, and Completion.
/// </summary>
public class NuruApp : NuruCoreApp
{
  public NuruApp() : base()  { }

  /// <summary>
  /// Runs the application and automatically flushes telemetry on completion.
  /// </summary>
  /// <param name="args">Command line arguments.</param>
  /// <returns>Exit code from the command execution.</returns>
  public new async Task<int> RunAsync(string[] args)
  {
    int exitCode = await base.RunAsync(args).ConfigureAwait(false);
    await NuruTelemetryExtensions.FlushAsync(delayMs: 0).ConfigureAwait(false);
    return exitCode;
  }

  /// <summary>
  /// Creates a full-featured builder with DI, Configuration, and all extensions auto-wired.
  /// </summary>
  /// <param name="args">Command line arguments.</param>
  /// <param name="options">Optional core application options.</param>
  /// <returns>A configured NuruAppBuilder with all extensions.</returns>
  public static NuruAppBuilder CreateBuilder(string[] args, NuruCoreApplicationOptions? options = null)
    => CreateBuilder(args, nuruAppOptions: null, options);

  /// <summary>
  /// Creates a full-featured builder with DI, Configuration, and all extensions auto-wired.
  /// </summary>
  /// <param name="args">Command line arguments.</param>
  /// <param name="nuruAppOptions">Options to configure auto-wired extensions (REPL, Telemetry, interactive routes).</param>
  /// <param name="coreOptions">Optional core application options.</param>
  /// <returns>A configured NuruAppBuilder with all extensions.</returns>
  /// <example>
  /// <code>
  /// NuruApp.CreateBuilder(args, new NuruAppOptions
  /// {
  ///   ConfigureRepl = options =>
  ///   {
  ///     options.Prompt = "myapp> ";
  ///     options.WelcomeMessage = "Welcome!";
  ///   },
  ///   ConfigureTelemetry = options =>
  ///   {
  ///     options.ServiceName = "my-service";
  ///   }
  /// })
  /// .Map("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
  /// .Build();
  /// </code>
  /// </example>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
    Justification = "Builder ownership is transferred to caller who is responsible for disposal")]
  public static NuruAppBuilder CreateBuilder
  (
    string[] args,
    NuruAppOptions? nuruAppOptions,
    NuruCoreApplicationOptions? coreOptions = null
  )
  {
    _ = nuruAppOptions; // currently unused - reserved for future use
    ArgumentNullException.ThrowIfNull(args);
    coreOptions ??= new NuruCoreApplicationOptions();
    coreOptions.Args = args;
    NuruAppBuilder builder = new(coreOptions);
    return builder;
  }
}
