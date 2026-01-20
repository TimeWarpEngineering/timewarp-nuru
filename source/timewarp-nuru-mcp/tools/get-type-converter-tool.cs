namespace TimeWarp.Nuru.Mcp.Tools;

using System.ComponentModel;
using TimeWarp.Nuru.Mcp.Services;

/// <summary>
/// MCP tool that provides information about custom type converters in TimeWarp.Nuru.
/// </summary>
internal sealed class GetTypeConverterTool
{
  private const string DocPath = "documentation/developer/reference/type-converters.md";
  private const string CacheCategory = "type-converters";

  [McpServerTool]
  [Description("Get information about TimeWarp.Nuru custom type converters")]
  public static async Task<string> GetTypeConverterInfoAsync(
      [Description("Force refresh from GitHub, bypassing cache")] bool forceRefresh = false)
  {
    string? content = await GitHubCacheService.FetchAsync(DocPath, CacheCategory, forceRefresh);
    return content ?? GetTypeConverterOverviewFallback();
  }

  private static string GetTypeConverterOverviewFallback()
  {
    return """
            # Custom Type Converters in TimeWarp.Nuru

            Type converters transform command-line string arguments into strongly-typed
            values for your route handlers. TimeWarp.Nuru provides built-in converters
            for common types and supports custom converters for domain-specific types.

            ## Built-in Type Converters

            TimeWarp.Nuru includes converters for:

            | Type | Constraint | Example |
            |------|------------|---------|
            | `int` | `:int` | `{count:int}` |
            | `long` | `:long` | `{id:long}` |
            | `double` | `:double` | `{rate:double}` |
            | `bool` | `:bool` | `{enabled:bool}` |
            | `Guid` | `:guid` | `{userId:guid}` |
            | `DateTime` | `:datetime` | `{date:datetime}` |
            | Enums | `:EnumName` | `{level:LogLevel}` |

            ## Implementing Custom Converters

            1. Implement `IRouteTypeConverter<T>`:

            ```csharp
            public sealed class MyTypeConverter : IRouteTypeConverter<MyType>
            {
              public bool TryConvert(string input, out MyType result)
              {
                // Parse input and set result
                // Return true if successful, false otherwise
              }
            }
            ```

            2. Register with the app builder:

            ```csharp
            NuruApp.CreateBuilder()
              .AddTypeConverter<MyType, MyTypeConverter>()
              // ...
            ```

            3. Use in route patterns:

            ```csharp
            .Map("command {param:MyType}")
              .WithHandler((MyType param) => ...)
            ```

            Use `get_example("custom-type-converter")` for a complete code sample.
            """;
  }
}
