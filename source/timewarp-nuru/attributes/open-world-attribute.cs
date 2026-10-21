namespace TimeWarp.Nuru;

/// <summary>
/// States whether an endpoint reaches outside the app: the web, a remote service, other people's data.
/// The <c>--capabilities</c> catalog carries it as <c>"openWorld"</c>, and an MCP host maps it to the
/// tool's <c>openWorldHint</c>.
/// </summary>
/// <remarks>
/// <c>[OpenWorld(false)]</c> marks a closed-world endpoint that only reads or changes local state, such
/// as a code-analysis query over a loaded solution. Nothing infers this hint. A route without the
/// attribute has no <c>openWorld</c> member in the catalog, and the MCP specification tells clients to
/// assume an unset <c>openWorldHint</c> is true.
/// </remarks>
/// <param name="isOpenWorld">False for an endpoint that only touches local state.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OpenWorldAttribute(bool isOpenWorld = true) : Attribute
{
  /// <summary>True when the endpoint reaches outside the app.</summary>
  public bool IsOpenWorld { get; } = isOpenWorld;
}
