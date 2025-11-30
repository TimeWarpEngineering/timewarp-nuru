namespace TimeWarp.Nuru.Mcp.Tools;

/// <summary>
/// MCP tool for managing the example cache.
/// </summary>
internal sealed class CacheManagementTool
{
  private static string CacheDirectory => Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "TimeWarp.Nuru.Mcp",
      "cache",
      "examples"
  );

  [McpServerTool]
  [Description("Clear all cached TimeWarp.Nuru examples")]
  public static string ClearCache()
  {
    try
    {
      if (Directory.Exists(CacheDirectory))
      {
        string[] files = Directory.GetFiles(CacheDirectory);
        int fileCount = files.Length;

        foreach (string file in files)
        {
          File.Delete(file);
        }

        return $"Cache cleared successfully. Removed {fileCount} files from:\n{CacheDirectory}";
      }

      return $"Cache directory does not exist: {CacheDirectory}";
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
      if (!Directory.Exists(CacheDirectory))
      {
        return $"Cache directory does not exist: {CacheDirectory}\nNo examples cached.";
      }

      string[] cacheFiles = Directory.GetFiles(CacheDirectory, "*.cache");
      string[] metaFiles = Directory.GetFiles(CacheDirectory, "*.meta");

      if (cacheFiles.Length == 0)
      {
        return $"Cache location: {CacheDirectory}\nNo examples cached.";
      }

      List<string> cachedExamples = [];
      long totalSize = 0;

      foreach (string cacheFile in cacheFiles)
      {
        FileInfo fileInfo = new(cacheFile);
        totalSize += fileInfo.Length;

        string name = Path.GetFileNameWithoutExtension(cacheFile);
        string metaFile = Path.Combine(CacheDirectory, $"{name}.meta");

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

        cachedExamples.Add($"  - {name}: {fileInfo.Length:N0} bytes, {status}");
      }

      return $"Cache location: {CacheDirectory}\n" +
             $"Total size: {totalSize:N0} bytes\n" +
             $"Cached examples ({cacheFiles.Length}):\n" +
             string.Join("\n", cachedExamples);
    }
    catch (IOException ex)
    {
      return $"Error getting cache status: {ex.Message}";
    }
  }
}
