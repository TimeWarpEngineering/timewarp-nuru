namespace TimeWarp.Nuru;

/// <summary>
/// Marks an endpoint as destructive: it may delete or overwrite data that cannot be recovered.
/// The <c>--capabilities</c> catalog carries it as <c>"destructive": true</c>, and an MCP host
/// maps it to the tool's <c>destructiveHint</c>.
/// </summary>
/// <remarks>
/// Nothing infers this hint from the endpoint kind or name. A route without the attribute has no
/// <c>destructive</c> member in the catalog. <c>[Destructive(false)]</c> states that a command only
/// adds data, so the catalog carries <c>"destructive": false</c> and a harness may treat it as safe.
/// </remarks>
/// <param name="isDestructive">False to state that the endpoint is not destructive.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DestructiveAttribute(bool isDestructive = true) : Attribute
{
  /// <summary>True when the endpoint may delete or overwrite data.</summary>
  public bool IsDestructive { get; } = isDestructive;
}
