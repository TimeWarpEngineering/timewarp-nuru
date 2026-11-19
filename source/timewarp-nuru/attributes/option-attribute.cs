#region Purpose
// Attribute that marks a request property as a command-line option (flag or valued).
#endregion

#region Design
// Read by EndpointExtractor (and OperationEndpointExtractor) from the request property. Dashes
// are added by the generator, so the attribute carries bare names. Option shape comes from the
// property type: a nullable type is an optional value, an array or collection is repeatable.
// A non-nullable bool is a flag, except when its initializer is true: that bool is a value
// option so absence keeps true. IsRepeated = true on a non-collection reports NURU_A004.
// IsRepeated = false does not stop a collection from repeating. The property initializer is
// the default, and it applies only when the option is absent.
#endregion

#region Open Questions
// Q1 (2026-11-14, claude): EndpointExtractor reads the named arguments LongName, ShortName and
//     IsRequired, which this attribute does not define (it has constructor parameters
//     longForm/shortForm and no required flag). Are those dead branches to delete, or is an
//     IsRequired setting intended?
#endregion

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
///   <item><description><c>bool</c> - Boolean flag (no value expected), unless the initializer is <c>true</c>. That bool is a value option, so leaving it off stays true and <c>--name false</c> binds false.</description></item>
///   <item><description>Other types - Valued option (value expected after the flag)</description></item>
///   <item><description>Nullable types (<c>string?</c>, <c>int?</c>) - Optional valued option</description></item>
/// </list>
/// </para>
/// </remarks>
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
  /// When true, the property must be an array or collection. A scalar reports NURU_A004.
  /// A collection repeats from its type even when this is left false.
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
