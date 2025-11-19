namespace TimeWarp.Nuru;

/// <summary>
/// Holds application metadata for help display and other features.
/// Consolidates name and description information in a single class.
/// </summary>
public class ApplicationMetadata(string? name = null, string? description = null)
{
  /// <summary>
  /// Gets the application name.
  /// </summary>
  public string? Name { get; } = name;

  /// <summary>
  /// Gets the application description.
  /// </summary>
  public string? Description { get; } = description;
}