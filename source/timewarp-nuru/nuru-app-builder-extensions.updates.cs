namespace TimeWarp.Nuru;

/// <summary>
/// Update checking route handler for NuruAppBuilderExtensions.
/// </summary>
/// <remarks>
/// This method is interpreted by the source generator at compile time.
/// The actual --check-updates handling is emitted as generated code.
/// This stub exists for API compatibility and to allow the DSL method to be recognized.
/// </remarks>
public static partial class NuruAppBuilderExtensions
{
  /// <summary>
  /// Adds a <c>--check-updates</c> route that checks GitHub for newer versions.
  /// </summary>
  /// <typeparam name="TBuilder">The builder type.</typeparam>
  /// <param name="builder">The NuruCoreAppBuilder instance.</param>
  /// <returns>The builder for chaining.</returns>
  /// <remarks>
  /// This method is interpreted by the source generator. The generated code:
  /// <list type="bullet">
  /// <item><description>Queries GitHub releases API for available versions</description></item>
  /// <item><description>Compares current version against latest release using SemVer</description></item>
  /// <item><description>Pre-release versions only compare against pre-releases</description></item>
  /// <item><description>Displays colored output (green = up-to-date, yellow = update available)</description></item>
  /// </list>
  /// Requires <c>RepositoryUrl</c> assembly metadata pointing to a GitHub repository.
  /// </remarks>
  public static TBuilder AddCheckUpdatesRoute<TBuilder>(this TBuilder builder)
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    // This method is interpreted by the source generator at compile time.
    // The actual --check-updates handling is emitted as generated code.
    // This stub exists for API compatibility.
    return builder;
  }
}
