#region Purpose
// The runner and container-image lines of a green-key manifest.
#endregion

#region Design
// Runner image: GREEN_KEY_RUNNER_IMAGE when set. Otherwise the hosted ImageOS/ImageVersion, and the
// SHA-256 of /var/lib/dpkg/status when that file exists. The wslc image publishes no image id into
// the job, so the package-status hash is the image version there (task 539). A rebuild that changes
// no installed package does not bust the key.
// Container images: docker/podman/nerdctl pull lines and a workflow image: key. Nuru's workflow pulls
// none. An unresolved tag is recorded as tag@unresolved so a tag string change still busts the key.
#endregion

namespace DevCli;

/// <summary>Describes the runner image and the container images a workflow pulls.</summary>
internal static class GreenKeyRunnerImage
{
  private static readonly Regex ImagePull = new(
    @"^\s*(?:docker|podman|nerdctl)\s+pull\s+(?:--?\S+\s+)*(\S+)",
    RegexOptions.CultureInvariant | RegexOptions.Multiline,
    TimeSpan.FromSeconds(1));

  private static readonly Regex JobImage = new(
    @"^\s*image:\s*['""]?([^\s'""#]+)",
    RegexOptions.CultureInvariant | RegexOptions.Multiline,
    TimeSpan.FromSeconds(1));

  /// <summary>Hosted image version, dpkg status hash, or an explicit override.</summary>
  internal static string Describe(string? overrideImage, string? imageOs, string? imageVersion, string? dpkgStatusSha256)
  {
    if (!string.IsNullOrWhiteSpace(overrideImage))
    {
      return GreenKeyManifest.OneLine(overrideImage);
    }

    List<string> parts = [];
    if (!string.IsNullOrWhiteSpace(imageVersion))
    {
      string os = string.IsNullOrWhiteSpace(imageOs) ? "unknown-os" : GreenKeyManifest.OneLine(imageOs);
      parts.Add("hosted-image:" + os + ":" + GreenKeyManifest.OneLine(imageVersion));
    }

    if (!string.IsNullOrWhiteSpace(dpkgStatusSha256))
    {
      parts.Add("dpkg:" + GreenKeyManifest.OneLine(dpkgStatusSha256).ToLowerInvariant());
    }

    return parts.Count == 0 ? "unknown" : string.Join('|', parts);
  }

  /// <summary>Image references a workflow or script would pull, sorted and distinct.</summary>
  internal static IReadOnlyList<string> FindImagePulls(string text)
  {
    ArgumentNullException.ThrowIfNull(text);
    HashSet<string> found = [];
    Collect(ImagePull, text, found);
    Collect(JobImage, text, found);
    List<string> tags = [.. found];
    tags.Sort(StringComparer.Ordinal);
    return tags;
  }

  /// <summary><c>tag@digest</c> lines. A null digest becomes <c>tag@unresolved</c>.</summary>
  internal static IReadOnlyList<string> ImageLines(IReadOnlyList<string> tags, IReadOnlyDictionary<string, string?> digests)
  {
    ArgumentNullException.ThrowIfNull(tags);
    ArgumentNullException.ThrowIfNull(digests);
    List<string> lines = [];
    foreach (string tag in tags)
    {
      string digest = "unresolved";
      if (digests.TryGetValue(tag, out string? resolved) && !string.IsNullOrWhiteSpace(resolved))
      {
        digest = GreenKeyManifest.OneLine(resolved);
      }

      lines.Add(GreenKeyManifest.OneLine(tag) + "@" + digest);
    }

    lines.Sort(StringComparer.Ordinal);
    return lines;
  }

  private static void Collect(Regex pattern, string text, HashSet<string> found)
  {
    foreach (Match match in pattern.Matches(text))
    {
      if (match.Groups.Count < 2)
      {
        continue;
      }

      string tag = match.Groups[1].Value.Trim().Trim('\'', '"');
      if (tag.Length > 0)
      {
        found.Add(tag);
      }
    }
  }
}
