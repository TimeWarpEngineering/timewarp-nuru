#region Purpose
// Honest prerelease-increment distance between the version in source and the
// latest published NuGet version, for check-version's advisory warning
// (kanban task 456).
#endregion
#region Design
// Distance is reported only when it is a single number: major.minor.patch
// match (NuGet core normalization, so 2.0 == 2.0.0) AND the prerelease is
// exactly {label}.{number} on both sides AND the labels match
// (case-insensitive). beta.9 → beta.14 is 5. A core change, a different
// label, a label with no number, extra prerelease identifiers, a stable
// release, or no latest version returns null — callers print both versions
// and skip the distance line rather than inventing a metric.
//
// The warning count is distance minus one. The source version is the one
// being released; the intermediates (beta.10 through beta.13 when source is
// beta.14 and latest is beta.9) are the numbers that were bumped and never
// published. Distance 1 is a normal bump and produces no warning. Distance
// greater than 1 warns. --strict turns that warning into a non-zero exit;
// the predicate lives here so the command and the tests share one rule.
// A source version behind the latest published prerelease is a negative
// distance ("behind"), not a skipped-release warning.
// Numeric identifiers that do not fit in int (date-stamped ids) return null
// so the gate skips the line instead of overflowing.
#endregion

namespace DevCli;

using System.Globalization;

/// <summary>
/// Prerelease-increment distance between a source version and the latest
/// published version. See the Design region for which shapes return a distance.
/// </summary>
public static class PrereleaseDistance
{
  /// <summary>
  /// Returns how many prerelease increments <paramref name="sourceVersion"/> is
  /// ahead of <paramref name="latestVersion"/> (negative when source is behind).
  /// Returns <see langword="null"/> when the pair is not an honest
  /// label-plus-number gap, including when <paramref name="latestVersion"/> is
  /// missing.
  /// </summary>
  public static int? TryGetIncrements(string sourceVersion, string? latestVersion)
  {
    ArgumentNullException.ThrowIfNull(sourceVersion);

    if (string.IsNullOrWhiteSpace(latestVersion))
    {
      return null;
    }

    if (!TrySplitRelease(sourceVersion, out string sourceCore, out string sourcePre))
    {
      return null;
    }

    if (!TrySplitRelease(latestVersion, out string latestCore, out string latestPre))
    {
      return null;
    }

    if (NuGetVersionService.CompareVersions(sourceCore, latestCore) != 0)
    {
      return null;
    }

    if (!TryParseLabelAndNumber(sourcePre, out string sourceLabel, out int sourceNumber))
    {
      return null;
    }

    if (!TryParseLabelAndNumber(latestPre, out string latestLabel, out int latestNumber))
    {
      return null;
    }

    if (!string.Equals(sourceLabel, latestLabel, StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    return sourceNumber - latestNumber;
  }

  /// <summary>
  /// True when <paramref name="increments"/> is a skipped-release gap
  /// (more than one prerelease increment ahead). <c>--strict</c> exits
  /// non-zero for that gap.
  /// </summary>
  public static bool IsStrictFailure(int? increments) => increments is > 1;

  /// <summary>
  /// Distance line, e.g. <c>Source is 5 prerelease increments ahead of v2.0.0-beta.9</c>.
  /// Build metadata on <paramref name="latestVersion"/> is omitted.
  /// </summary>
  public static string FormatDistanceLine(int increments, string latestVersion)
  {
    ArgumentNullException.ThrowIfNull(latestVersion);

    string display = StripBuildMetadata(latestVersion);
    int magnitude = Math.Abs(increments);
    string noun = magnitude == 1 ? "prerelease increment" : "prerelease increments";
    string relation = increments < 0 ? "behind" : "ahead of";
    return $"Source is {magnitude} {noun} {relation} v{display}";
  }

  /// <summary>
  /// Skipped-release warning for a distance greater than 1, or
  /// <see langword="null"/> otherwise. The count is the intermediate bumps
  /// (distance minus the source version being released).
  /// </summary>
  public static string? FormatSkippedReleaseWarning(int increments)
  {
    if (!IsStrictFailure(increments))
    {
      return null;
    }

    int skipped = increments - 1;
    return $"{skipped} version(s) were bumped but never released — was a release step skipped?";
  }

  private static bool TrySplitRelease(string version, out string core, out string prerelease)
  {
    core = "";
    prerelease = "";

    string noBuild = StripBuildMetadata(version);
    if (noBuild.Length == 0)
    {
      return false;
    }

    int dash = noBuild.IndexOf('-');
    if (dash < 0)
    {
      core = noBuild;
      return IsDottedNumericCore(core);
    }

    if (dash == 0 || dash == noBuild.Length - 1)
    {
      return false;
    }

    core = noBuild[..dash];
    prerelease = noBuild[(dash + 1)..];
    return IsDottedNumericCore(core);
  }

  private static string StripBuildMetadata(string version)
  {
    int plus = version.IndexOf('+');
    return plus >= 0 ? version[..plus] : version;
  }

  private static bool IsDottedNumericCore(string core)
  {
    if (core.Length == 0)
    {
      return false;
    }

    string[] parts = core.Split('.');
    foreach (string part in parts)
    {
      if (part.Length == 0 || !IsAsciiDigits(part))
      {
        return false;
      }
    }

    return true;
  }

  private static bool TryParseLabelAndNumber(string prerelease, out string label, out int number)
  {
    label = "";
    number = 0;

    if (prerelease.Length == 0)
    {
      return false;
    }

    string[] identifiers = prerelease.Split('.');
    if (identifiers.Length != 2)
    {
      return false;
    }

    if (!IsPrereleaseLabel(identifiers[0]) || !TryParsePrereleaseNumber(identifiers[1], out number))
    {
      return false;
    }

    label = identifiers[0];
    return true;
  }

  private static bool IsPrereleaseLabel(string identifier)
  {
    if (identifier.Length == 0)
    {
      return false;
    }

    bool hasLetter = false;
    foreach (char c in identifier)
    {
      if (c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'))
      {
        hasLetter = true;
      }
      else if (c is (>= '0' and <= '9') or '-')
      {
        continue;
      }
      else
      {
        return false;
      }
    }

    return hasLetter;
  }

  private static bool TryParsePrereleaseNumber(string identifier, out int number)
  {
    number = 0;
    if (!IsAsciiDigits(identifier))
    {
      return false;
    }

    // Leading zeros are numerically insignificant (beta.007 == beta.7).
    // int.MaxValue is the largest gap this gate will state; wider identifiers
    // (date stamps) return null so the caller skips the distance line.
    string trimmed = identifier.TrimStart('0');
    if (trimmed.Length == 0)
    {
      number = 0;
      return true;
    }

    return int.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out number);
  }

  private static bool IsAsciiDigits(string identifier)
  {
    if (identifier.Length == 0)
    {
      return false;
    }

    foreach (char c in identifier)
    {
      if (c is < '0' or > '9')
      {
        return false;
      }
    }

    return true;
  }
}
