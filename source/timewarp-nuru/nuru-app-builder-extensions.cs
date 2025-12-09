namespace TimeWarp.Nuru;

using System.Reflection;

/// <summary>
/// Extension methods that auto-wire all Nuru extensions to an existing builder.
/// </summary>
public static class NuruAppBuilderExtensions
{
  /// <summary>
  /// Adds all Nuru extensions to the builder: Telemetry, Logging, REPL, Shell Completion, and Interactive routes.
  /// This is called automatically by <see cref="NuruApp.CreateBuilder"/>.
  /// </summary>
  /// <param name="builder">The builder to extend.</param>
  /// <param name="options">Optional configuration for extensions. If null, uses defaults.</param>
  /// <returns>The builder for chaining.</returns>
  /// <remarks>
  /// This method adds:
  /// <list type="bullet">
  /// <item>OpenTelemetry integration via <c>UseTelemetry()</c></item>
  /// <item>REPL mode support via <c>AddReplSupport()</c></item>
  /// <item>Dynamic shell completion via <c>EnableDynamicCompletion()</c> (routes: <c>--generate-completion</c>, <c>__complete</c>, <c>--install-completion</c>)</item>
  /// <item>Interactive route (<c>--interactive</c>, <c>-i</c>) via <c>AddInteractiveRoute()</c></item>
  /// </list>
  /// Logging is already configured by <c>UseTelemetry()</c>.
  /// </remarks>
  public static NuruAppBuilder UseAllExtensions(this NuruAppBuilder builder, NuruAppOptions? options = null)
  {
    ArgumentNullException.ThrowIfNull(builder);
    options ??= new NuruAppOptions();

    // Configure help options if provided
    if (options.ConfigureHelp is not null)
    {
      builder.ConfigureHelp(options.ConfigureHelp);
    }

    builder
      .UseTelemetry(options.ConfigureTelemetry ?? (_ => { }))
      .AddReplSupport(options.ConfigureRepl)
      .EnableDynamicCompletion(configure: options.ConfigureCompletion)
      .AddInteractiveRoute(options.InteractiveRoutePatterns);

    // Add version route unless disabled
    if (!options.DisableVersionRoute)
    {
      builder.AddVersionRoute();
    }

    return builder;
  }

  /// <summary>
  /// Adds a <c>--version,-v</c> route that displays version information including commit hash and date when available.
  /// </summary>
  /// <typeparam name="TBuilder">The builder type.</typeparam>
  /// <param name="builder">The NuruCoreAppBuilder instance.</param>
  /// <returns>The builder for chaining.</returns>
  /// <remarks>
  /// The version output includes:
  /// <list type="bullet">
  /// <item><description>Assembly informational version (or simple version as fallback)</description></item>
  /// <item><description>Commit hash (if available from <c>AssemblyMetadataAttribute</c> with key "CommitHash")</description></item>
  /// <item><description>Commit date (if available from <c>AssemblyMetadataAttribute</c> with key "CommitDate")</description></item>
  /// </list>
  /// This information is automatically injected by TimeWarp.Build.Tasks which is a transitive dependency.
  /// </remarks>
  public static TBuilder AddVersionRoute<TBuilder>(this TBuilder builder)
    where TBuilder : NuruCoreAppBuilder
  {
    ArgumentNullException.ThrowIfNull(builder);
    builder.Map("--version,-v", DisplayVersion, "Display version information");
    return builder;
  }

  private const string VersionUnavailableMessage = "Version information unavailable";
  private const string UnknownVersion = "Unknown";
  private const string CommitHashKey = "CommitHash";
  private const string CommitDateKey = "CommitDate";

  /// <summary>
  /// Handler for the version route that displays version information.
  /// Uses Action (void return) so it uses the common "NoParams" invoker signature
  /// that virtually every consuming app will generate.
  /// </summary>
  internal static void DisplayVersion()
  {
    Assembly? entryAssembly = Assembly.GetEntryAssembly();

    if (entryAssembly is null)
    {
      Console.WriteLine(VersionUnavailableMessage);
      return;
    }

    // Get informational version or fall back to simple version
    string version = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
      ?? entryAssembly.GetName().Version?.ToString()
      ?? UnknownVersion;

    // Strip build metadata suffix (+<hash>) if present, per SemVer 2.0 convention
    // The full commit hash is displayed separately below
    int plusIndex = version.IndexOf('+', StringComparison.Ordinal);
    string displayVersion = plusIndex >= 0 ? version[..plusIndex] : version;
    Console.WriteLine(displayVersion);

    // Get commit hash and date from AssemblyMetadataAttribute (injected by TimeWarp.Build.Tasks)
    // Materialize to list to avoid multiple enumeration
    List<AssemblyMetadataAttribute> metadata = [.. entryAssembly.GetCustomAttributes<AssemblyMetadataAttribute>()];

    string? commitHash = metadata.Find(m => m.Key == CommitHashKey)?.Value;
    string? commitDate = metadata.Find(m => m.Key == CommitDateKey)?.Value;

    if (!string.IsNullOrEmpty(commitHash))
    {
      Console.WriteLine($"Commit: {commitHash}");
    }

    if (!string.IsNullOrEmpty(commitDate))
    {
      Console.WriteLine($"Date: {commitDate}");
    }
  }
}
