namespace TimeWarp.Nuru;

/// <summary>
/// Holds <c>AddSessionScoped</c> instances for the current REPL session or single CLI invocation.
/// Registered as a singleton. <see cref="ResetAsync"/> disposes those instances without disposing
/// process-wide singletons.
/// </summary>
public sealed class NuruSessionCache : IDisposable, IAsyncDisposable
{
  private readonly Dictionary<Type, object> Instances = [];
  private readonly List<object> CreationOrder = [];

  /// <summary>
  /// Creates the session cache bound to the root provider.
  /// </summary>
  /// <param name="root">The root <see cref="IServiceProvider"/> used to construct session services.</param>
  public NuruSessionCache(IServiceProvider root)
  {
    ArgumentNullException.ThrowIfNull(root);
    Root = root;
  }

  /// <summary>
  /// Gets the root provider captured when the cache was constructed.
  /// </summary>
  public IServiceProvider Root { get; }

  /// <summary>
  /// Returns the session instance for <paramref name="serviceType"/>, creating it on first use.
  /// </summary>
  /// <typeparam name="TService">The service type.</typeparam>
  /// <param name="serviceType">The service type key.</param>
  /// <param name="factory">Creates the instance from the root provider. Called at most once per session.</param>
  /// <returns>The cached instance.</returns>
  public TService GetOrCreate<TService>(Type serviceType, Func<TService> factory)
    where TService : class
  {
    ArgumentNullException.ThrowIfNull(serviceType);
    ArgumentNullException.ThrowIfNull(factory);

    if (Instances.TryGetValue(serviceType, out object? existing))
      return (TService)existing;

    TService created = factory();
    Instances.Add(serviceType, created);
    CreationOrder.Add(created);
    return created;
  }

  /// <summary>
  /// Disposes session instances and allows the next resolution to create new ones.
  /// </summary>
  public async ValueTask ResetAsync()
  {
    object[] pending = [.. CreationOrder];
    CreationOrder.Clear();
    Instances.Clear();
    await NuruInstanceDisposal.DisposeReverseAsync(pending).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public void Dispose()
  {
    object[] pending = [.. CreationOrder];
    CreationOrder.Clear();
    Instances.Clear();

    for (int index = pending.Length - 1; index >= 0; index--)
    {
      if (pending[index] is IDisposable disposable)
        disposable.Dispose();
    }
  }

  /// <inheritdoc />
  public ValueTask DisposeAsync() => ResetAsync();
}
