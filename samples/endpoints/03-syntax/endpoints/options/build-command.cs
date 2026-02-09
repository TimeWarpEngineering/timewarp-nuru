// ═══════════════════════════════════════════════════════════════════════════════
// BUILD COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Build a project with optional configuration and verbose flag.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("build", Description = "Build a project")]
public sealed class BuildCommand : ICommand<Unit>
{
  [Parameter(Description = "Project to build")]
  public string Project { get; set; } = string.Empty;

  [Option("mode", "m", Description = "Build mode (Debug or Release)")]
  public string Mode { get; set; } = "Debug";

  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

  public sealed class Handler : ICommandHandler<BuildCommand, Unit>
  {
    public ValueTask<Unit> Handle(BuildCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Building {command.Project} ({command.Mode}, verbose: {command.Verbose})");
      return default;
    }
  }
}
