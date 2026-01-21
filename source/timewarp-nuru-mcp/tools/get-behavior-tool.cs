namespace TimeWarp.Nuru.Mcp.Tools;

using System.ComponentModel;
using TimeWarp.Nuru.Mcp.Services;

/// <summary>
/// MCP tool that provides information about pipeline behaviors in TimeWarp.Nuru.
/// Requires internet access to fetch documentation from GitHub.
/// </summary>
internal sealed class GetBehaviorTool
{
  private const string DocPath = "documentation/user/features/pipeline-behaviors.md";
  private const string CacheCategory = "behaviors";

  [McpServerTool]
  [Description("Get information about TimeWarp.Nuru pipeline behaviors")]
  public static async Task<string> GetBehaviorInfoAsync(
      [Description("Force refresh from GitHub, bypassing cache")] bool forceRefresh = false)
  {
    string? content = await GitHubCacheService.FetchAsync(DocPath, CacheCategory, forceRefresh);

    if (content is null)
    {
      return "❌ **Internet access required.** Unable to fetch pipeline behaviors documentation from GitHub. " +
             "Please check your network connection and try again.";
    }

    return content;
  }
}
