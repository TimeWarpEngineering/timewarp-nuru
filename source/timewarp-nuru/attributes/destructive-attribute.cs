namespace TimeWarp.Nuru;

/// <summary>
/// Marks an endpoint as destructive: it may delete or overwrite data that cannot be recovered.
/// The <c>--capabilities</c> catalog carries it as <c>"destructive": true</c>, and an MCP host
/// maps it to the tool's <c>destructiveHint</c>.
/// </summary>
/// <remarks>
/// Nothing infers this hint from the endpoint kind or name. A route without the attribute has no
/// <c>destructive</c> member in the catalog.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DestructiveAttribute : Attribute;
