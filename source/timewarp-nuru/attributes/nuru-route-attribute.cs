namespace TimeWarp.Nuru;

/// <summary>
/// Marks a request class for auto-registration with the specified route pattern.
/// The source generator will create route registration code that runs at module initialization.
/// </summary>
/// <remarks>
/// <para>
/// The pattern contains only the literal segments (e.g., "deploy", "docker compose up", or "" for default route).
/// Parameters and options are inferred from properties marked with <see cref="ParameterAttribute"/> and <see cref="OptionAttribute"/>.
/// </para>
/// <para>
/// If the class inherits from a base class with <see cref="NuruRouteGroupAttribute"/>, the group prefix
/// is automatically prepended to this pattern.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [NuruRoute("deploy", Description = "Deploy to an environment")]
/// public sealed class DeployRequest : IRequest&lt;Unit&gt;
/// {
///     [Parameter(Description = "Target environment")]
///     public string Env { get; set; } = string.Empty;
///     
///     [Option("force", "f", Description = "Skip confirmation")]
///     public bool Force { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class NuruRouteAttribute : Attribute
{
  /// <summary>
  /// Gets the route pattern (literal segments only).
  /// </summary>
  /// <remarks>
  /// Use empty string for the default route.
  /// Multiple literals are space-separated (e.g., "docker compose up").
  /// </remarks>
  public string Pattern { get; }

  /// <summary>
  /// Gets or sets the description for help text.
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// Creates a new route attribute with the specified pattern.
  /// </summary>
  /// <param name="pattern">The route pattern (literal segments only). Use "" for default route.</param>
  public NuruRouteAttribute(string pattern = "")
  {
    Pattern = pattern ?? string.Empty;
  }
}
