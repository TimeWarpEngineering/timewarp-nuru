namespace TimeWarp.Nuru.Mcp.Tools;

using System.ComponentModel;
using TimeWarp.Nuru.Mcp.Services;

/// <summary>
/// MCP tool that provides information about custom type converters in TimeWarp.Nuru.
/// Requires internet access to fetch documentation from GitHub.
/// </summary>
internal sealed class GetTypeConverterTool
{
  private const string DocPath = "documentation/user/reference/supported-types.md";
  private const string CacheCategory = "type-converters";

  [McpServerTool]
  [Description("Get information about TimeWarp.Nuru custom type converters")]
  public static async Task<string> GetTypeConverterInfoAsync(
      [Description("Force refresh from GitHub, bypassing cache")] bool forceRefresh = false)
  {
    string? content = await GitHubCacheService.FetchAsync(DocPath, CacheCategory, forceRefresh);

    if (content is null)
    {
      return "❌ **Internet access required.** Unable to fetch type converters documentation from GitHub. " +
             "Please check your network connection and try again.";
    }

    return content;
  }
}
