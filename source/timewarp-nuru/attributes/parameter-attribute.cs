#region Purpose
// Attribute that marks a request property as a positional route parameter.
#endregion

#region Design
// Read by EndpointExtractor from the request property. The route parameter name is the
// lowercased property name and optionality comes from nullability, so the generator derives
// shape from the type system instead of repeating it in the attribute. Position follows
// declaration order. The catch-all flag is the one setting the generator takes from the
// attribute, together with Description.
#endregion

#region Open Questions
// Q1 (2026-11-14, claude): Order and Name are not read by the generator. Parameter position is
//     property declaration order and the segment name is the lowercased property name. The XML
//     docs state Order is required for several parameters and Name overrides the name. Should
//     the generator honor them, or should the properties and docs be removed?
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Marks a property as a positional parameter in the route.
/// </summary>
/// <remarks>
/// <para>
/// Positional parameters are matched in the order specified by <see cref="Order"/>.
/// When a command has multiple parameters, <see cref="Order"/> is required to ensure deterministic ordering.
/// </para>
/// <para>
/// The parameter is required by default; use a nullable type (e.g., <c>string?</c>) for optional parameters.
/// </para>
/// <para>
/// The parameter name defaults to the property name (in camelCase). Use <see cref="Name"/> to override.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [NuruRoute("tag")]
/// public sealed class TagCommand : ICommand&lt;Unit&gt;
/// {
///     [Parameter(Order = 0, Description = "Source image name")]
///     public string Source { get; set; } = string.Empty;
///
///     [Parameter(Order = 1, Description = "Target image name")]
///     public string Target { get; set; } = string.Empty;
/// }
///
/// // Generated route: "tag {source} {target}"
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class ParameterAttribute : Attribute
{
  /// <summary>
  /// Gets or sets the order of this parameter in the CLI argument list.
  /// Required when a command has multiple parameters.
  /// Lower values come first (0, 1, 2, ...).
  /// </summary>
  public int Order { get; set; } = -1;

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
