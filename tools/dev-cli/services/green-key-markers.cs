#region Purpose
// Green markers: the artifact name for a key, the marker file, and picking an earlier green run.
#endregion

#region Design
// Markers are GitHub Actions artifacts named green-key-v1-&lt;key&gt;. Artifacts are repo-scoped, so a master
// push can see a pull request marker. The Actions cache is branch-scoped and would miss that case.
// A run step does not receive ACTIONS_RUNTIME_TOKEN, so dev workflow does not call the artifact service.
// After a same-repo full success it writes artifacts/green-key/marker.json and appends
// green_key_artifact=&lt;name&gt; to GITHUB_OUTPUT. The workflow upload step publishes that file.
// Fork pull requests must not stage a marker. A path-filter skip did not run tests, so it does not stage.
#endregion

namespace DevCli;

/// <summary>Green marker artifacts for a key.</summary>
internal static class GreenKeyMarkers
{
  /// <summary>Marker file the upload step publishes. Gitignored under <c>artifacts/</c>.</summary>
  internal const string MarkerRelativePath = "artifacts/green-key/marker.json";

  /// <summary>Step output the workflow's upload step reads. Set only when a marker is staged.</summary>
  internal const string ArtifactOutputName = "green_key_artifact";

  /// <summary>
  /// jq projection for one artifact row. <c>head_branch</c> is the short branch name
  /// (<c>master</c>, not <c>refs/heads/master</c>). Carry probes keep master rows.
  /// </summary>
  internal const string ArtifactListJq =
    ".artifacts[] | {id,expired,created_at,run_id:.workflow_run.id,head_sha:.workflow_run.head_sha,head_branch:.workflow_run.head_branch}";

  /// <summary>Artifact name for a key. Repo-scoped lookup uses this exact name.</summary>
  internal static string ArtifactName(string key)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    return GreenKeyManifest.FormatId + "-" + key.ToLowerInvariant();
  }

  /// <summary>One artifact list row. <paramref name="jsonLine"/> is one object from <see cref="ArtifactListJq"/>.</summary>
  internal static bool TryParseArtifactRow(string jsonLine, out GreenKeyArtifactRow row)
  {
    row = default;
    if (string.IsNullOrWhiteSpace(jsonLine))
    {
      return false;
    }

    try
    {
      using JsonDocument document = JsonDocument.Parse(jsonLine);
      JsonElement root = document.RootElement;
      if (!root.TryGetProperty("id", out JsonElement idElement) || !idElement.TryGetInt64(out long id))
      {
        return false;
      }

      bool expired = root.TryGetProperty("expired", out JsonElement expiredElement)
        && expiredElement.ValueKind == JsonValueKind.True;
      string? createdAt = GreenKeyEvent.JsonText(root, "created_at");
      long? runId = null;
      if (root.TryGetProperty("run_id", out JsonElement runElement)
        && runElement.ValueKind == JsonValueKind.Number
        && runElement.TryGetInt64(out long parsedRun))
      {
        runId = parsedRun;
      }

      row = new GreenKeyArtifactRow(
        id,
        expired,
        createdAt,
        runId,
        GreenKeyEvent.JsonText(root, "head_sha"),
        GreenKeyEvent.JsonText(root, "head_branch"));
      return true;
    }
    catch (JsonException)
    {
      return false;
    }
  }

  /// <summary>Earliest non-expired artifact. Null when none remain.</summary>
  internal static GreenKeyMarker? SelectMarker(string key, IEnumerable<GreenKeyArtifactRow> rows)
  {
    ArgumentNullException.ThrowIfNull(key);
    ArgumentNullException.ThrowIfNull(rows);
    GreenKeyArtifactRow? best = null;
    DateTimeOffset bestCreated = DateTimeOffset.MaxValue;
    foreach (GreenKeyArtifactRow row in rows)
    {
      if (row.Expired)
      {
        continue;
      }

      DateTimeOffset created = DateTimeOffset.MaxValue;
      if (row.CreatedAt is not null
        && DateTimeOffset.TryParse(row.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsed))
      {
        created = parsed;
      }

      if (best is null || created < bestCreated || (created == bestCreated && row.ArtifactId < best.Value.ArtifactId))
      {
        best = row;
        bestCreated = created;
      }
    }

    if (best is null)
    {
      return null;
    }

    GreenKeyArtifactRow chosen = best.Value;
    return new GreenKeyMarker(key, chosen.RunId ?? 0, chosen.Commit ?? "", chosen.ArtifactId);
  }

  /// <summary>JSON document stored at <see cref="MarkerRelativePath"/>.</summary>
  internal static string MarkerDocument(string key, string runId, string commit)
  {
    ArgumentNullException.ThrowIfNull(key);
    ArgumentNullException.ThrowIfNull(runId);
    ArgumentNullException.ThrowIfNull(commit);
    using MemoryStream stream = new();
    using (Utf8JsonWriter writer = new(stream))
    {
      writer.WriteStartObject();
      writer.WriteString("format", GreenKeyManifest.FormatId);
      writer.WriteString("key", key);
      writer.WriteString("run", runId);
      writer.WriteString("commit", commit);
      writer.WriteEndObject();
    }

    return Encoding.UTF8.GetString(stream.ToArray());
  }

  /// <summary>One <c>GITHUB_OUTPUT</c> line. The workflow upload step reads this name.</summary>
  internal static string ArtifactOutputLine(string key)
  {
    ArgumentNullException.ThrowIfNull(key);
    return ArtifactOutputName + "=" + ArtifactName(key);
  }
}

/// <summary>A previous green artifact for this key.</summary>
internal sealed record GreenKeyMarker(string Key, long RunId, string Commit, long ArtifactId);

/// <summary>One row from the artifact list.</summary>
internal readonly record struct GreenKeyArtifactRow(
  long ArtifactId,
  bool Expired,
  string? CreatedAt,
  long? RunId,
  string? Commit,
  string? HeadBranch = null);
