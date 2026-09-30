namespace TimeWarp.Nuru;

/// <summary>
/// Runs a source-generated scope cleanup when the command method returns.
/// </summary>
public sealed class NuruAsyncLease : IAsyncDisposable
{
  private readonly Func<ValueTask>? DisposeAction;

  private NuruAsyncLease(Func<ValueTask>? disposeAction)
  {
    DisposeAction = disposeAction;
  }

  /// <summary>
  /// Selects the REPL or single-run cleanup delegate.
  /// </summary>
  /// <param name="fromRepl">True when the command is executing inside the REPL.</param>
  /// <param name="replEnd">Disposes command-scoped instances and leaves the session intact.</param>
  /// <param name="singleRunEnd">Disposes command-scoped and session-scoped instances for one invocation.</param>
  /// <returns>A lease whose disposal invokes the selected delegate.</returns>
  public static NuruAsyncLease For(bool fromRepl, Func<ValueTask> replEnd, Func<ValueTask> singleRunEnd)
  {
    ArgumentNullException.ThrowIfNull(replEnd);
    ArgumentNullException.ThrowIfNull(singleRunEnd);
    return new NuruAsyncLease(fromRepl ? replEnd : singleRunEnd);
  }

  /// <inheritdoc />
  public ValueTask DisposeAsync()
  {
    Func<ValueTask>? disposeAction = DisposeAction;
    return disposeAction is null ? ValueTask.CompletedTask : disposeAction();
  }
}

/// <summary>
/// Opens a Microsoft.Extensions.DependencyInjection command scope for one command and
/// disposes <see cref="NuruCommandCache"/> when the command returns.
/// A single CLI invocation also resets <see cref="NuruSessionCache"/>.
/// </summary>
public sealed class NuruCommandLease : IAsyncDisposable
{
  private readonly IServiceProvider? Root;
  private readonly IServiceScope? Scope;
  private readonly bool ResetSession;
  private readonly Action<IServiceScope?>? SetScope;

  private NuruCommandLease
  (
    IServiceProvider? root,
    IServiceScope? scope,
    bool resetSession,
    Action<IServiceScope?>? setScope
  )
  {
    Root = root;
    Scope = scope;
    ResetSession = resetSession;
    SetScope = setScope;
  }

  /// <summary>
  /// Starts command-scoped resolution against <paramref name="root"/>.
  /// In the REPL, services are resolved from a new <see cref="IServiceScope"/> so <c>AddScoped</c>
  /// matches command scope. A single CLI invocation keeps resolving <c>AddScoped</c> from the root.
  /// </summary>
  /// <param name="root">The app's root provider.</param>
  /// <param name="fromRepl">True when the command is executing inside the REPL.</param>
  /// <param name="setScope">Receives the REPL command scope, or null for a single CLI invocation.</param>
  /// <param name="hostRepl">True when this invocation is starting the REPL (<c>--interactive</c>), not running a command.</param>
  /// <returns>A lease that disposes the command when the caller exits.</returns>
  public static NuruCommandLease Enter
  (
    IServiceProvider root,
    bool fromRepl,
    Action<IServiceScope?> setScope,
    bool hostRepl = false
  )
  {
    ArgumentNullException.ThrowIfNull(root);
    ArgumentNullException.ThrowIfNull(setScope);

    // The REPL host call nests ExecuteRouteAsync for each command. Those commands own the
    // command cache. Beginning it here would make the first command's Begin throw.
    if (hostRepl && !fromRepl)
    {
      setScope(null);
      return new NuruCommandLease(root: null, scope: null, resetSession: false, setScope);
    }

    NuruCommandCache? commands = root.GetService<NuruCommandCache>();

    if (!fromRepl)
    {
      setScope(null);
      commands?.Begin();
      return new NuruCommandLease(root, scope: null, resetSession: true, setScope);
    }

    IServiceScope scope = root.CreateScope();
    setScope(scope);
    commands?.Begin();
    return new NuruCommandLease(root, scope, resetSession: false, setScope);
  }

  /// <summary>
  /// Disposes session-scoped instances held by <paramref name="root"/>, if a session cache was registered.
  /// </summary>
  /// <param name="root">The app's root provider, or null when the provider was never built.</param>
  public static async ValueTask ResetSessionAsync(IServiceProvider? root)
  {
    if (root is null)
      return;

    NuruSessionCache? session = root.GetService<NuruSessionCache>();
    if (session is not null)
      await session.ResetAsync().ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async ValueTask DisposeAsync()
  {
    if (Root is null)
      return;

    NuruCommandCache? commands = Root.GetService<NuruCommandCache>();
    if (commands is not null)
      await commands.EndAsync(disposeInstances: Scope is null).ConfigureAwait(false);

    if (Scope is not null)
    {
      if (Scope is IAsyncDisposable asyncScope)
        await asyncScope.DisposeAsync().ConfigureAwait(false);
      else
        Scope.Dispose();

      SetScope?.Invoke(null);
    }

    if (ResetSession)
      await ResetSessionAsync(Root).ConfigureAwait(false);
  }
}
