namespace TimeWarp.Nuru;

/// <summary>
/// Static factory class for creating Nuru application builders.
/// Provides convenient entry points for creating NuruCoreApp instances.
/// </summary>
public static partial class NuruApp
{
  /// <summary>
  /// Creates a new <see cref="NuruAppBuilder"/> instance configured for Nuru applications.
  /// </summary>
  /// <param name="args">Command-line arguments passed to the application.</param>
  /// <returns>A new <see cref="NuruAppBuilder"/> instance ready for configuration.</returns>
  public static NuruAppBuilder CreateBuilder(string[]? args = null) =>
    new NuruAppBuilder();
}
