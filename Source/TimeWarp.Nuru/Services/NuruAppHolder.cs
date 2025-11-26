namespace TimeWarp.Nuru;

/// <summary>
/// Holds a reference to the NuruApp instance for deferred access scenarios.
/// Used for DI path where the app is created after the service provider is built.
/// Enables interactive mode route handlers to access the app via injection.
/// </summary>
public sealed class NuruAppHolder
{
  private NuruApp? StoredApp;

  /// <summary>
  /// Gets the NuruApp instance.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown if accessed before the app is set.</exception>
  public NuruApp App => StoredApp ?? throw new InvalidOperationException(
    "NuruApp has not been set. This is a framework bug - the holder should be populated during Build().");

  /// <summary>
  /// Gets a value indicating whether the app has been set.
  /// </summary>
  public bool HasApp => StoredApp is not null;

  /// <summary>
  /// Sets the NuruApp instance. Can only be called once.
  /// </summary>
  /// <param name="app">The NuruApp instance to store.</param>
  /// <exception cref="InvalidOperationException">Thrown if called more than once.</exception>
  internal void SetApp(NuruApp app)
  {
    if (StoredApp is not null)
    {
      throw new InvalidOperationException("NuruApp has already been set. This is a framework bug.");
    }

    StoredApp = app ?? throw new ArgumentNullException(nameof(app));
  }
}
