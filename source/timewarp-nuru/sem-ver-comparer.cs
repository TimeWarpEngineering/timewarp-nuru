namespace TimeWarp.Nuru;

/// <summary>
/// Internal utility for semantic version comparison.
/// </summary>
/// <remarks>
/// Implements SemVer 2.0 comparison rules:
/// <list type="bullet">
/// <item><description>Major.minor.patch are compared numerically</description></item>
/// <item><description>A stable version (no prerelease) is greater than any prerelease of the same base version</description></item>
/// <item><description>Prerelease labels are compared segment by segment (numeric segments numerically, others lexicographically)</description></item>
/// </list>
/// </remarks>
internal static class SemVerComparer
{
  /// <summary>
  /// Compares two SemVer version strings.
  /// Returns: negative if v1 &lt; v2, zero if equal, positive if v1 &gt; v2.
  /// </summary>
  /// <remarks>
  /// Comparison strategy:
  /// 1. Compare major.minor.patch numerically
  /// 2. A stable version (no prerelease) is greater than any prerelease of the same base version
  /// 3. Prerelease labels are compared lexicographically (with numeric segment comparison)
  /// </remarks>
  public static int Compare(string version1, string version2)
  {
    // Split into base version and prerelease parts
    (string baseVersion1, string? prerelease1) = SplitVersion(version1);
    (string baseVersion2, string? prerelease2) = SplitVersion(version2);

    // Compare base versions (major.minor.patch)
    int baseComparison = CompareBaseVersions(baseVersion1, baseVersion2);
    if (baseComparison != 0)
    {
      return baseComparison;
    }

    // Base versions are equal, compare prerelease labels
    // Rule: no prerelease > any prerelease (stable is newer)
    if (prerelease1 is null && prerelease2 is null)
    {
      return 0;
    }

    if (prerelease1 is null)
    {
      return 1; // v1 is stable, v2 is prerelease -> v1 is newer
    }

    if (prerelease2 is null)
    {
      return -1; // v1 is prerelease, v2 is stable -> v2 is newer
    }

    // Both have prerelease labels - compare them
    return ComparePrereleaseLabels(prerelease1, prerelease2);
  }

  /// <summary>
  /// Splits a version string into base version and prerelease parts.
  /// </summary>
  private static (string BaseVersion, string? Prerelease) SplitVersion(string version)
  {
    int dashIndex = version.IndexOf('-', StringComparison.Ordinal);
    if (dashIndex >= 0)
    {
      return (version[..dashIndex], version[(dashIndex + 1)..]);
    }

    return (version, null);
  }

  /// <summary>
  /// Compares base versions (major.minor.patch) numerically.
  /// </summary>
  private static int CompareBaseVersions(string baseVersion1, string baseVersion2)
  {
    string[] parts1 = baseVersion1.Split('.');
    string[] parts2 = baseVersion2.Split('.');

    int maxParts = Math.Max(parts1.Length, parts2.Length);
    for (int i = 0; i < maxParts; i++)
    {
      int num1 = i < parts1.Length && int.TryParse(parts1[i], out int n1) ? n1 : 0;
      int num2 = i < parts2.Length && int.TryParse(parts2[i], out int n2) ? n2 : 0;

      if (num1 != num2)
      {
        return num1.CompareTo(num2);
      }
    }

    return 0;
  }

  /// <summary>
  /// Compares prerelease labels following SemVer rules.
  /// Segments are compared: numeric segments numerically, others lexicographically.
  /// </summary>
  private static int ComparePrereleaseLabels(string prerelease1, string prerelease2)
  {
    string[] segments1 = prerelease1.Split('.');
    string[] segments2 = prerelease2.Split('.');

    int maxSegments = Math.Max(segments1.Length, segments2.Length);
    for (int i = 0; i < maxSegments; i++)
    {
      // Missing segments are considered "less than" present ones
      if (i >= segments1.Length)
      {
        return -1;
      }

      if (i >= segments2.Length)
      {
        return 1;
      }

      string seg1 = segments1[i];
      string seg2 = segments2[i];

      bool isNum1 = int.TryParse(seg1, out int num1);
      bool isNum2 = int.TryParse(seg2, out int num2);

      if (isNum1 && isNum2)
      {
        // Both numeric - compare numerically
        int numCompare = num1.CompareTo(num2);
        if (numCompare != 0)
        {
          return numCompare;
        }
      }
      else if (isNum1)
      {
        // Numeric < non-numeric in SemVer
        return -1;
      }
      else if (isNum2)
      {
        // Non-numeric > numeric in SemVer
        return 1;
      }
      else
      {
        // Both non-numeric - compare lexicographically
        int strCompare = StringComparer.Ordinal.Compare(seg1, seg2);
        if (strCompare != 0)
        {
          return strCompare;
        }
      }
    }

    return 0;
  }
}
