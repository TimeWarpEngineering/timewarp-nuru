namespace AttributedRoutes.Commands;

using TimeWarp.Nuru;

/// <summary>
/// Base class for Docker commands - defines group prefix and shared options.
/// </summary>
[NuruRouteGroup("docker")]
public abstract class DockerRequestBase
{
  [GroupOption("debug", "D", Description = "Enable debug mode")]
  public bool Debug { get; set; }
}
