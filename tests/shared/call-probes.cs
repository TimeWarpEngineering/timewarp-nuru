// Helpers for the concurrency tests (task 508-016): hold handlers inside a call, count how many run at
// once, and write to the console directly. Compiled into the routing, daemon and McpHost test runfiles
// (their Directory.Build.props) and once into the CI multi-mode assembly. Global namespace: handler
// lambdas are copied into generated code, which sees only file-level usings.

#pragma warning disable CA1050 // Global on purpose; see above.

/// <summary>Writes to the process console, as a handler that ignores ITerminal does.</summary>
public static class CcConsole
{
#pragma warning disable RS0030 // The point is a direct console write.
  public static Task WriteLineAsync(string text) => Console.Out.WriteLineAsync(text);
#pragma warning restore RS0030
}

/// <summary>Lets a test hold handlers inside a call and release them by name.</summary>
public sealed class CcRendezvous(int expected)
{
  private readonly ConcurrentDictionary<string, TaskCompletionSource> Releases = new();
  private readonly TaskCompletionSource All = new(TaskCreationOptions.RunContinuationsAsynchronously);
  private readonly ConcurrentQueue<string> CancelledQueue = new();
  private int Entered;
  private volatile bool ReleasedAll;

  public static CcRendezvous Current { get; set; } = new(1);

  /// <summary>Completes when <c>expected</c> handlers have entered (or been cancelled).</summary>
  public Task AllEntered => All.Task;

  public IReadOnlyList<string> CancelledNames => [.. CancelledQueue];

  /// <summary>Called by a handler: counts it in, then waits for its release (or its call's cancellation).</summary>
  public async Task EnterAsync(string name, CancellationToken cancellationToken)
  {
    TaskCompletionSource release = Gate(name);
    Arrive();
    await release.Task.WaitAsync(cancellationToken);
  }

  /// <summary>Called by a handler whose call was cancelled.</summary>
  public void Cancelled(string name)
  {
    CancelledQueue.Enqueue(name);
    Arrive();
  }

  /// <summary>Completes once at least <paramref name="count"/> handlers have entered.</summary>
  public async Task EnteredAtLeast(int count)
  {
    while (Volatile.Read(ref Entered) < count)
      await Task.Delay(10);
  }

  public void Release(string name) => Gate(name).TrySetResult();

  public void ReleaseAll()
  {
    ReleasedAll = true;
    foreach (TaskCompletionSource release in Releases.Values)
      release.TrySetResult();
  }

  private void Arrive()
  {
    if (Interlocked.Increment(ref Entered) >= expected)
      All.TrySetResult();
  }

  private TaskCompletionSource Gate(string name)
  {
    TaskCompletionSource release = Releases.GetOrAdd(name, static _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
    if (ReleasedAll)
      release.TrySetResult();
    return release;
  }
}

/// <summary>Counts how many handlers run at once.</summary>
public sealed class CcTracker
{
  private int Active;
  private int Max;
  private int Count;

  public static CcTracker Current { get; set; } = new();

  public int MaxActive => Volatile.Read(ref Max);

  public int Runs => Volatile.Read(ref Count);

  public async Task<int> RunAsync(int value)
  {
    int now = Interlocked.Increment(ref Active);
    int seen;
    do
    {
      seen = Volatile.Read(ref Max);
    }
    while (now > seen && Interlocked.CompareExchange(ref Max, now, seen) != seen);

    await Task.Delay(40);
    Interlocked.Decrement(ref Active);
    Interlocked.Increment(ref Count);
    return value;
  }
}

/// <summary>A result whose renderer resolves <see cref="Name"/> as a path while rendering.</summary>
public sealed record CcPathResult(string Name);

/// <summary>Renders a <see cref="CcPathResult"/> with the path it resolves against the call's directory.</summary>
public sealed class CcPathRenderer : TimeWarp.Contracts.IResponseRenderer<CcPathResult>
{
  public string Render(CcPathResult response)
  {
    ArgumentNullException.ThrowIfNull(response);
    return $"{response.Name}->{TimeWarp.Nuru.NuruCallContext.GetFullPath(response.Name)}";
  }
}
