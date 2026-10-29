#region Purpose
// Finds the token NuGetVersionService sends to the org GitHub Packages feed
// (kanban task 536). The feed answers 401 to an anonymous request, even for a
// package id it does not have, so the publish gate needs a token to read it.
#endregion
#region Design
// Order: GITHUB_TOKEN, then GH_TOKEN, then `gh auth token`. Actions jobs pass
// the run token as GH_TOKEN; a local shell usually has only the gh login.
// Source names where the token came from, so a refusal can say which
// credential lacked read:packages without printing the token. A missing gh
// (Win32Exception), a failing `gh auth token` or blank output all mean "no
// token": the caller then refuses, it never treats the feed as empty.
#endregion

namespace DevCli;

public sealed record GitHubPackagesCredential(string Token, string Source)
{
  public const string GitHubTokenVariable = "GITHUB_TOKEN";
  public const string GhTokenVariable = "GH_TOKEN";
  public const string GhAuthTokenSource = "gh auth token";

  /// <summary>
  /// Shown in a credential's place by <see cref="ToString"/>, so a logged
  /// record never prints the token.
  /// </summary>
  public override string ToString() => $"GitHubPackagesCredential {{ Source = {Source} }}";

  /// <summary>
  /// Resolves the first non-blank token from <c>GITHUB_TOKEN</c>, <c>GH_TOKEN</c>
  /// and <c>gh auth token</c>, or <see langword="null"/> when none is available.
  /// </summary>
  public static async Task<GitHubPackagesCredential?> ResolveAsync(CancellationToken cancellationToken)
  {
    foreach (string variable in new[] { GitHubTokenVariable, GhTokenVariable })
    {
      string? value = Environment.GetEnvironmentVariable(variable);
      if (!string.IsNullOrWhiteSpace(value))
      {
        return new GitHubPackagesCredential(value.Trim(), variable);
      }
    }

    CommandOutput output;
    try
    {
      output = await Shell.Builder("gh")
        .WithArguments("auth", "token")
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);
    }
    catch (System.ComponentModel.Win32Exception)
    {
      return null;
    }

    string token = output.Stdout.Trim();
    return output.ExitCode == 0 && token.Length > 0
      ? new GitHubPackagesCredential(token, GhAuthTokenSource)
      : null;
  }
}
