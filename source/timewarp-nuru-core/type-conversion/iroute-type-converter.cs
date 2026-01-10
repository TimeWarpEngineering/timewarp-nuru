namespace TimeWarp.Nuru;

/// <summary>
/// Interface for converting string values from route parameters to strongly typed values.
/// </summary>
/// <remarks>
/// The primary constraint name is always the type name (e.g., "EmailAddress", "DateTime").
/// The optional <see cref="Alias"/> property allows defining a shorter alternative name
/// (e.g., "email" for EmailAddress). Both the type name and alias work in route patterns.
/// </remarks>
public interface IRouteTypeConverter
{
  /// <summary>
  /// Gets the target type this converter produces.
  /// </summary>
  Type TargetType { get; }

  /// <summary>
  /// Gets an optional alias for use in route patterns.
  /// </summary>
  /// <remarks>
  /// If null, only the type name can be used as the constraint (e.g., {param:EmailAddress}).
  /// If set, both the alias and type name work (e.g., {param:email} OR {param:EmailAddress}).
  /// </remarks>
  string? ConstraintAlias { get; }

  /// <summary>
  /// Attempts to convert a string value to the target type.
  /// </summary>
  /// <param name="value">The string value to convert.</param>
  /// <param name="result">The converted value if successful, null otherwise.</param>
  /// <returns>True if conversion succeeded, false otherwise.</returns>
  bool TryConvert(string value, out object? result);
}
