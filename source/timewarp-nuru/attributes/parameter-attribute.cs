#region Purpose
// Attribute that marks a request property as a positional route parameter.
#endregion

#region Design
// Read by EndpointExtractor from the request property. Optionality comes from nullability.
// Order is the positional order. Two or more parameters that omit it report NURU_A002.
// A single parameter may omit Order. Equal Order values keep declaration order.
// A catch-all is forced last. Name, when set, is the route segment name (lowercased)
// and the generated local. It must be an identifier. A name that is not reports NURU_A005
// and the property name is used. Description is help text.
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
  /// The name must be an identifier: a letter or underscore, then letters, digits, and underscores.
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
