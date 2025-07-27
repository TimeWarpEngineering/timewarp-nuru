namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Represents a parameter in a route pattern.
/// </summary>
public class RouteParameter
{
  /// <summary>
  /// Gets or sets the type constraint (e.g., "int" from {id:int}).
  /// </summary>
  public string? TypeConstraint { get; set; }
  /// <summary>
  /// Gets or sets the option name this parameter is associated with (e.g., "--message" for --message {msg}).
  /// </summary>
  public string? AssociatedOption { get; set; }
}