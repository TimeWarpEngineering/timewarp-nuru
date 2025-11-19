namespace TimeWarp.Nuru;

/// <summary>
/// Holds application metadata for help display and other features.
/// Consolidates name and description information in a single class.
/// </summary>
public class ApplicationMetadata
{
  /// <summary>
  /// Gets the application name.
  /// </summary>
  public string? Name { get; }

  /// <summary>
  /// Gets the application description.
  /// </summary>
  public string? Description { get; }

  /// <summary>
  /// Initializes application metadata with optional values.
  /// </summary>
  /// <param name="name">The application name.</param>
  /// <param name="description">The application description.</param>
  public ApplicationMetadata(string? name = null, string? description = null)
  {
    Name = name;
    Description = description;
  }
}