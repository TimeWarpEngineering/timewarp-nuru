namespace TimeWarp.Nuru.Mcp.Tools;

using System.ComponentModel;
using TimeWarp.Nuru.Mcp.Services;

/// <summary>
/// MCP tool that provides information about pipeline behaviors in TimeWarp.Nuru.
/// </summary>
internal sealed class GetBehaviorTool
{
  private const string DocPath = "documentation/developer/reference/pipeline-behaviors.md";
  private const string CacheCategory = "behaviors";

  [McpServerTool]
  [Description("Get information about TimeWarp.Nuru pipeline behaviors")]
  public static async Task<string> GetBehaviorInfoAsync(
      [Description("Force refresh from GitHub, bypassing cache")] bool forceRefresh = false)
  {
    string? content = await GitHubCacheService.FetchAsync(DocPath, CacheCategory, forceRefresh);
    return content ?? GetBehaviorOverviewFallback();
  }

  private static string GetBehaviorOverviewFallback()
  {
    return """
            # Pipeline Behaviors in TimeWarp.Nuru

            Pipeline behaviors provide a middleware-like pattern for adding cross-cutting
            concerns to your CLI routes. They wrap route handlers and can execute code
            before and after the handler runs.

            ## Behavior Types

            ### Global Behaviors: `INuruBehavior<IRouteContext>`

            Apply to ALL routes in the application. Use for:
            - Logging
            - Performance monitoring
            - Exception handling
            - Telemetry

            ### Filtered Behaviors: `INuruBehavior<TFilter>`

            Apply only to routes that implement the marker interface `TFilter`. Use for:
            - Authentication/Authorization
            - Auditing
            - Validation
            - Rate limiting

            ## Registration

            ```csharp
            NuruApp app = NuruApp.CreateBuilder()
              .AddBehavior(typeof(LoggingBehavior))      // Global
              .AddBehavior(typeof(AuthBehavior))         // Filtered: IRequireAuth
              .Map("deploy {env}")
                .WithHandler(...)
                .Implements<IRequireAuth>()  // Opt-in to AuthBehavior
                .AsCommand()
                .Done()
              .Build();
            ```

            ## Execution Order

            Behaviors execute in registration order (first registered = outermost):

            1. First behavior's pre-handler code
            2. Second behavior's pre-handler code
            3. Route handler
            4. Second behavior's post-handler code
            5. First behavior's post-handler code

            Use `get_example("behaviors-basic")` and `get_example("behaviors-filtered")`
            for complete code samples.
            """;
  }
}
