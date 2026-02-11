namespace GroupOptionsSample.Endpoints;

using TimeWarp.Nuru;

/// <summary>
/// Git clone command demonstrating typed group options.
/// Inherits standard group options plus demonstrates numeric types.
/// </summary>
[NuruRoute("clone", Description = "Clone a repository into a new directory")]
public sealed class CloneCommand : GitGroupBase, ICommand<Unit>
{
  [Parameter(Description = "Repository URL to clone")]
  public string Url { get; set; } = string.Empty;

  [Option("depth", Description = "Create a shallow clone with limited history")]
  public int? Depth { get; set; }

  [Option("branch", "b", Description = "Branch to checkout after cloning")]
  public string? Branch { get; set; }

  public sealed class Handler(ITerminal terminal) : ICommandHandler<CloneCommand, Unit>
  {
    public async ValueTask<Unit> Handle(CloneCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

      if (command.Verbose)
      {
        await terminal.WriteLineAsync($"Verbose mode enabled").ConfigureAwait(false);
        await terminal.WriteLineAsync($"Cloning from: {command.Url}").ConfigureAwait(false);
        if (command.Depth.HasValue)
        {
          await terminal.WriteLineAsync($"Shallow clone depth: {command.Depth}").ConfigureAwait(false);
        }
        if (!string.IsNullOrEmpty(command.Branch))
        {
          await terminal.WriteLineAsync($"Target branch: {command.Branch}").ConfigureAwait(false);
        }
      }

      if (command.DryRun)
      {
        await terminal.WriteLineAsync("[DRY RUN] Would clone repository:").ConfigureAwait(false);
        await terminal.WriteLineAsync($"  URL: {command.Url}").ConfigureAwait(false);
      }
      else
      {
        await terminal.WriteLineAsync($"Cloning into 'repository'...").ConfigureAwait(false);
        await terminal.WriteLineAsync("remote: Enumerating objects...").ConfigureAwait(false);
        await terminal.WriteLineAsync("remote: Counting objects...").ConfigureAwait(false);
        await terminal.WriteLineAsync("Receiving objects...").ConfigureAwait(false);
        await terminal.WriteLineAsync("Successfully cloned repository").ConfigureAwait(false);
      }

      return default;
    }
  }
}
