#region Purpose
// The per-app gate that exclusive calls take ([Exclusive], .Exclusive(), a host's ExclusiveCalls option).
#endregion

#region Design
// One SemaphoreSlim per NuruApp. Generated code takes it around the behaviors and handler of an exclusive
// route; a host with ExclusiveCalls takes it around the whole call. The holder is recorded on the
// NuruCallContext (an AsyncLocal set inside an async method would not reach the caller), so a call that
// already holds the gate (a host-wide exclusive call reaching an exclusive route, or a nested call) does not
// wait for itself. Outside a call context nothing runs concurrently, so the gate is not taken.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Holds an app's exclusive gate until disposed. Generated code and hosts take it with
/// <see cref="EnterAsync"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class NuruExclusiveLease : IAsyncDisposable
{
  /// <summary>A lease that holds nothing, for a call that does not take the gate.</summary>
  public static NuruExclusiveLease None { get; } = new(null, null);

  private readonly SemaphoreSlim? Gate;
  private NuruCallContext? Holder;

  private NuruExclusiveLease(SemaphoreSlim? gate, NuruCallContext? holder)
  {
    Gate = gate;
    Holder = holder;
  }

  /// <summary>
  /// Waits until no other exclusive call of <paramref name="app"/> runs, then holds the gate for the
  /// current call. Returns at once outside a call context or when the current call already holds it.
  /// </summary>
  /// <param name="app">The app whose gate to take.</param>
  /// <param name="cancellationToken">Cancels the wait.</param>
  /// <returns>A lease that releases the gate when disposed.</returns>
  public static Task<NuruExclusiveLease> EnterAsync(NuruApp app, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(app);
    return NuruCallContext.Current is { } context ? EnterAsync(app, context, cancellationToken) : Task.FromResult(None);
  }

  /// <summary>
  /// Waits until no other exclusive call of <paramref name="app"/> runs, then holds the gate for the call
  /// <paramref name="context"/> runs. A host takes it this way before it starts the call. Returns at once
  /// when that call already holds it.
  /// </summary>
  /// <param name="app">The app whose gate to take.</param>
  /// <param name="context">The call that holds the gate.</param>
  /// <param name="cancellationToken">Cancels the wait.</param>
  /// <returns>A lease that releases the gate when disposed.</returns>
  public static async Task<NuruExclusiveLease> EnterAsync(NuruApp app, NuruCallContext context, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(app);
    ArgumentNullException.ThrowIfNull(context);
    if (context.HoldsExclusive)
      return None;

    await app.ExclusiveGate.WaitAsync(cancellationToken).ConfigureAwait(false);
    context.HoldsExclusive = true;
    return new NuruExclusiveLease(app.ExclusiveGate, context);
  }

  /// <inheritdoc />
  public ValueTask DisposeAsync()
  {
    if (Interlocked.Exchange(ref Holder, null) is { } holder)
    {
      holder.HoldsExclusive = false;
      Gate!.Release();
    }

    return ValueTask.CompletedTask;
  }
}
