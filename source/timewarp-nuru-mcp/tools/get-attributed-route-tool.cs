namespace TimeWarp.Nuru.Mcp.Tools;

using System.ComponentModel;
using TimeWarp.Nuru.Mcp.Services;

/// <summary>
/// MCP tool that provides information about attributed routes in TimeWarp.Nuru.
/// </summary>
internal sealed class GetAttributedRouteTool
{
  private const string DocPath = "documentation/developer/reference/attributed-routes.md";
  private const string CacheCategory = "attributed-routes";

  [McpServerTool]
  [Description("Get information about TimeWarp.Nuru attributed routes ([NuruRoute])")]
  public static async Task<string> GetAttributedRouteInfoAsync(
      [Description("Force refresh from GitHub, bypassing cache")] bool forceRefresh = false)
  {
    string? content = await GitHubCacheService.FetchAsync(DocPath, CacheCategory, forceRefresh);
    return content ?? GetAttributedRouteOverviewFallback();
  }

  private static string GetAttributedRouteOverviewFallback()
  {
    return """
            # Attributed Routes in TimeWarp.Nuru

            The `[NuruRoute]` attribute enables auto-discovery of routes through source
            generation. This pattern is ideal for larger applications where you want
            clean separation between command definitions and their handlers.

            ## Basic Structure

            ```csharp
            [NuruRoute("pattern {param}", Description = "...")]
            public sealed class MyEndpoint
            {
              // Route parameters become properties
              public required string Param { get; init; }

              // Nested Handler class with DI support
              internal sealed class Handler(IMyService service)
              {
                public ValueTask<Unit> Handle(MyEndpoint request, CancellationToken ct)
                {
                  // Implementation
                  return ValueTask.FromResult(Unit.Value);
                }
              }
            }
            ```

            ## Key Features

            ### Auto-Discovery
            Routes are discovered at compile time via source generation. Register with:

            ```csharp
            NuruApp.CreateBuilder()
              .DiscoverEndpoints()
              .Build();
            ```

            ### Dependency Injection
            The Handler class supports constructor injection:

            ```csharp
            internal sealed class Handler(ILogger logger, IDatabase db, ITerminal terminal)
            ```

            ### Filtered Behaviors
            Implement marker interfaces to opt-in to behaviors:

            ```csharp
            [NuruRoute("admin delete {id}")]
            public sealed class DeleteEndpoint : IRequireAuth, IAuditable
            ```

            ### Return Types

            | Return Type | Exit Code |
            |-------------|-----------|
            | `ValueTask<Unit>` | 0 (success) |
            | `ValueTask<int>` | Custom exit code |
            | `ValueTask<T>` | 0, result serialized |
            | `void` | 0 (success) |
            | `int` | Custom exit code |

            Use the `get_example("attributed-routes")` tool for complete code samples.
            """;
  }
}
