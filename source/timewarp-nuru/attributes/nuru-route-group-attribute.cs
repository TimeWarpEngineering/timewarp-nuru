namespace TimeWarp.Nuru;

/// <summary>
/// Defines a route group with a shared prefix. Applied to a base class that request classes inherit from.
/// </summary>
/// <remarks>
/// <para>
/// Route groups allow related requests to share a common prefix and options.
/// The group prefix is prepended to the pattern defined in <see cref="NuruRouteAttribute"/>.
/// </para>
/// <para>
/// Group options are defined using <see cref="GroupOptionAttribute"/> on properties in the base class.
/// These options are automatically included in all routes that inherit from this group.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Base class defines the group
/// [NuruRouteGroup("docker")]
/// public abstract class DockerRequestBase
/// {
///     [GroupOption("debug", "D")]
///     public bool Debug { get; set; }
/// }
/// 
/// // Request inherits group membership
/// [NuruRoute("run")]
/// public sealed class DockerRunRequest : DockerRequestBase, IRequest&lt;Unit&gt;
/// {
///     [Parameter]
///     public string Image { get; set; } = string.Empty;
/// }
/// 
/// // Generated route: "docker run {image} --debug,-D"
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class NuruRouteGroupAttribute : Attribute
{
  /// <summary>
  /// Gets the group prefix (literal segments).
  /// </summary>
  public string Prefix { get; }

  /// <summary>
  /// Gets or sets the description for the group (shown in help).
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// Creates a new route group attribute with the specified prefix.
  /// </summary>
  /// <param name="prefix">The route prefix (literal segments, e.g., "docker" or "git remote").</param>
  public NuruRouteGroupAttribute(string prefix)
  {
    Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
  }
}
