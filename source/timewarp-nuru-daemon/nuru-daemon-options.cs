namespace TimeWarp.Nuru.Daemon;

/// <summary>
/// Options shared by the <c>serve</c> route and the forwarding client of a served Nuru app.
/// </summary>
/// <remarks>
/// <c>AddServeRoute()</c> copies its arguments into one instance, so the server and the client always
/// agree on the socket.
/// </remarks>
public sealed class NuruDaemonOptions
{
  /// <summary>Default <see cref="IdleTimeout"/>: ten minutes.</summary>
  public static readonly TimeSpan DefaultIdleTimeout = TimeSpan.FromMinutes(10);

  /// <summary>Default <see cref="StartTimeout"/>: thirty seconds.</summary>
  public static readonly TimeSpan DefaultStartTimeout = TimeSpan.FromSeconds(30);

  /// <summary>
  /// Environment variable that overrides the default socket path when <see cref="SocketPath"/> is null.
  /// Tests and side-by-side builds use it to keep servers apart.
  /// </summary>
  public const string SocketPathEnvironmentVariable = "NURU_DAEMON_SOCKET";

  /// <summary>
  /// Unix domain socket path. Null uses <see cref="SocketPathEnvironmentVariable"/>, then the per-user
  /// default from <see cref="NuruDaemonPaths.GetDefaultSocketPath"/>. A relative path is a path under
  /// <see cref="NuruDaemonPaths.GetDefaultSocketDirectory"/>.
  /// </summary>
  public string? SocketPath { get; set; }

  /// <summary>
  /// How long the server waits without a call before it stops. <see cref="TimeSpan.Zero"/> never stops.
  /// </summary>
  public TimeSpan IdleTimeout { get; set; } = DefaultIdleTimeout;

  /// <summary>
  /// When true, a CLI call with no running server starts one (detached) and forwards to it. When false
  /// (the default) the call runs in-process unless <c>--daemon</c> is passed.
  /// </summary>
  public bool AutoStart { get; set; }

  /// <summary>How long the client waits for a server it started to answer.</summary>
  public TimeSpan StartTimeout { get; set; } = DefaultStartTimeout;

  /// <summary>
  /// When true, the server runs one call at a time, as if every route were marked
  /// <see cref="ExclusiveAttribute"/>. When false (the default) calls run concurrently and only exclusive
  /// routes wait for each other. <c>ping</c> and <c>stop</c> never wait.
  /// </summary>
  public bool ExclusiveCalls { get; set; }

  /// <summary>Arguments that start the server: the built-in <c>serve</c> route.</summary>
  public IReadOnlyList<string> ServeArguments { get; set; } = ["serve"];

  /// <summary>
  /// The socket path for <paramref name="app"/>: <see cref="SocketPath"/>, then the environment
  /// override, then the per-user default for the app's name.
  /// </summary>
  /// <remarks>
  /// A relative path is resolved against <see cref="NuruDaemonPaths.GetDefaultSocketDirectory"/>, never
  /// the current directory, so every call and the server it starts agree on one socket.
  /// </remarks>
  /// <param name="app">The served app.</param>
  /// <returns>An absolute socket path.</returns>
  public string ResolveSocketPath(NuruApp app)
  {
    ArgumentNullException.ThrowIfNull(app);
    if (!string.IsNullOrEmpty(SocketPath))
      return Resolve(SocketPath);

    string? fromEnvironment = Environment.GetEnvironmentVariable(SocketPathEnvironmentVariable);
    if (!string.IsNullOrEmpty(fromEnvironment))
      return Resolve(fromEnvironment);

    return NuruDaemonPaths.GetDefaultSocketPath(app.Capabilities.Name);
  }

  private static string Resolve(string path) =>
    Path.IsPathFullyQualified(path) ? Path.GetFullPath(path) : Path.GetFullPath(path, NuruDaemonPaths.GetDefaultSocketDirectory());

  /// <summary>Copies the options so a host can keep its own instance.</summary>
  internal NuruDaemonOptions Clone() => new()
  {
    SocketPath = SocketPath,
    IdleTimeout = IdleTimeout,
    AutoStart = AutoStart,
    StartTimeout = StartTimeout,
    ExclusiveCalls = ExclusiveCalls,
    ServeArguments = [.. ServeArguments]
  };
}
