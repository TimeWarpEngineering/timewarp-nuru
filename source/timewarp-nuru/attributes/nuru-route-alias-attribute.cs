namespace TimeWarp.Nuru;

/// <summary>
/// Specifies additional route patterns that invoke the same request.
/// Aliases share the primary route's description in help output.
/// </summary>
/// <remarks>
/// <para>
/// Use this for common command aliases like "exit", "quit", "q" that all do the same thing.
/// The primary pattern is defined via <see cref="NuruRouteAttribute"/>.
/// </para>
/// <para>
/// In help output, aliases are displayed together: "exit, quit, q    Exit the application"
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class NuruRouteAliasAttribute : Attribute
{
  /// <summary>
  /// Gets the alias patterns.
  /// </summary>
  public string[] Aliases { get; }

  /// <summary>
  /// Creates a new route alias attribute with the specified patterns.
  /// </summary>
  /// <param name="aliases">One or more alternative route patterns.</param>
  public NuruRouteAliasAttribute(params string[] aliases)
  {
    Aliases = aliases ?? [];
  }
}
