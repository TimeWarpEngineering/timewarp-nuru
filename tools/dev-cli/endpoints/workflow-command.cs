#region Purpose
// Executes the full CI workflow
#endregion
#region Design
// Runs clean, build, test sequentially by invoking ./bin/dev subcommands
// Handler stores RepoRoot and DevBin as fields so private methods are zero-parameter
// RunStepAsync DRYs up the identical shell-invoke/exit-code-check pattern
#endregion

namespace DevCli.Commands;

[NuruRoute("workflow", Description = "Execute full CI workflow")]
internal sealed class WorkflowCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<WorkflowCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;
    private string DevBin = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(WorkflowCommand command, CancellationToken ct)
    {
      Ct = ct;

      if (!FindRepoRoot()) return Value;
      if (!await RunStepAsync("clean", "Clean failed!")) return Value;
      if (!await RunStepAsync("build", "Build failed!")) return Value;
      if (!await RunStepAsync("test", "Tests failed!")) return Value;

      Terminal.WriteLine("\nWorkflow completed successfully!".Green());
      return Value;
    }

    private bool FindRepoRoot()
    {
      string? root = Git.FindRoot();
      if (root is null)
      {
        Terminal.WriteErrorLine("Error: could not find repository root.");
        Environment.ExitCode = 1;
        return false;
      }

      RepoRoot = root;
      DevBin = Path.Combine(RepoRoot, "bin", "dev");
      Terminal.WriteLine("Starting CI workflow...");
      return true;
    }

    private async Task<bool> RunStepAsync(string subcommand, string failureMessage)
    {
      int exitCode = await Shell.Builder(DevBin)
        .WithArguments(subcommand)
        .WithNoValidation()
        .RunAsync(Ct);

      if (exitCode != 0)
      {
        Terminal.WriteErrorLine(failureMessage.Red());
        Environment.ExitCode = exitCode;
        return false;
      }

      return true;
    }
  }
}
