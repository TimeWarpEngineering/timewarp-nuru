namespace TimeWarp.Nuru;

/// <summary>
/// Holds a reference to the NuruCoreApp instance for deferred access scenarios.
/// Used for DI path where the app is created after the service provider is built.
/// Enables interactive mode route handlers to access the app via injection.
/// </summary>
public sealed class NuruCoreAppHolder
{
  private NuruCoreApp? StoredApp;

  /// <summary>
  /// Gets the NuruCoreApp instance.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown if accessed before the app is set.</exception>
  public NuruCoreApp App => StoredApp ?? throw new InvalidOperationException(
    "NuruCoreApp has not been set. This is a framework bug - the holder should be populated during Build().");

  /// <summary>
  /// Gets a value indicating whether the app has been set.
  /// </summary>
  public bool HasApp => StoredApp is not null;

  /// <summary>
  /// Sets the NuruCoreApp instance. Can only be called once.
  /// </summary>
  /// <param name="app">The NuruCoreApp instance to store.</param>
  /// <exception cref="InvalidOperationException">Thrown if called more than once.</exception>
  internal void SetApp(NuruCoreApp app)
  {
    if (StoredApp is not null)
    {
      throw new InvalidOperationException("NuruCoreApp has already been set. This is a framework bug.");
    }

    StoredApp = app ?? throw new ArgumentNullException(nameof(app));
  }
}
