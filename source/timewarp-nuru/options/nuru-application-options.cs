namespace TimeWarp.Nuru;

/// <summary>
/// Options for configuring a Nuru application.
/// Mirrors ASP.NET Core's WebApplicationOptions pattern.
/// </summary>
public class NuruAppOptions
{
  /// <summary>
  /// Gets or sets the command line arguments.
  /// </summary>
  /// <remarks>
  /// This property returns an array to match ASP.NET Core's WebApplicationOptions.Args pattern.
  /// The array is typically set once during initialization and not modified afterward.
  /// </remarks>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1819:Properties should not return arrays",
    Justification = "Matches ASP.NET Core's WebApplicationOptions.Args pattern for familiarity")]
  public string[]? Args { get; set; }

  /// <summary>
  /// Gets or sets the application name.
  /// If not set, will be auto-detected from the entry assembly.
  /// </summary>
  public string? ApplicationName { get; set; }

  /// <summary>
  /// Gets or sets the environment name.
  /// Defaults to ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT environment variable, or "Production".
  /// </summary>
  public string? EnvironmentName { get; set; }

  /// <summary>
  /// Gets or sets the content root path.
  /// Defaults to the directory containing the application.
  /// </summary>
  public string? ContentRootPath { get; set; }
}
