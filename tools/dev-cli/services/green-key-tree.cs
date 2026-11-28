#region Purpose
// Turns `git ls-tree -r HEAD` output into a green-key manifest and key.
#endregion

#region Design
// Blob and submodule (commit) entries count. global.json, the workflow file, and .github/scripts
// go to their own labeled lines. The rest go through GreenKeyManifest.IsCodeInputPath.
// This type does not run git. The ledger does, and passes the text here, so the rules stay testable.
#endregion

namespace DevCli;

/// <summary>Parses a committed tree into a green key.</summary>
internal static class GreenKeyTree
{
  /// <summary>Reads <paramref name="lsTree"/> (`git ls-tree -r HEAD`) into a key. Never fails on text shape.</summary>
  internal static GreenKeyComputation FromLsTree(
    string? lsTree,
    string sdkVersion,
    string runnerImage,
    IReadOnlyList<string> imageLines)
  {
    ArgumentNullException.ThrowIfNull(sdkVersion);
    ArgumentNullException.ThrowIfNull(runnerImage);
    ArgumentNullException.ThrowIfNull(imageLines);

    string? globalJson = null;
    string? workflow = null;
    List<GreenKeyEntry> scripts = [];
    List<GreenKeyEntry> tree = [];
    string text = (lsTree ?? "").Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    foreach (string raw in text.Split('\n'))
    {
      if (raw.Length == 0)
      {
        continue;
      }

      int tab = raw.IndexOf('\t', StringComparison.Ordinal);
      if (tab <= 0 || tab == raw.Length - 1)
      {
        continue;
      }

      string[] metaParts = raw[..tab].Split(' ');
      if (metaParts.Length < 3)
      {
        continue;
      }

      string type = metaParts[1];
      string sha = metaParts[2];
      string path = raw[(tab + 1)..];
      if (type is not ("blob" or "commit"))
      {
        continue;
      }

      if (string.Equals(path, "global.json", StringComparison.Ordinal))
      {
        globalJson = sha;
        continue;
      }

      if (string.Equals(path, GreenKeyManifest.WorkflowPath, StringComparison.Ordinal))
      {
        workflow = sha;
        continue;
      }

      if (path.StartsWith(GreenKeyManifest.ScriptsPrefix, StringComparison.Ordinal))
      {
        scripts.Add(new GreenKeyEntry(path, sha));
        continue;
      }

      if (GreenKeyManifest.IsCodeInputPath(path))
      {
        tree.Add(new GreenKeyEntry(path, sha));
      }
    }

    GreenKeyParts parts = new(
      sdkVersion,
      globalJson,
      workflow,
      scripts,
      runnerImage,
      imageLines,
      tree);
    string manifest = GreenKeyManifest.Build(parts);
    return new GreenKeyComputation(GreenKeyManifest.Hash(manifest), manifest, tree.Count);
  }
}

/// <summary>A parsed tree. <c>TreeCount</c> is the code-input lines, not the explicit component lines.</summary>
internal sealed record GreenKeyComputation(string Key, string Manifest, int TreeCount);
