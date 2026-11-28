#region Purpose
// The green-key-v1 manifest: which tracked paths are inputs, the line layout, and its hash.
#endregion

#region Design
// Format id green-key-v1 is the first line. A layout change bumps that id, so old markers cannot hit.
// The manifest is UTF-8, LF, one fact per line, then SHA-256 hex (lowercase). Same line order as
// timewarp-ganda: sdk, global.json, the workflow file, .github/scripts, the runner, container image
// digests, then the code-input tree.
// Code inputs are the committed tree minus paths no nuru job reads. Ganda's exclusions apply
// (kanban/, documentation/, .agent/, skills/). Nuru also skips .claude/, assets/mascot/,
// changelog.md, and assets/timewarp-nuru-avatar.svg: paths-ignore already treats those as non-code,
// and no job packs or lints them. readme.md and assets/logo.png stay inputs because the packages pack them.
// global.json, the workflow file, and .github/scripts are their own lines, not tree lines.
#endregion

namespace DevCli;

/// <summary>Versioned manifest layout and content key for <c>dev workflow</c>.</summary>
internal static class GreenKeyManifest
{
  /// <summary>First line of the manifest. Bump it when the line layout changes.</summary>
  internal const string FormatId = "green-key-v1";

  /// <summary>Workflow file hashed on its own line.</summary>
  internal const string WorkflowPath = ".github/workflows/workflow.yml";

  /// <summary>Scripts the workflow calls. Hashed on <c>script</c> lines.</summary>
  internal const string ScriptsPrefix = ".github/scripts/";

  private static readonly string[] ExcludedPrefixes =
  [
    "kanban/",
    "documentation/",
    ".agent/",
    ".claude/",
    "skills/",
    "assets/mascot/"
  ];

  private static readonly string[] ExcludedFiles =
  [
    "changelog.md",
    "assets/timewarp-nuru-avatar.svg"
  ];

  /// <summary>
  /// True when <paramref name="relativePath"/> is a code-input tree entry.
  /// Explicit components (global.json, the workflow file, scripts) return false;
  /// they are still hashed, on their own lines.
  /// </summary>
  internal static bool IsCodeInputPath(string relativePath)
  {
    string? path = Normalize(relativePath);
    if (path is null)
    {
      return false;
    }

    if (IsExcluded(path) || IsExplicitComponent(path))
    {
      return false;
    }

    return true;
  }

  /// <summary>Builds the canonical manifest. Line order is part of the key.</summary>
  internal static string Build(GreenKeyParts parts)
  {
    ArgumentNullException.ThrowIfNull(parts);
    StringBuilder builder = new();
    builder.Append(FormatId).Append('\n');
    builder.Append("sdk ").Append(Sdk(parts.SdkVersion)).Append('\n');
    builder.Append("global.json ").Append(parts.GlobalJsonBlob ?? "missing").Append('\n');
    builder.Append("workflow ").Append(WorkflowPath).Append(' ').Append(parts.WorkflowBlob ?? "missing").Append('\n');
    foreach (GreenKeyEntry script in Sorted(parts.Scripts))
    {
      builder.Append("script ").Append(script.Path).Append(' ').Append(script.Blob).Append('\n');
    }

    builder.Append("runner ").Append(OneLine(string.IsNullOrWhiteSpace(parts.RunnerImage) ? "unknown" : parts.RunnerImage)).Append('\n');
    if (parts.ImageLines.Count == 0)
    {
      builder.Append("images none\n");
    }
    else
    {
      foreach (string image in parts.ImageLines.Order(StringComparer.Ordinal))
      {
        builder.Append("image ").Append(OneLine(image)).Append('\n');
      }
    }

    foreach (GreenKeyEntry entry in Sorted(parts.Tree))
    {
      builder.Append("tree ").Append(entry.Path).Append(' ').Append(entry.Blob).Append('\n');
    }

    return builder.ToString();
  }

  /// <summary>SHA-256 hex of the manifest, lowercase.</summary>
  internal static string Hash(string manifest)
  {
    ArgumentNullException.ThrowIfNull(manifest);
    return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(manifest)));
  }

  /// <summary>True when <paramref name="key"/> is a 64-character hex digest.</summary>
  internal static bool IsKey(string? key)
  {
    if (key is null || key.Length != 64)
    {
      return false;
    }

    foreach (char character in key)
    {
      bool hex = character is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');
      if (!hex)
      {
        return false;
      }
    }

    return true;
  }

  /// <summary>Drops line breaks so a value stays on one manifest or log line.</summary>
  internal static string OneLine(string value) =>
    value.Replace("\r", "", StringComparison.Ordinal).Replace("\n", "", StringComparison.Ordinal).Trim();

  private static string Sdk(string? sdkVersion)
  {
    string text = OneLine(sdkVersion ?? "");
    return text.Length == 0 ? "unknown" : text;
  }

  private static bool IsExcluded(string path)
  {
    foreach (string file in ExcludedFiles)
    {
      if (string.Equals(path, file, StringComparison.Ordinal))
      {
        return true;
      }
    }

    foreach (string prefix in ExcludedPrefixes)
    {
      if (path.StartsWith(prefix, StringComparison.Ordinal))
      {
        return true;
      }

      string bare = prefix.TrimEnd('/');
      if (string.Equals(path, bare, StringComparison.Ordinal))
      {
        return true;
      }
    }

    return false;
  }

  private static bool IsExplicitComponent(string path) =>
    string.Equals(path, "global.json", StringComparison.Ordinal)
    || string.Equals(path, WorkflowPath, StringComparison.Ordinal)
    || path.StartsWith(ScriptsPrefix, StringComparison.Ordinal);

  private static string? Normalize(string relativePath)
  {
    if (string.IsNullOrWhiteSpace(relativePath))
    {
      return null;
    }

    string path = relativePath.Replace('\\', '/').Trim();
    if (path.StartsWith("./", StringComparison.Ordinal))
    {
      path = path[2..];
    }

    return path.Length == 0 ? null : path;
  }

  private static List<GreenKeyEntry> Sorted(IReadOnlyList<GreenKeyEntry> entries)
  {
    List<GreenKeyEntry> copy = [.. entries];
    copy.Sort(static (left, right) => string.CompareOrdinal(left.Path, right.Path));
    return copy;
  }
}

/// <summary>One hashed path.</summary>
internal readonly record struct GreenKeyEntry(string Path, string Blob);

/// <summary>Inputs to <see cref="GreenKeyManifest.Build"/>.</summary>
internal sealed record GreenKeyParts(
  string SdkVersion,
  string? GlobalJsonBlob,
  string? WorkflowBlob,
  IReadOnlyList<GreenKeyEntry> Scripts,
  string RunnerImage,
  IReadOnlyList<string> ImageLines,
  IReadOnlyList<GreenKeyEntry> Tree);
