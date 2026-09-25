#region Purpose
// Package layout gate (kanban 461): verify a produced .nupkg actually contains
// its declared critical payload entries before CI uploads/publishes it.
// Exists because pack can silently produce hollow packages (evaluation-time
// wildcard globs — nuget.org TimeWarp.Nuru 3.0.0-beta.72 shipped without
// build/net10.0, breaking consumers with MSB4062 on clean restores). Tests
// exercise project references, never the packed nupkg — this gate is the only
// thing that looks inside the package.
//
// NuruRequiredPackageEntries (kanban 462) is the static required-payload list
// used by `dev workflow`. It must stay in sync with the explicit Pack="true"
// None items under build/ and analyzers/ in timewarp-nuru.csproj — the
// nupkg-layout-02-csproj-gate-parity test diffs those two sets. Do NOT parse
// the csproj on the release path; keep this array hand-maintained and
// fail-closed (461).
#endregion
#region Design
// Pure-ish: opens the zip read-only, compares entry names ordinally against a
// caller-supplied required list, returns the missing subset. No Terminal, no
// process execution — callers decide how loudly to fail (CI: abort the run).
#endregion

namespace DevCli;

using System.IO.Compression;
using System.Text.RegularExpressions;

public static class NupkgLayoutCheck
{
  private static readonly Regex TfmSegmentPattern = new(
    @"^net\d+(\.\d+)*$",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

  /// <summary>
  /// Critical payload the TimeWarp.Nuru nupkg must contain (kanban 461 / 462).
  /// Mirrors explicit Pack includes under build/ and analyzers/ in
  /// timewarp-nuru.csproj, plus the packed lib assembly. Update this array
  /// when those Pack items change — nupkg-layout-02-csproj-gate-parity enforces
  /// build/ + analyzers/ parity with the csproj.
  /// </summary>
  public static readonly string[] NuruRequiredPackageEntries =
  [
    "build/TimeWarp.Nuru.targets",
    "build/net10.0/TimeWarp.Nuru.Build.dll",
    "build/net10.0/TimeWarp.Nuru.Analyzers.dll",
    "build/net10.0/ICSharpCode.Decompiler.dll",
    "build/net10.0/Microsoft.Build.Framework.dll",
    "build/net10.0/Microsoft.Build.Utilities.Core.dll",
    "build/net10.0/Microsoft.CodeAnalysis.dll",
    "build/net10.0/Microsoft.CodeAnalysis.CSharp.dll",
    "build/net10.0/Microsoft.NET.StringTools.dll",
    "build/net10.0/System.Configuration.ConfigurationManager.dll",
    "build/net10.0/System.Diagnostics.EventLog.dll",
    "build/net10.0/System.Security.Cryptography.ProtectedData.dll",
    "analyzers/dotnet/cs/TimeWarp.Nuru.Analyzers.dll",
    "analyzers/dotnet/cs/Microsoft.Extensions.Logging.Abstractions.dll",
    "analyzers/dotnet/cs/ICSharpCode.Decompiler.dll",
    "lib/net10.0/TimeWarp.Nuru.dll"
  ];

  /// <summary>
  /// Returns the required entries NOT present in the package (empty = pass).
  /// Entry comparison is ordinal against forward-slash zip paths.
  /// </summary>
  public static IReadOnlyList<string> FindMissing(string nupkgPath, IReadOnlyList<string> requiredEntries)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(nupkgPath);
    ArgumentNullException.ThrowIfNull(requiredEntries);

    using ZipArchive zip = ZipFile.OpenRead(nupkgPath);
    HashSet<string> entries = new(StringComparer.Ordinal);
    foreach (ZipArchiveEntry entry in zip.Entries)
    {
      entries.Add(entry.FullName);
    }

    List<string> missing = [];
    foreach (string required in requiredEntries)
    {
      if (!entries.Contains(required))
      {
        missing.Add(required);
      }
    }

    return missing;
  }

  /// <summary>
  /// Resolves the published nupkg entry path for a Pack Include + PackagePath
  /// pair (kanban 462). PackagePath that already names a file is used as-is;
  /// directory PackagePaths append the Include file name. TFM segments like
  /// net10.0 are treated as directories, not file names.
  /// </summary>
  public static string ResolvePublishedEntry(string include, string packagePath)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(include);
    ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

    string normalizedPath = packagePath.Replace('\\', '/').TrimEnd('/');
    string fileName = Path.GetFileName(include.Replace('\\', '/'));
    if (string.IsNullOrEmpty(fileName))
    {
      throw new ArgumentException("Include must end with a file name.", nameof(include));
    }

    string lastSegment = normalizedPath.Split('/').Last();
    if (PackagePathNamesFile(lastSegment))
    {
      return normalizedPath;
    }

    return $"{normalizedPath}/{fileName}";
  }

  private static bool PackagePathNamesFile(string lastSegment) =>
    lastSegment.Contains('.', StringComparison.Ordinal)
    && !TfmSegmentPattern.IsMatch(lastSegment);
}
