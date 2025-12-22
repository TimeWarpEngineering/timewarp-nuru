namespace TimeWarp.Nuru;

using System.Reflection;

/// <summary>
/// Version display route handler for NuruAppBuilderExtensions.
/// </summary>
/// <remarks>
/// This partial class contains:
/// <list type="bullet">
/// <item><description><see cref="AddVersionRoute{TBuilder}"/> - Registers the --version,-v route</description></item>
/// <item><description><see cref="DisplayVersion"/> - Handler that displays version, commit hash, and date</description></item>
/// </list>
/// </remarks>
public static partial class NuruAppBuilderExtensions
{
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
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    ArgumentNullException.ThrowIfNull(builder);
    builder.Map("--version,-v")
      .WithHandler(DisplayVersion)
      .WithDescription("Display version information")
      .Done();
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
