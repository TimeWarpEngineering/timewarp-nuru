namespace TimeWarp.Nuru;

/// <summary>
/// Disposes scope-owned instances once, preferring <see cref="IAsyncDisposable"/> when a type implements both.
/// </summary>
internal static class NuruInstanceDisposal
{
  public static async ValueTask DisposeReverseAsync(IReadOnlyList<object> instances)
  {
    ArgumentNullException.ThrowIfNull(instances);

    for (int index = instances.Count - 1; index >= 0; index--)
    {
      await DisposeOneAsync(instances[index]).ConfigureAwait(false);
    }
  }

  public static ValueTask DisposeOneAsync(object instance)
  {
    ArgumentNullException.ThrowIfNull(instance);

    if (instance is IAsyncDisposable asyncDisposable)
      return asyncDisposable.DisposeAsync();

    if (instance is IDisposable disposable)
      disposable.Dispose();

    return ValueTask.CompletedTask;
  }
}
