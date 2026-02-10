using System.Text.RegularExpressions;
using TimeWarp.Nuru;

public readonly record struct SemanticVersion : IComparable<SemanticVersion>
{
  public int Major { get; }
  public int Minor { get; }
  public int Patch { get; }
  public string? Prerelease { get; }
  public string? Build { get; }

  public SemanticVersion(int major, int minor, int patch, string? prerelease = null, string? build = null)
  {
    Major = major;
    Minor = minor;
    Patch = patch;
    Prerelease = prerelease;
    Build = build;
  }

  public SemanticVersion(string version)
  {
    Match match = Regex.Match(version, @"^(\d+)\.(\d+)\.(\d+)(?:-([\w.]+))?(?:\+([\w.]+))?$");
    if (!match.Success)
      throw new ArgumentException($"Invalid semantic version: {version}");

    Major = int.Parse(match.Groups[1].Value);
    Minor = int.Parse(match.Groups[2].Value);
    Patch = int.Parse(match.Groups[3].Value);
    Prerelease = match.Groups[4].Success ? match.Groups[4].Value : null;
    Build = match.Groups[5].Success ? match.Groups[5].Value : null;
  }

  public int CompareTo(SemanticVersion other)
  {
    int result = Major.CompareTo(other.Major);
    if (result != 0) return result;

    result = Minor.CompareTo(other.Minor);
    if (result != 0) return result;

    result = Patch.CompareTo(other.Patch);
    if (result != 0) return result;

    if (Prerelease == null && other.Prerelease != null) return 1;
    if (Prerelease != null && other.Prerelease == null) return -1;

    return string.Compare(Prerelease, other.Prerelease, StringComparison.Ordinal);
  }

  public override string ToString()
  {
    string result = $"{Major}.{Minor}.{Patch}";
    if (Prerelease != null) result += $"-{Prerelease}";
    if (Build != null) result += $"+{Build}";
    return result;
  }
}

public class SemanticVersionConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(SemanticVersion);
  public string? ConstraintAlias => "semver";

  public bool TryConvert(string value, out object? result)
  {
    try
    {
      result = new SemanticVersion(value);
      return true;
    }
    catch
    {
      result = null;
      return false;
    }
  }
}
