namespace TimeWarp.Nuru.Mcp.Tools;

using System.ComponentModel;
using TimeWarp.Nuru.Mcp.Services;

/// <summary>
/// MCP tool that provides information about endpoints in TimeWarp.Nuru.
/// Requires internet access to fetch documentation from GitHub.
/// </summary>
internal sealed class GetEndpointTool
{
  private const string DocPath = "documentation/user/features/endpoints.md";
  private const string CacheCategory = "endpoints";

  [McpServerTool]
  [Description("Get information about TimeWarp.Nuru endpoints ([NuruRoute])")]
  public static async Task<string> GetEndpointInfoAsync(
      [Description("Force refresh from GitHub, bypassing cache")] bool forceRefresh = false)
  {
    string? content = await GitHubCacheService.FetchAsync(DocPath, CacheCategory, forceRefresh);

    if (content is null)
    {
      return "❌ **Internet access required.** Unable to fetch endpoints documentation from GitHub. " +
             "Please check your network connection and try again.";
    }

    return content;
  }
}
