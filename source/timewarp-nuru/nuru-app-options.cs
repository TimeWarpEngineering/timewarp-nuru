namespace TimeWarp.Nuru;

/// <summary>
/// Configuration options for <see cref="NuruApp.CreateBuilder"/> that controls how extensions are auto-wired.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a way to configure all auto-wired extensions (telemetry, REPL, shell completion, interactive routes)
/// when using <see cref="NuruApp.CreateBuilder"/>. Without this, users would need to use
/// <see cref="NuruCoreApp.CreateBuilder"/> and manually wire everything to avoid duplicate route warnings.
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
///   ConfigureCompletion = registry =>
///   {
///     registry.RegisterForParameter("env", new StaticCompletionSource("dev", "staging", "prod"));
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
  /// Action to configure shell completion sources.
  /// If null, dynamic completion uses default (no custom sources).
  /// </summary>
  /// <remarks>
  /// Shell completion (Tab-completion in bash, zsh, pwsh, fish) is distinct from REPL tab-completion:
  /// <list type="bullet">
  /// <item><description>Shell completion: External process - shell invokes the CLI app to get completions before command execution</description></item>
  /// <item><description>REPL completion: In-process - the running REPL handles Tab keypresses internally</description></item>
  /// </list>
  /// </remarks>
  public Action<CompletionSourceRegistry>? ConfigureCompletion { get; set; }

  /// <summary>
  /// Comma-separated route patterns that trigger interactive REPL mode.
  /// Default is "--interactive,-i".
  /// </summary>
  public string InteractiveRoutePatterns { get; set; } = "--interactive,-i";

  /// <summary>
  /// Action to configure help output filtering and display.
  /// If null, help uses default options (hide per-command help routes, REPL commands in CLI, completion routes).
  /// </summary>
  public Action<HelpOptions>? ConfigureHelp { get; set; }

  /// <summary>
  /// When true, disables OpenTelemetry integration via <c>UseTelemetry()</c>.
  /// Default is false (telemetry is enabled).
  /// </summary>
  /// <remarks>
  /// When disabled, no ActivitySource, Meter, or OpenTelemetry providers are configured.
  /// This can improve startup performance when telemetry is not needed.
  /// </remarks>
  public bool DisableTelemetry { get; set; }

  /// <summary>
  /// When true, disables REPL support and routes (exit, quit, history, clear, etc.).
  /// Default is false (REPL is enabled).
  /// </summary>
  /// <remarks>
  /// When disabled, the following routes are not registered:
  /// exit, quit, q, history, clear, cls, clear-history.
  /// The <c>--interactive,-i</c> route is controlled separately by <see cref="DisableInteractiveRoute"/>.
  /// </remarks>
  public bool DisableRepl { get; set; }

  /// <summary>
  /// When true, disables dynamic shell completion routes.
  /// Default is false (completion is enabled).
  /// </summary>
  /// <remarks>
  /// When disabled, the following routes are not registered:
  /// <c>__complete</c>, <c>--generate-completion</c>, <c>--install-completion</c>, <c>--install-completion --dry-run</c>.
  /// </remarks>
  public bool DisableCompletion { get; set; }

  /// <summary>
  /// When true, disables the <c>--interactive,-i</c> route.
  /// Default is false (interactive route is enabled).
  /// </summary>
  /// <remarks>
  /// This route triggers REPL mode when invoked.
  /// Note: REPL support itself is controlled by <see cref="DisableRepl"/>.
  /// </remarks>
  public bool DisableInteractiveRoute { get; set; }

  /// <summary>
  /// When true, disables the automatic registration of the <c>--version,-v</c> route.
  /// Default is false (version route is registered).
  /// </summary>
  /// <remarks>
  /// The version route displays:
  /// <list type="bullet">
  /// <item><description>Assembly informational version (or simple version as fallback)</description></item>
  /// <item><description>Commit hash (if available from TimeWarp.Build.Tasks)</description></item>
  /// <item><description>Commit date (if available from TimeWarp.Build.Tasks)</description></item>
  /// </list>
  /// </remarks>
  public bool DisableVersionRoute { get; set; }

  /// <summary>
  /// When true, disables the automatic registration of the <c>--check-updates</c> route.
  /// Default is false (check-updates route is registered).
  /// </summary>
  /// <remarks>
  /// The check-updates route:
  /// <list type="bullet">
  /// <item><description>Queries GitHub releases for the latest version</description></item>
  /// <item><description>Compares against the current assembly version</description></item>
  /// <item><description>Displays update availability with colored output</description></item>
  /// </list>
  /// Requires <c>RepositoryUrl</c> to be set in the project file pointing to a GitHub repository.
  /// </remarks>
  public bool DisableCheckUpdatesRoute { get; set; }

  /// <summary>
  /// When true, disables the automatic registration of the <c>--capabilities</c> route.
  /// Default is false (capabilities route is registered).
  /// </summary>
  /// <remarks>
  /// The capabilities route outputs machine-readable JSON metadata about all commands,
  /// enabling AI tools (OpenCode, Claude, etc.) to discover CLI capabilities without MCP complexity.
  /// </remarks>
  public bool DisableCapabilitiesRoute { get; set; }
}
