#region Purpose
// Pure release-gate assertion: the tag that triggered a release must equal
// "v" + <Version> from source/Directory.Build.props (task 458-003, convention rule 6).
// Pure function so the matrix is unit-testable; env/git access stays in the caller.
#endregion

namespace DevCli;

public static class TagAssertion
{
  public static TagAssertionResult Validate(string? refName, string? propsVersion)
  {
    if (string.IsNullOrWhiteSpace(propsVersion))
    {
      return new TagAssertionResult(false, null, "could not read <Version> from source/Directory.Build.props");
    }

    string expectedTag = $"v{propsVersion}";

    if (string.IsNullOrWhiteSpace(refName))
    {
      return new TagAssertionResult(false, expectedTag, $"GITHUB_REF_NAME is not set; expected release tag '{expectedTag}'");
    }

    if (!string.Equals(refName, expectedTag, StringComparison.Ordinal))
    {
      return new TagAssertionResult(false, expectedTag, $"release tag '{refName}' does not match expected '{expectedTag}' from source/Directory.Build.props");
    }

    return new TagAssertionResult(true, expectedTag, null);
  }
}

public sealed record TagAssertionResult(bool IsValid, string? ExpectedTag, string? Error);
