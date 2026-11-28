#region Purpose
// Whether a GitHub Actions event may look up or write a green marker, and the pull request fields the ledger reads.
#endregion

#region Design
// A candidate is a master push or a same-repo pull request. A fork pull request may look up and must not write.
// release, schedule, and workflow_dispatch never look up and never write, so a manual dispatch is always a full run.
// A pull_request run checks out the synthetic merge commit. The head commit, which carries the force trailer,
// is pull_request.head.sha.
#endregion

namespace DevCli;

/// <summary>Classifies workflow events for the green-key ledger.</summary>
internal static class GreenKeyEvent
{
  /// <summary>Classifies a GitHub Actions event for lookup and marker writes.</summary>
  internal static GreenKeyDisposition Classify(
    string? eventName,
    string? githubRef,
    string? repository,
    string? headRepositoryFullName,
    bool releaseMode,
    out string reason)
  {
    if (releaseMode || string.Equals(eventName, "release", StringComparison.Ordinal))
    {
      reason = "release";
      return GreenKeyDisposition.Excluded;
    }

    if (string.Equals(eventName, "schedule", StringComparison.Ordinal))
    {
      reason = "schedule";
      return GreenKeyDisposition.Excluded;
    }

    if (string.Equals(eventName, "workflow_dispatch", StringComparison.Ordinal))
    {
      reason = "workflow_dispatch";
      return GreenKeyDisposition.Excluded;
    }

    if (string.Equals(eventName, "push", StringComparison.Ordinal))
    {
      if (string.Equals(githubRef, "refs/heads/master", StringComparison.Ordinal))
      {
        reason = "master-push";
        return GreenKeyDisposition.Candidate;
      }

      reason = "non-master-push";
      return GreenKeyDisposition.Excluded;
    }

    if (string.Equals(eventName, "pull_request", StringComparison.Ordinal))
    {
      if (string.IsNullOrEmpty(repository) || string.IsNullOrEmpty(headRepositoryFullName))
      {
        reason = "pull-request-repo-unknown";
        return GreenKeyDisposition.Fork;
      }

      if (string.Equals(headRepositoryFullName, repository, StringComparison.Ordinal))
      {
        reason = "same-repo-pull-request";
        return GreenKeyDisposition.Candidate;
      }

      reason = "fork-pull-request";
      return GreenKeyDisposition.Fork;
    }

    reason = "not-a-candidate-event";
    return GreenKeyDisposition.Excluded;
  }

  /// <summary>Reads <c>pull_request.head.repo.full_name</c>. Null when the event has no head repo.</summary>
  internal static string? TryReadHeadRepository(string? json) => ReadHead(json, "repo");

  /// <summary>Reads <c>pull_request.head.sha</c>. Null when the event has no pull request head.</summary>
  internal static string? TryReadPullRequestHeadSha(string? json) => ReadHead(json, "sha");

  private static string? ReadHead(string? json, string property)
  {
    if (string.IsNullOrWhiteSpace(json))
    {
      return null;
    }

    try
    {
      using JsonDocument document = JsonDocument.Parse(json);
      if (!TryGetPullRequestHead(document.RootElement, out JsonElement head))
      {
        return null;
      }

      if (property == "repo")
      {
        if (!head.TryGetProperty("repo", out JsonElement repo) || repo.ValueKind != JsonValueKind.Object)
        {
          return null;
        }

        return JsonText(repo, "full_name");
      }

      return JsonText(head, property);
    }
    catch (JsonException)
    {
      return null;
    }
  }

  private static bool TryGetPullRequestHead(JsonElement root, out JsonElement head)
  {
    head = default;
    return root.ValueKind == JsonValueKind.Object
      && root.TryGetProperty("pull_request", out JsonElement pullRequest)
      && pullRequest.ValueKind == JsonValueKind.Object
      && pullRequest.TryGetProperty("head", out head)
      && head.ValueKind == JsonValueKind.Object;
  }

  internal static string? JsonText(JsonElement element, string name) =>
    element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
      ? value.GetString()
      : null;
}

/// <summary>Whether a run may write a green marker, may only look one up, or is excluded.</summary>
internal enum GreenKeyDisposition
{
  /// <summary>Same-repo master push or same-repo pull request. Lookup, and write after a full green run.</summary>
  Candidate,

  /// <summary>Fork pull request. Lookup, never write.</summary>
  Fork,

  /// <summary>Release, schedule, manual, or any other event. No lookup, no write.</summary>
  Excluded
}
