namespace TimeWarp.Nuru;

/// <summary>
/// Holds <c>AddCommandScoped</c> instances for the command currently executing.
/// A single CLI invocation and each REPL command each call <see cref="Begin"/> and <see cref="EndAsync"/>.
/// </summary>
public sealed class NuruCommandCache : IDisposable, IAsyncDisposable
{
  private Dictionary<Type, object>? Instances;
  private List<object>? CreationOrder;

  /// <summary>
  /// Starts a command scope. Resolutions until <see cref="EndAsync"/> share one instance per service type.
  /// </summary>
  public void Begin()
  {
    if (Instances is not null)
      throw new InvalidOperationException("A Nuru command scope is already active.");

    Instances = [];
    CreationOrder = [];
  }

  /// <summary>
  /// Returns the command instance for <paramref name="serviceType"/>, creating it on first use.
  /// </summary>
  /// <typeparam name="TService">The service type.</typeparam>
  /// <param name="serviceType">The service type key.</param>
  /// <param name="provider">The provider for this command. A REPL command passes its <see cref="IServiceScope"/> provider.</param>
  /// <param name="factory">Creates the instance. Called at most once per command.</param>
  /// <returns>The cached instance.</returns>
  public TService GetOrCreate<TService>
  (
    Type serviceType,
    IServiceProvider provider,
    Func<IServiceProvider, TService> factory
  )
    where TService : class
  {
    ArgumentNullException.ThrowIfNull(serviceType);
    ArgumentNullException.ThrowIfNull(provider);
    ArgumentNullException.ThrowIfNull(factory);

    if (Instances is null || CreationOrder is null)
      throw new InvalidOperationException("Nuru command scope is not active.");

    if (Instances.TryGetValue(serviceType, out object? existing))
      return (TService)existing;

    NuruSessionCache? session = provider.GetService<NuruSessionCache>();
    IServiceProvider constructionProvider = session is null
      ? provider
      : new NuruConstructionProvider(session.Root, provider);
    TService created = factory(constructionProvider);
    Instances.Add(serviceType, created);
    CreationOrder.Add(created);
    return created;
  }

  /// <summary>
  /// Drops command instances. When <paramref name="disposeInstances"/> is false, the active
  /// <see cref="IServiceScope"/> owns disposal (it captured the instances as transients).
  /// </summary>
  /// <param name="disposeInstances">True for a single CLI invocation, which has no command scope.</param>
  public async ValueTask EndAsync(bool disposeInstances)
  {
    if (CreationOrder is null)
      return;

    object[] pending = [.. CreationOrder];
    CreationOrder = null;
    Instances = null;
    if (disposeInstances)
      await NuruInstanceDisposal.DisposeReverseAsync(pending).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public void Dispose()
  {
    if (CreationOrder is null)
      return;

    object[] pending = [.. CreationOrder];
    CreationOrder = null;
    Instances = null;

    for (int index = pending.Length - 1; index >= 0; index--)
    {
      if (pending[index] is IDisposable disposable)
        disposable.Dispose();
    }
  }

  /// <inheritdoc />
  public ValueTask DisposeAsync() => EndAsync(disposeInstances: true);
}
