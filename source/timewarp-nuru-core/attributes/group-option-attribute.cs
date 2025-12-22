namespace TimeWarp.Nuru;

/// <summary>
/// Marks a property as an option inherited from a route group.
/// Used on base classes with <see cref="NuruRouteGroupAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// Group options are shared across all requests that inherit from the group's base class.
/// They work the same as <see cref="OptionAttribute"/> but are defined once on the base class.
/// </para>
/// <para>
/// Do not include dashes in the attribute - the generator adds them automatically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [NuruRouteGroup("docker")]
/// public abstract class DockerRequestBase
/// {
///     [GroupOption("debug", "D")]
///     public bool Debug { get; set; }
///
///     [GroupOption("log-level")]
///     public string? LogLevel { get; set; }
/// }
///
/// [NuruRoute("run")]
/// public sealed class DockerRunRequest : DockerRequestBase, IRequest&lt;Unit&gt;
/// {
///     [Parameter]
///     public string Image { get; set; } = string.Empty;
/// }
///
/// // Generated route: "docker run {image} --debug,-D --log-level {logLevel?}"
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class GroupOptionAttribute : Attribute
{
  /// <summary>
  /// Gets the long form of the option (without dashes).
  /// </summary>
  public string LongForm { get; }

  /// <summary>
  /// Gets the short form of the option (without dash), or null if no short form.
  /// </summary>
  public string? ShortForm { get; }

  /// <summary>
  /// Gets or sets the description for help text.
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// Creates a new group option attribute.
  /// </summary>
  /// <param name="longForm">The long form without dashes (e.g., "debug" for --debug).</param>
  /// <param name="shortForm">The short form without dash (e.g., "D" for -D), or null.</param>
  public GroupOptionAttribute(string longForm, string? shortForm = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(longForm);
    LongForm = longForm;
    ShortForm = shortForm;
  }
}
