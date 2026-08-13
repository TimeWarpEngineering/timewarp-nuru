namespace TimeWarp.Nuru;

/// <summary>
/// Declares a concrete usage example for a route. Examples render in per-route help output
/// (the "Examples:" section) and in the <c>--capabilities</c> JSON for AI agent discovery.
/// </summary>
/// <remarks>
/// <para>
/// Apply multiple times to declare several examples; they render in the order declared.
/// The primary pattern is defined via <see cref="NuruRouteAttribute"/>.
/// </para>
/// <para>
/// The command text is verbatim - write it exactly as a user would type it after the
/// executable name, e.g. <c>"deploy prod --dry-run"</c>.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class NuruRouteExampleAttribute : Attribute
{
  /// <summary>
  /// Gets the example command invocation, verbatim as typed after the executable name.
  /// </summary>
  public string Command { get; }

  /// <summary>
  /// Gets or sets an optional description of what this example demonstrates.
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// Creates a new route example attribute with the specified command.
  /// </summary>
  /// <param name="command">The example invocation, verbatim as typed after the executable name.</param>
  public NuruRouteExampleAttribute(string command)
  {
    Command = command;
  }
}
