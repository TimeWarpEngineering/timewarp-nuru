namespace TimeWarp.Nuru;

/// <summary>
/// Configuration options for <see cref="NuruApp.CreateBuilder"/> that controls how extensions are auto-wired.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a way to configure all auto-wired extensions (telemetry, REPL, interactive routes)
/// when using <see cref="NuruApp.CreateBuilder"/>. Without this, users would need to use
/// <see cref="NuruCoreApp.CreateSlimBuilder"/> and manually wire everything to avoid duplicate route warnings.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// NuruApp.CreateBuilder(args, new NuruAppOptions
/// {
///   ConfigureRepl = options =>
///   {
///     options.Prompt = "myapp> ";
///     options.WelcomeMessage = "Welcome to my app!";
///   },
///   ConfigureTelemetry = options =>
///   {
///     options.ServiceName = "my-app";
///   },
///   InteractiveRoutePatterns = "--interactive,-i,--repl"
/// })
/// .Map("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
/// .Build();
/// </code>
/// </example>
public sealed class NuruAppOptions
{
  /// <summary>
  /// Action to configure REPL options.
  /// If null, REPL uses default options.
  /// </summary>
  public Action<ReplOptions>? ConfigureRepl { get; set; }

  /// <summary>
  /// Action to configure telemetry options.
  /// If null, telemetry uses default options.
  /// </summary>
  public Action<NuruTelemetryOptions>? ConfigureTelemetry { get; set; }

  /// <summary>
  /// Comma-separated route patterns that trigger interactive REPL mode.
  /// Default is "--interactive,-i".
  /// </summary>
  public string InteractiveRoutePatterns { get; set; } = "--interactive,-i";
}
