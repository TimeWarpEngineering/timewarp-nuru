namespace TimeWarp.Nuru;

using TimeWarp.Nuru.Daemon;

/// <summary>
/// Builder extension that makes an app a served app: a built-in <c>serve</c> route and route forwarding.
/// </summary>
/// <remarks>
/// This method is interpreted by the source generator at compile time, like <c>AddMcpRoute()</c>.
/// The generated code runs <see cref="NuruDaemonServer"/> for <c>serve</c> and attaches a
/// <see cref="NuruDaemonClient"/> as <see cref="NuruApp.RouteForwarder"/>. This stub exists so the
/// call compiles.
/// </remarks>
public static class NuruDaemonAppBuilderExtensions
{
  /// <summary>
  /// Adds <c>serve</c>, <c>serve --status</c> and <c>serve --stop</c>, and forwards every matched route
  /// that is not local-only to a running server.
  /// </summary>
  /// <typeparam name="TBuilder">The builder type.</typeparam>
  /// <param name="builder">The NuruAppBuilder instance.</param>
  /// <param name="socketPath">Unix domain socket path; null uses the per-user default for the app name.</param>
  /// <param name="idleTimeoutSeconds">Seconds without a call before the server stops; 0 never stops.</param>
  /// <param name="autoStart">
  /// When true, a call with no running server starts one and forwards to it. When false, calls run
  /// in-process unless <c>--daemon</c> is passed.
  /// </param>
  /// <param name="exclusiveCalls">
  /// When true, the server runs one call at a time, as if every route were marked
  /// <see cref="ExclusiveAttribute"/>. When false (the default) calls run concurrently.
  /// </param>
  /// <returns>The builder for chaining.</returns>
  /// <remarks>
  /// The arguments must be compile-time constants: the generator copies them into the route. Routes the
  /// app maps itself are matched first, so a user route <c>serve</c> (or a catch-all) takes precedence
  /// over the built-in. <c>--daemon</c> and <c>--no-daemon</c> are removed from every call's arguments.
  /// </remarks>
  public static TBuilder AddServeRoute<TBuilder>(
    this TBuilder builder,
    string? socketPath = null,
    int idleTimeoutSeconds = 600,
    bool autoStart = false,
    bool exclusiveCalls = false)
    where TBuilder : NuruAppBuilder
  {
    // Interpreted by the source generator; the arguments are read at compile time.
    _ = socketPath;
    _ = idleTimeoutSeconds;
    _ = autoStart;
    _ = exclusiveCalls;
    return builder;
  }
}
