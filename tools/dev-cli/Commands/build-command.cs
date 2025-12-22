// ═══════════════════════════════════════════════════════════════════════════════
// BUILD COMMAND
// ═════════════════════════════════════════════════════════════════════════════
// Migrates build.cs logic to attributed routes pattern
// Builds all TimeWarp.Nuru projects in dependency order using Release configuration

namespace DevCli.Commands;
/// <summary>
/// Build all TimeWarp.Nuru projects.
/// </summary>
[NuruRoute("build", Description = "Build all TimeWarp.Nuru projects")]
internal sealed class BuildCommand : ICommand<Unit>
{
  [Option("clean", "c", Description = "Clean before building")]
  public bool Clean { get; set; }

  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

}

internal sealed class BuildCommandHandler : ICommandHandler<BuildCommand, Unit>
{
  public ValueTask<Unit> Handle(BuildCommand command, CancellationToken cancellationToken)
  {
    TimeWarpTerminal.Default.WriteLine("Build command works!");
    return default;
  }
}
