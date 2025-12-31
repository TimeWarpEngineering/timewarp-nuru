namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;

/// <summary>
/// Base class for all docker-related commands.
/// The NuruRouteGroup attribute prefixes all derived routes with "docker".
/// </summary>
[NuruRouteGroup("docker")]
public abstract class DockerGroupBase;
