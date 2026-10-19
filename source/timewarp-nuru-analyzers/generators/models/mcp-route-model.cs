namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Design-time representation of an <c>AddMcpRoute()</c> call (package TimeWarp.Nuru.McpHost).
/// The generator emits a built-in <c>mcp</c> route that serves the app's routes as MCP tools over stdio.
/// </summary>
/// <param name="Instructions">App-provided text appended to the server instructions, or null.</param>
/// <param name="GroupFilter">Dot-separated group prefix that limits the published tools, or null.</param>
/// <param name="ContentFormat">Underlying value of <c>NuruMcpContentFormat</c> (0 text, 1 text plus JSON).</param>
public sealed record McpRouteModel(
  string? Instructions,
  string? GroupFilter,
  int ContentFormat);
