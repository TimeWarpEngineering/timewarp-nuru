#region Purpose
// Single reader for the release version in source/Directory.Build.props, shared by
// check-version and release so both gates agree on what "the version" is
// (kanban task 470-007, parent-470 finding M10: check-version did not trim the
// <Version> element while release did, so a whitespace-padded element made
// check-version miss an already-published version).
#endregion
#region Design
// Reads {repoRoot}/source/Directory.Build.props, preferring a namespaced
// <Version> (legacy MSBuild namespace) and falling back to an un-namespaced one
// (SDK-style props). The value is trimmed; a missing, empty or whitespace-only
// element yields null so callers hit their existing "could not read Version"
// error instead of comparing "" or "\n  " against NuGet.
#endregion

namespace DevCli;

using System.Xml.Linq;

public static class PropsVersionReader
{
  private static readonly XNamespace MsBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

  /// <summary>
  /// Returns the trimmed <c>&lt;Version&gt;</c> from <c>{repoRoot}/source/Directory.Build.props</c>,
  /// or <see langword="null"/> when the repo root, file, or element is missing or blank.
  /// </summary>
  public static string? Read(string? repoRoot)
  {
    if (repoRoot is null)
    {
      return null;
    }

    string sourceDir = Path.Combine(repoRoot, "source");
    if (!Directory.Exists(sourceDir))
    {
      return null;
    }

    string[] buildPropsFiles = Directory.GetFiles(sourceDir, "Directory.Build.props", SearchOption.TopDirectoryOnly);
    if (buildPropsFiles is not { Length: > 0 })
    {
      return null;
    }

    return Parse(File.ReadAllText(buildPropsFiles[0]));
  }

  /// <summary>
  /// Returns the trimmed <c>&lt;Version&gt;</c> from props XML already in hand (for example
  /// <c>git show {sha}:source/Directory.Build.props</c>), or <see langword="null"/> when the
  /// element is missing or blank. A leading UTF-8 byte order mark is ignored: git passes the
  /// file's BOM through as U+FEFF, where <see cref="File.ReadAllText(string)"/> strips it.
  /// </summary>
  public static string? Parse(string xml)
  {
    ArgumentNullException.ThrowIfNull(xml);

    XElement? root = XDocument.Parse(xml.TrimStart('\uFEFF')).Root;

    XElement? versionElement = root?.Descendants(MsBuildNamespace + "Version").FirstOrDefault()
      ?? root?.Descendants("Version").FirstOrDefault();

    string? version = versionElement?.Value.Trim();
    return string.IsNullOrEmpty(version) ? null : version;
  }
}
