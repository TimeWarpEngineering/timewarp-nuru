namespace TimeWarp.Nuru.Mcp.Tools;

/// <summary>
/// MCP tool for managing all TimeWarp.Nuru.Mcp caches.
/// </summary>
internal sealed class CacheManagementTool
{
  private static string BaseCacheDirectory => Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "TimeWarp.Nuru.Mcp",
      "cache"
  );

  private static readonly string[] CacheSubdirectories =
  [
    "examples",
    "attributed-routes",
    "behaviors",
    "type-converters",
    "error-handling"
  ];

  [McpServerTool]
  [Description("Clear all cached TimeWarp.Nuru examples")]
  public static string ClearCache()
  {
    try
    {
      int totalFiles = 0;
      List<string> clearedDirs = [];

      foreach (string subdir in CacheSubdirectories)
      {
        string cacheDir = Path.Combine(BaseCacheDirectory, subdir);
        if (Directory.Exists(cacheDir))
        {
          string[] files = Directory.GetFiles(cacheDir);
          foreach (string file in files)
          {
            File.Delete(file);
          }

          totalFiles += files.Length;

          if (files.Length > 0)
          {
            clearedDirs.Add($"  {subdir}: {files.Length} files");
          }
        }
      }

      if (totalFiles == 0)
      {
        return $"Cache is empty. Location: {BaseCacheDirectory}";
      }

      return $"Cache cleared successfully. Removed {totalFiles} files:\n" +
             string.Join("\n", clearedDirs);
    }
    catch (IOException ex)
    {
      return $"Error clearing cache: {ex.Message}";
    }
  }

  [McpServerTool]
  [Description("Get cache status and information")]
  public static string CacheStatus()
  {
    try
    {
      List<string> results = [$"Cache location: {BaseCacheDirectory}", ""];
      long grandTotalSize = 0;
      int grandTotalFiles = 0;

      foreach (string subdir in CacheSubdirectories)
      {
        string cacheDir = Path.Combine(BaseCacheDirectory, subdir);
        if (!Directory.Exists(cacheDir))
        {
          results.Add($"**{subdir}**: (no cache)");
          continue;
        }

        string[] cacheFiles = Directory.GetFiles(cacheDir, "*.cache");
        string[] mdFiles = Directory.GetFiles(cacheDir, "*.md");
        string[] allContentFiles = [.. cacheFiles, .. mdFiles];

        if (allContentFiles.Length == 0)
        {
          results.Add($"**{subdir}**: (empty)");
          continue;
        }

        List<string> entries = [];
        long totalSize = 0;

        foreach (string contentFile in allContentFiles)
        {
          FileInfo fileInfo = new(contentFile);
          totalSize += fileInfo.Length;

          string name = Path.GetFileNameWithoutExtension(contentFile);
          string metaFile = Path.Combine(cacheDir, $"{name}.meta");

          string status = "Valid";
          if (File.Exists(metaFile))
          {
            string metaContent = File.ReadAllText(metaFile);
            if (DateTime.TryParse(metaContent, out DateTime cachedTime))
            {
              TimeSpan age = DateTime.UtcNow - cachedTime;
              if (age >= TimeSpan.FromHours(1))
              {
                status = "Expired";
              }
              else
              {
                status = $"Valid ({Math.Round(60 - age.TotalMinutes)} min remaining)";
              }
            }
          }

          entries.Add($"    {name}: {fileInfo.Length:N0} bytes, {status}");
        }

        grandTotalSize += totalSize;
        grandTotalFiles += allContentFiles.Length;

        results.Add($"**{subdir}** ({allContentFiles.Length} files, {totalSize:N0} bytes):");
        results.AddRange(entries);
      }

      results.Add("");
      results.Add($"**Total**: {grandTotalFiles} files, {grandTotalSize:N0} bytes");

      return string.Join("\n", results);
    }
    catch (IOException ex)
    {
      return $"Error getting cache status: {ex.Message}";
    }
  }
}
