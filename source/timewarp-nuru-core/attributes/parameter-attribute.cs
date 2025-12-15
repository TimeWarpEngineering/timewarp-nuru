namespace TimeWarp.Nuru;

/// <summary>
/// Marks a property as a positional parameter in the route.
/// </summary>
/// <remarks>
/// <para>
/// Positional parameters are matched in order of property declaration.
/// The parameter is required by default; use a nullable type (e.g., <c>string?</c>) for optional parameters.
/// </para>
/// <para>
/// The parameter name defaults to the property name (in camelCase). Use <see cref="Name"/> to override.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [NuruRoute("greet")]
/// public sealed class GreetRequest : IRequest&lt;Unit&gt;
/// {
///     [Parameter(Description = "Name to greet")]
///     public string Name { get; set; } = string.Empty;
///     
///     [Parameter(Description = "Optional greeting style")]
///     public string? Style { get; set; }  // Optional because nullable
/// }
/// 
/// // Generated route: "greet {name} {style?}"
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class ParameterAttribute : Attribute
{
  /// <summary>
  /// Gets or sets the parameter name. If null, the property name is used (in camelCase).
  /// </summary>
  public string? Name { get; set; }

  /// <summary>
  /// Gets or sets the description for help text.
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// Gets or sets whether this is a catch-all parameter that captures all remaining arguments.
  /// </summary>
  /// <remarks>
  /// Catch-all parameters must be the last parameter and should have an array type (e.g., <c>string[]</c>).
  /// </remarks>
  public bool IsCatchAll { get; set; }
}
