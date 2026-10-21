namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Design-time representation of an <c>AddServeRoute()</c> call (package TimeWarp.Nuru.Daemon).
/// The generator emits a built-in <c>serve</c> route, attaches the forwarding client, and forwards
/// matched routes that are not local-only.
/// </summary>
/// <param name="SocketPath">Explicit socket path, or null for the per-user default.</param>
/// <param name="IdleTimeoutSeconds">Seconds without a call before the server stops; 0 never stops.</param>
/// <param name="AutoStart">When true, a call with no running server starts one and forwards.</param>
/// <param name="ExclusiveCalls">When true, the server runs one call at a time.</param>
public sealed record ServeRouteModel(
  string? SocketPath,
  int IdleTimeoutSeconds,
  bool AutoStart,
  bool ExclusiveCalls = false);
