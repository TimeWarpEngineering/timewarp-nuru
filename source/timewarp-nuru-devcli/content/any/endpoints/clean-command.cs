#region Purpose
// Clean the solution and all build artifacts
#endregion
#region Design
// Uses IRepoCleanService from TimeWarp.Amuru for comprehensive cleaning.
// Executes dotnet restore, dotnet clean, then removes bin/obj directories.
#endregion

namespace DevCli;

using TimeWarp.Amuru;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

/// <summary>
/// Clean solution and build artifacts.
/// </summary>
[NuruRoute("clean", Description = "Clean solution and build artifacts")]
public sealed class CleanCommand : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<CleanCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private readonly IRepoCleanService RepoCleanService;

    public Handler(ITerminal terminal, IRepoCleanService repoCleanService)
    {
      Terminal = terminal;
      RepoCleanService = repoCleanService;
    }

    public async ValueTask<Unit> Handle(CleanCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

      Terminal.WriteLine("Cleaning repository...");

      CleanResult result = await RepoCleanService
        .CleanAsync(cancellationToken)
        .ConfigureAwait(false);

      Terminal.WriteLine
      (
        $"Deleted {result.ObjDirectoriesDeleted} obj directories, {result.BinDirectoriesDeleted} bin directories"
          .Green()
      );

      if (result.RootBinFilesCleaned > 0)
      {
        Terminal.WriteLine
        (
          $"Cleaned {result.RootBinFilesCleaned} files from root bin/ (preserved dev executable)"
            .Green()
        );
      }

      Terminal.WriteLine("\nClean completed successfully!".Green());
      return Unit.Value;
    }
  }
}
