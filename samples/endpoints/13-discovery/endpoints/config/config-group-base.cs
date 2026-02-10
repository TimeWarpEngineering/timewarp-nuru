namespace Endpoints.Messages;

using TimeWarp.Nuru;

/// <summary>
/// Base class for all config-related commands.
/// The NuruRouteGroup attribute prefixes all derived routes with "config".
/// </summary>
[NuruRouteGroup("config")]
public abstract class ConfigGroupBase;
