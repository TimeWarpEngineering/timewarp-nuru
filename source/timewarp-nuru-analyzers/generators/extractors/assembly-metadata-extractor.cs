// Extracts assembly metadata (version, commit info) from compilation at compile time.
// This enables --capabilities to include version info without runtime reflection.

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Extracts assembly metadata from the compilation.
/// This runs at compile time, reading attributes set by MSBuild/TimeWarp.Build.Tasks.
/// </summary>
internal static class AssemblyMetadataExtractor
{
  /// <summary>
  /// Extracts assembly metadata from the compilation.
  /// </summary>
  /// <param name="compilation">The Roslyn compilation context.</param>
  /// <returns>Assembly metadata including version and optional commit info.</returns>
  public static AssemblyMetadata Extract(Compilation compilation)
  {
    IAssemblySymbol assembly = compilation.Assembly;

    string? version = null;
    string? commitHash = null;
    string? commitDate = null;

    foreach (AttributeData attr in assembly.GetAttributes())
    {
      string? attrName = attr.AttributeClass?.Name;

      // AssemblyInformationalVersionAttribute is the preferred version source
      // It may include +commit suffix (e.g., "1.2.3+abc123")
      if (attrName == "AssemblyInformationalVersionAttribute" &&
          attr.ConstructorArguments.Length > 0)
      {
        version = attr.ConstructorArguments[0].Value as string;
      }
      // AssemblyMetadataAttribute is used by TimeWarp.Build.Tasks for commit info
      else if (attrName == "AssemblyMetadataAttribute" &&
               attr.ConstructorArguments.Length >= 2)
      {
        string? key = attr.ConstructorArguments[0].Value as string;
        string? value = attr.ConstructorArguments[1].Value as string;

        if (key == "CommitHash")
          commitHash = value;
        else if (key == "CommitDate")
          commitDate = value;
      }
    }

    // Fallback to AssemblyVersion if no InformationalVersion
    version ??= assembly.Identity.Version.ToString();

    return new AssemblyMetadata(version, commitHash, commitDate);
  }
}

/// <summary>
/// Assembly metadata extracted at compile time.
/// </summary>
/// <param name="Version">Assembly version (from AssemblyInformationalVersionAttribute or AssemblyVersion).</param>
/// <param name="CommitHash">Git commit hash (from TimeWarp.Build.Tasks, may be null).</param>
/// <param name="CommitDate">Git commit date (from TimeWarp.Build.Tasks, may be null).</param>
internal sealed record AssemblyMetadata(
  string? Version,
  string? CommitHash,
  string? CommitDate);
