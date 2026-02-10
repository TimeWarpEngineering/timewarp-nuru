// ═══════════════════════════════════════════════════════════════════════════════
// BUILD COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Builds all TimeWarp.Nuru projects in dependency order using Release configuration.
// Migrated from runfiles/build.cs to endpoints pattern.

namespace DevCli.Commands;

/// <summary>
/// Build all TimeWarp.Nuru projects in dependency order.
/// </summary>
[NuruRoute("build", Description = "Build all TimeWarp.Nuru projects")]
internal sealed class BuildCommand : ICommand<Unit>
{
  [Option("clean", "c", Description = "Clean before building")]
  public bool Clean { get; set; }

  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

  internal sealed class Handler : ICommandHandler<BuildCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(BuildCommand command, CancellationToken ct)
    {
      // Get repo root using Git.FindRoot
      string? repoRoot = Git.FindRoot();

      if (repoRoot is null)
      {
        throw new InvalidOperationException("Could not find git repository root (.git not found)");
      }

      // Verify we're in the right place
      if (!File.Exists(Path.Combine(repoRoot, "timewarp-nuru.slnx")))
      {
        throw new InvalidOperationException("Could not find repository root (timewarp-nuru.slnx not found)");
      }

      Terminal.WriteLine("Building TimeWarp.Nuru library...");
      Terminal.WriteLine($"Working from: {repoRoot}");

      // Clean first if requested
      if (command.Clean)
      {
        Terminal.WriteLine("\nCleaning before build...");
        string verbosity = command.Verbose ? "normal" : "minimal";

        CommandResult cleanResult = DotNet.Clean()
          .WithProject(Path.Combine(repoRoot, "timewarp-nuru.slnx"))
          .WithVerbosity(verbosity)
          .Build();

        if (await cleanResult.RunAsync() != 0)
        {
          throw new InvalidOperationException("Clean failed!");
        }
      }

      // Build each project individually to avoid framework resolution issues
      // Note: Some projects are commented out in the solution or have known issues:
      // - timewarp-nuru-repl: Needs NuruApp properties not yet implemented
      // - timewarp-nuru-testapp-delegates: Has catch-all parameter generator bug (#331)
      // - benchmarks/samples: Not needed for CI validation
      // IMPORTANT: timewarp-nuru-build must be built BEFORE timewarp-nuru because
      // timewarp-nuru includes its output DLLs in the NuGet package via wildcard.
      string[] projectsToBuild =
      [
        "source/timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj",
        "source/timewarp-nuru-build/timewarp-nuru-build.csproj",
        "source/timewarp-nuru-mcp/timewarp-nuru-mcp.csproj",
        "source/timewarp-nuru/timewarp-nuru.csproj"
      ];

      string verbosityLevel = command.Verbose ? "normal" : "minimal";

      foreach (string projectPath in projectsToBuild)
      {
        string fullPath = Path.Combine(repoRoot, projectPath);
        Terminal.WriteLine($"\nBuilding {projectPath}...");

        CommandResult buildCommandResult = DotNet.Build()
          .WithProject(fullPath)
          .WithConfiguration("Release")
          .WithVerbosity(verbosityLevel)
          .Build();

        if (command.Verbose)
        {
          Terminal.WriteLine(buildCommandResult.ToCommandString());
        }

        int exitCode = await buildCommandResult.RunAsync();

        if (exitCode != 0)
        {
          throw new InvalidOperationException($"Failed to build {projectPath}!");
        }
      }

      Terminal.WriteLine("\nBuild completed successfully!");
      return Unit.Value;
    }
  }
}
