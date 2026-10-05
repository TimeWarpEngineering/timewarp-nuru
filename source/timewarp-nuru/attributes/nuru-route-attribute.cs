namespace TimeWarp.Nuru;

/// <summary>
/// Marks a request class for auto-registration with the specified route pattern.
/// The source generator will create route registration code that runs at module initialization.
/// </summary>
/// <remarks>
/// <para>
/// The pattern is a single literal (e.g., "deploy") or "" for the default route.
/// Multi-word patterns and parameters are rejected (NURU_A001).
/// Use <see cref="NuruRouteGroupAttribute"/> on a base class for sub-command prefixes.
/// Parameters and options are inferred from properties marked with <see cref="ParameterAttribute"/> and <see cref="OptionAttribute"/>.
/// </para>
/// <para>
/// If the class inherits from a base class with <see cref="NuruRouteGroupAttribute"/>, the group prefix
/// is automatically prepended to this pattern.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class NuruRouteAttribute : Attribute
{
  /// <summary>
  /// Gets the route pattern (a single literal).
  /// </summary>
  /// <remarks>
  /// Use empty string for the default route.
  /// Space-separated multi-word patterns are not allowed (NURU_A001).
  /// </remarks>
  public string Pattern { get; }

  /// <summary>
  /// Gets or sets the description for help text.
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// Creates a new route attribute with the specified pattern.
  /// </summary>
  /// <param name="pattern">The route pattern (a single literal). Use "" for default route.</param>
  public NuruRouteAttribute(string pattern = "")
  {
    Pattern = pattern ?? string.Empty;
  }
}
