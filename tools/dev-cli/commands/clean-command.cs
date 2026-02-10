// ═══════════════════════════════════════════════════════════════════════════════
// CLEAN COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Cleans the TimeWarp.Nuru solution and deletes all bin/obj directories.
// Migrated from runfiles/clean.cs to endpoints pattern.

namespace DevCli.Commands;

/// <summary>
/// Clean the TimeWarp.Nuru solution and all build artifacts.
/// </summary>
[NuruRoute("clean", Description = "Clean solution and build artifacts")]
internal sealed class CleanCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<CleanCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(CleanCommand command, CancellationToken ct)
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

      Terminal.WriteLine("Cleaning TimeWarp.Nuru solution...");
      Terminal.WriteLine($"Working from: {repoRoot}");

      // Clean the solution with minimal verbosity
      CommandResult cleanResult = DotNet.Clean()
        .WithProject(Path.Combine(repoRoot, "timewarp-nuru.slnx"))
        .WithVerbosity("minimal")
        .Build();

      int exitCode = await cleanResult.RunAsync();

      if (exitCode != 0)
      {
        throw new InvalidOperationException("dotnet clean failed!");
      }

      // Also delete obj and bin directories to ensure complete cleanup
      Terminal.WriteLine("\nDeleting obj and bin directories...");
      try
      {
        exitCode = await Shell.Builder("find")
          .WithArguments(repoRoot, "-type", "d", "(", "-name", "obj", "-o", "-name", "bin", ")", "-exec", "rm", "-rf", "{}", "+")
          .RunAsync();

        if (exitCode == 0)
        {
          Terminal.WriteLine("Deleted all obj and bin directories");
        }
      }
      catch (Exception ex)
      {
        Terminal.WriteLine($"Warning: Could not delete some directories: {ex.Message}");
        // Don't fail on this - the dotnet clean succeeded
      }

      Terminal.WriteLine("\nClean completed successfully!");
      return Unit.Value;
    }
  }
}
