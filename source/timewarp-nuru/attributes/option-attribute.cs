namespace TimeWarp.Nuru;

/// <summary>
/// Marks a property as a command-line option (flag or valued option).
/// </summary>
/// <remarks>
/// <para>
/// Options are specified with <c>--longForm</c> and optionally <c>-s</c> (short form).
/// Do not include dashes in the attribute - the generator adds them automatically.
/// </para>
/// <para>
/// The option type is inferred from the property type:
/// <list type="bullet">
///   <item><description><c>bool</c> - Boolean flag (no value expected)</description></item>
///   <item><description>Other types - Valued option (value expected after the flag)</description></item>
///   <item><description>Nullable types (<c>string?</c>, <c>int?</c>) - Optional valued option</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [NuruRoute("deploy")]
/// public sealed class DeployRequest : IRequest&lt;Unit&gt;
/// {
///     [Option("force", "f", Description = "Skip confirmation")]
///     public bool Force { get; set; }  // Flag: --force or -f
///
///     [Option("config", "c")]
///     public string? ConfigFile { get; set; }  // Optional valued: --config file.json
///
///     [Option("replicas", "r")]
///     public int Replicas { get; set; } = 1;  // Required valued: --replicas 3
/// }
///
/// // Generated route: "deploy --force,-f --config,-c {configFile?} --replicas,-r {replicas}"
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class OptionAttribute : Attribute
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
  /// Gets or sets whether this option can be specified multiple times.
  /// </summary>
  /// <remarks>
  /// When true, the property should have an array or collection type to hold multiple values.
  /// </remarks>
  public bool IsRepeated { get; set; }

  /// <summary>
  /// Creates a new option attribute.
  /// </summary>
  /// <param name="longForm">The long form without dashes (e.g., "force" for --force).</param>
  /// <param name="shortForm">The short form without dash (e.g., "f" for -f), or null.</param>
  public OptionAttribute(string longForm, string? shortForm = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(longForm);
    LongForm = longForm;
    ShortForm = shortForm;
  }
}
