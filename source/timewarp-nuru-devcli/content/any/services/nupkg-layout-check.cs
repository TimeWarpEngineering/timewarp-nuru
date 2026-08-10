#region Purpose
// Package layout gate (kanban 461): verify a produced .nupkg actually contains
// its declared critical payload entries before CI uploads/publishes it.
// Exists because pack can silently produce hollow packages (evaluation-time
// wildcard globs — nuget.org TimeWarp.Nuru 3.0.0-beta.72 shipped without
// build/net10.0, breaking consumers with MSB4062 on clean restores). Tests
// exercise project references, never the packed nupkg — this gate is the only
// thing that looks inside the package.
#endregion
#region Design
// Pure-ish: opens the zip read-only, compares entry names ordinally against a
// caller-supplied required list, returns the missing subset. No Terminal, no
// process execution — callers decide how loudly to fail (CI: abort the run).
#endregion

namespace DevCli;

using System.IO.Compression;

public static class NupkgLayoutCheck
{
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
}
