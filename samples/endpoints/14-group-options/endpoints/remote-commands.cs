namespace GroupOptionsSample.Endpoints;

using TimeWarp.Nuru;

/// <summary>
/// Nested group base for git remote commands.
/// Inherits from GitGroupBase to get all git group options,
/// plus adds remote-specific options.
/// </summary>
[NuruRouteGroup("remote", Description = "Manage remote repositories")]
public abstract class GitRemoteGroupBase : GitGroupBase
{
  [GroupOption("name", "n", Description = "Remote name")]
  public string? RemoteName { get; set; }
}

/// <summary>
/// Add a remote repository.
/// Route: git remote add {name} {url}
/// Inherits: --verbose, --dry-run, --config (from GitGroupBase)
///            --name (from GitRemoteGroupBase)
/// Route options: --fetch, --tags
/// </summary>
[NuruRoute("add", Description = "Add a remote named <name> at <url>")]
public sealed class RemoteAddCommand : GitRemoteGroupBase, ICommand<Unit>
{
  [Parameter(Description = "Remote repository name")]
  public string Name { get; set; } = string.Empty;

  [Parameter(Description = "Remote repository URL")]
  public string Url { get; set; } = string.Empty;

  [Option("fetch", "f", Description = "Fetch the remote branches immediately")]
  public bool Fetch { get; set; }

  [Option("tags", "t", Description = "Import tags from the remote repository")]
  public bool Tags { get; set; }

  public sealed class Handler(ITerminal terminal) : ICommandHandler<RemoteAddCommand, Unit>
  {
    public async ValueTask<Unit> Handle(RemoteAddCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

      if (command.Verbose)
      {
        await terminal.WriteLineAsync($"Adding remote '{command.Name}' at '{command.Url}'").ConfigureAwait(false);
      }

      if (command.DryRun)
      {
        await terminal.WriteLineAsync("[DRY RUN] Would add remote:").ConfigureAwait(false);
        await terminal.WriteLineAsync($"  Name: {command.Name}").ConfigureAwait(false);
        await terminal.WriteLineAsync($"  URL: {command.Url}").ConfigureAwait(false);
        await terminal.WriteLineAsync($"  Fetch immediately: {command.Fetch}").ConfigureAwait(false);
        await terminal.WriteLineAsync($"  Import tags: {command.Tags}").ConfigureAwait(false);
      }
      else
      {
        await terminal.WriteLineAsync($"Added remote '{command.Name}'").ConfigureAwait(false);
        if (command.Fetch)
        {
          await terminal.WriteLineAsync("Fetched remote branches").ConfigureAwait(false);
        }
        if (command.Tags)
        {
          await terminal.WriteLineAsync("Imported tags from remote").ConfigureAwait(false);
        }
      }

      return default;
    }
  }
}

/// <summary>
/// Remove a remote repository.
/// Route: git remote remove {name}
/// Inherits: --verbose, --dry-run, --config, --name
/// </summary>
[NuruRoute("remove", Description = "Remove the remote named <name>")]
public sealed class RemoteRemoveCommand : GitRemoteGroupBase, ICommand<Unit>
{
  [Parameter(Description = "Remote name to remove")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler(ITerminal terminal) : ICommandHandler<RemoteRemoveCommand, Unit>
  {
    public async ValueTask<Unit> Handle(RemoteRemoveCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

      if (command.Verbose)
      {
        await terminal.WriteLineAsync($"Removing remote '{command.Name}'").ConfigureAwait(false);
      }

      if (command.DryRun)
      {
        await terminal.WriteLineAsync("[DRY RUN] Would remove remote:").ConfigureAwait(false);
        await terminal.WriteLineAsync($"  Name: {command.Name}").ConfigureAwait(false);
      }
      else
      {
        await terminal.WriteLineAsync($"Removed remote '{command.Name}'").ConfigureAwait(false);
      }

      return default;
    }
  }
}
