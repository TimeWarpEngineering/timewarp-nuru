namespace TimeWarp.Nuru;

using TimeWarp.Nuru.McpHost;

/// <summary>
/// Builder extension that adds the built-in <c>mcp</c> route.
/// </summary>
/// <remarks>
/// This method is interpreted by the source generator at compile time, like
/// <c>AddCheckUpdatesRoute()</c>. The generated route runs <see cref="NuruMcpServer.RunStdioAsync"/>.
/// This stub exists so the call compiles.
/// </remarks>
public static class NuruMcpAppBuilderExtensions
{
  /// <summary>
  /// Adds an <c>mcp</c> route that serves the app's routes as MCP tools over stdio.
  /// </summary>
  /// <typeparam name="TBuilder">The builder type.</typeparam>
  /// <param name="builder">The NuruAppBuilder instance.</param>
  /// <param name="instructions">Text appended to the server instructions after the app description.</param>
  /// <param name="groupFilter">Dot-separated group prefix that limits the published tools.</param>
  /// <param name="contentFormat">What a <c>tools/call</c> result carries.</param>
  /// <returns>The builder for chaining.</returns>
  /// <remarks>
  /// The arguments must be compile-time constants: the generator copies them into the route.
  /// Routes the app maps itself are matched first, so a user route <c>mcp</c> (or a catch-all)
  /// takes precedence over the built-in.
  /// </remarks>
  public static TBuilder AddMcpRoute<TBuilder>(
    this TBuilder builder,
    string? instructions = null,
    string? groupFilter = null,
    NuruMcpContentFormat contentFormat = NuruMcpContentFormat.Text)
    where TBuilder : NuruAppBuilder
  {
    // Interpreted by the source generator; the arguments are read at compile time.
    _ = instructions;
    _ = groupFilter;
    _ = contentFormat;
    return builder;
  }
}
